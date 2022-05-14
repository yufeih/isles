using Microsoft.Xna.Framework.Input;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Isles.Graphics;

public class ImGuiRenderer
{
    private static readonly int s_vertexSize = Marshal.SizeOf<ImDrawVert>();
    private static readonly VertexDeclaration s_vertexDeclaration = new(
        s_vertexSize,
        new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );
    private static readonly RasterizerState s_rasterizerState = new()
    {
        CullMode = CullMode.None,
        DepthBias = 0,
        FillMode = FillMode.Solid,
        MultiSampleAntiAlias = false,
        ScissorTestEnable = true,
        SlopeScaleDepthBias = 0
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;

    private byte[] _vertexData;
    private VertexBuffer _vertexBuffer;
    private int _vertexBufferSize;

    private byte[] _indexData;
    private IndexBuffer _indexBuffer;
    private int _indexBufferSize;

    private readonly Dictionary<IntPtr, Texture2D> _textures = new();
    private nint _nextTextureId;

    private int _scrollWheelValue;
    private readonly List<int> _keys = new();

    public ImGuiRenderer(GraphicsDevice graphicsDevice)
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        _graphicsDevice = graphicsDevice;
        _effect = new BasicEffect(graphicsDevice)
        {
            World = Matrix.Identity,
            View = Matrix.Identity,
            VertexColorEnabled = true,
            TextureEnabled = true,
        };

        ImGui.GetIO().Fonts.AddFontDefault();
        CreateFontAtlas();
    }

    private void SetupInput()
    {
        var io = ImGui.GetIO();

        _keys.Add(io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab);
        _keys.Add(io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left);
        _keys.Add(io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right);
        _keys.Add(io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up);
        _keys.Add(io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down);
        _keys.Add(io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp);
        _keys.Add(io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home);
        _keys.Add(io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Back);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Space] = (int)Keys.Space);
        _keys.Add(io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A);
        _keys.Add(io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C);
        _keys.Add(io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V);
        _keys.Add(io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z);

        TextInputEXT.TextInput += c =>
        {
            if (c == '\t') return;

            ImGui.GetIO().AddInputCharacter(c);
        };
    }

    private unsafe void CreateFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out var width, out var height, out var bytesPerPixel);

        var pixels = new byte[width * height * bytesPerPixel];
        unsafe { Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length); }

        var texture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        texture.SetData(pixels);

        io.Fonts.SetTexID(BindTexture(texture));
        io.Fonts.ClearTexData();
    }

    public IntPtr BindTexture(Texture2D texture)
    {
        var id = new IntPtr(_nextTextureId++);
        _textures.Add(id, texture);
        return id;
    }

    public void Update(float dt)
    {
        ImGui.GetIO().DeltaTime = dt;
        UpdateInput();
    }

    public void Begin()
    {
        ImGui.NewFrame();
    }

    public void End()
    {
        ImGui.Render();
        Draw(ImGui.GetDrawData());
    }

    private void UpdateInput()
    {
        var io = ImGui.GetIO();
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        foreach (var key in _keys)
        {
            io.KeysDown[key] = keyboard.IsKeyDown((Keys)key);
        }

        io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        io.KeySuper = keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);

        io.DisplaySize = new(
            _graphicsDevice.PresentationParameters.BackBufferWidth,
            _graphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new(1f, 1f);

        io.MousePos = new(mouse.X, mouse.Y);

        io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed;
        io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed;
        io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed;

        var scrollDelta = mouse.ScrollWheelValue - _scrollWheelValue;
        io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
        _scrollWheelValue = mouse.ScrollWheelValue;
    }

    private void Draw(ImDrawDataPtr drawData)
    {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
        var lastViewport = _graphicsDevice.Viewport;
        var lastScissorBox = _graphicsDevice.ScissorRectangle;

        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
        _graphicsDevice.RasterizerState = s_rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        // Setup projection
        _graphicsDevice.Viewport = new(0, 0, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);

        UpdateBuffers(drawData);
        RenderCommandLists(drawData);

        // Restore modified state
        _graphicsDevice.Viewport = lastViewport;
        _graphicsDevice.ScissorRectangle = lastScissorBox;
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        // Expand buffers if we need more room
        if (drawData.TotalVtxCount > _vertexBufferSize)
        {
            _vertexBuffer?.Dispose();

            _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
            _vertexBuffer = new(_graphicsDevice, s_vertexDeclaration, _vertexBufferSize, BufferUsage.None);
            _vertexData = new byte[_vertexBufferSize * s_vertexSize];
        }

        if (drawData.TotalIdxCount > _indexBufferSize)
        {
            _indexBuffer?.Dispose();

            _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
            _indexBuffer = new(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
            _indexData = new byte[_indexBufferSize * sizeof(ushort)];
        }

        // Copy ImGui's vertices and indices to a set of managed byte arrays
        var vtxOffset = 0;
        var idxOffset = 0;

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdListsRange[n];

            fixed (void* vtxDstPtr = &_vertexData[vtxOffset * s_vertexSize])
            fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
            {
                Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * s_vertexSize);
                Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the gpu vertex- and index buffers
        _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * s_vertexSize);
        _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
    }

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        var vtxOffset = 0;
        var idxOffset = 0;

        var displaySize = ImGui.GetIO().DisplaySize;
        _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, displaySize.X, displaySize.Y, 0f, -1f, 1f);

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdListsRange[n];

            for (var cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
            {
                var drawCmd = cmdList.CmdBuffer[cmdi];
                if (drawCmd.ElemCount == 0)
                    continue;

                _effect.Texture = _textures[drawCmd.TextureId];

                _graphicsDevice.ScissorRectangle = new Rectangle(
                    (int)drawCmd.ClipRect.X,
                    (int)drawCmd.ClipRect.Y,
                    (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    _graphicsDevice.DrawIndexedPrimitives(
                        primitiveType: PrimitiveType.TriangleList,
                        baseVertex: (int)drawCmd.VtxOffset + vtxOffset,
                        minVertexIndex: 0,
                        numVertices: cmdList.VtxBuffer.Size,
                        startIndex: (int)drawCmd.IdxOffset + idxOffset,
                        primitiveCount: (int)drawCmd.ElemCount / 3
                    );
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }
}
