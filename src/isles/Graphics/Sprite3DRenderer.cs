// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles.Graphics;

public readonly struct Sprite3D
{
    public readonly Texture2D Texture { get; init; }
    public readonly Rectangle? SourceRectangle { get; init; }
    public readonly Matrix Transform { get; init; }
    public readonly Color Color { get; init; }
}

public class Sprite3DRenderer
{
    private const int MaxSpriteCount = 2048;
    private const int MaxVertexCount = MaxSpriteCount * 4;
    private const int MaxIndexCount = MaxSpriteCount * 6;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _effect;
    private readonly DynamicVertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[MaxSpriteCount];
    private readonly TextureComparer _sortComparer;
    private readonly Sprite3D[] _sprites = new Sprite3D[MaxSpriteCount];
    private readonly int[] _sort = new int[MaxSpriteCount];

    private int _spriteCount;

    public Sprite3DRenderer(GraphicsDevice graphicsDevice, ShaderLoader shaderLoader)
    {
        _graphicsDevice = graphicsDevice;
        _sortComparer = new() { Sprites = _sprites };
        _effect = shaderLoader.LoadShader("shaders/Surface.cso");
        _vertexBuffer = new(graphicsDevice, VertexPositionColorTexture.VertexDeclaration, MaxVertexCount, BufferUsage.WriteOnly);
        _indexBuffer = new(graphicsDevice, typeof(ushort), MaxIndexCount, BufferUsage.WriteOnly);
        _indexBuffer.SetData(GenerateIndices());

        for (var i = 0; i < _sort.Length; i++)
            _sort[i] = i;
    }

    public void Clear()
    {
        _spriteCount = 0;
    }

    public void Add(Texture2D texture, Vector3 position, float width, float height, Color color, Rectangle? sourceRectangle = null)
    {
        var transform = default(Matrix);
        transform.M11 = width;
        transform.M22 = height;
        transform.M33 = 1;
        transform.M41 = position.X - width / 2;
        transform.M42 = position.Y - height / 2;
        transform.M43 = position.Z;

        Add(new() { Texture = texture, Transform = transform, SourceRectangle = sourceRectangle, Color = color });
    }

    public void Add(in Sprite3D sprite)
    {
        _sprites[_spriteCount++] = sprite;
    }

    public void Draw(in ViewMatrices matrices)
    {
        if (_spriteCount <= 0)
            return;

        Array.Sort(_sort, 0, _spriteCount, _sortComparer);

        GenerateVertices();

        _vertexBuffer.SetData(0, _vertices, 0, _spriteCount * 4, Marshal.SizeOf<VertexPositionColorTexture>(), SetDataOptions.None);

        _effect.Parameters["WorldViewProjection"].SetValue(matrices.ViewProjection);

        _graphicsDevice.SetRenderState(depthStencilState: DepthStencilState.DepthRead, rasterizerState: RasterizerState.CullNone);
        _graphicsDevice.Indices = _indexBuffer;
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);

        var baseSprite = 0;
        for (var i = 0; i < _spriteCount - 1; i++)
        {
            ref var sprite = ref _sprites[_sort[i]];
            ref var nextSprite = ref _sprites[_sort[i + 1]];

            if (sprite.Texture != nextSprite.Texture)
            {
                DrawPrimitives(sprite.Texture, baseSprite, i + 1);
                baseSprite = i;
            }
        }

        DrawPrimitives(_sprites[_sort[baseSprite]].Texture, baseSprite, _spriteCount);
        
        void DrawPrimitives(Texture2D texture, int startInclusive, int endExclusive)
        {
            var count = endExclusive - startInclusive;
            _effect.Parameters["BasicTexture"].SetValue(texture);
            _effect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, startInclusive * 4, 0, count * 4, 0, count * 2);
        }
    }

    private void GenerateVertices()
    {
        var i = 0;

        for (var isort = 0; isort < _spriteCount; isort++)
        {
            ref var sprite = ref _sprites[_sort[isort]];

            _vertices[i].Color = sprite.Color;
            _vertices[i + 1].Color = sprite.Color;
            _vertices[i + 2].Color = sprite.Color;
            _vertices[i + 3].Color = sprite.Color;

            _vertices[i].Position = Vector3.Transform(default, sprite.Transform);
            _vertices[i + 1].Position = Vector3.Transform(new(1, 0, 0), sprite.Transform);
            _vertices[i + 2].Position = Vector3.Transform(new(0, 1, 0), sprite.Transform);
            _vertices[i + 3].Position = Vector3.Transform(new(1, 1, 0), sprite.Transform);

            var w = sprite.Texture.Width;
            var h = sprite.Texture.Height;
            var rect = sprite.SourceRectangle ?? new(0, 0, w, h);
            var ux0 = (float)rect.X / w;
            var uy0 = (float)rect.Y / h;
            var ux1 = (float)(rect.X + rect.Width) / w;
            var uy1 = (float)(rect.Y + rect.Height) / h;

            _vertices[i].TextureCoordinate.X = ux0;
            _vertices[i].TextureCoordinate.Y = uy0;
            _vertices[i + 1].TextureCoordinate.X = ux1;
            _vertices[i + 1].TextureCoordinate.Y = uy0;
            _vertices[i + 2].TextureCoordinate.X = ux0;
            _vertices[i + 2].TextureCoordinate.Y = uy1;
            _vertices[i + 3].TextureCoordinate.X = ux1;
            _vertices[i + 3].TextureCoordinate.Y = uy1;

            i += 4;
        }
    }

    private static short[] GenerateIndices()
    {
        var result = new short[MaxIndexCount];
        for (int i = 0, j = 0; i < MaxIndexCount; i += 6, j += 4)
        {
            result[i] = (short)(j);
            result[i + 1] = (short)(j + 1);
            result[i + 2] = (short)(j + 2);
            result[i + 3] = (short)(j + 3);
            result[i + 4] = (short)(j + 2);
            result[i + 5] = (short)(j + 1);
        }
        return result;
    }

    class TextureComparer : IComparer<int>
    {
        public Sprite3D[] Sprites { get; init; }

        public int Compare(int a, int b)
        {
            return Sprites[a].GetHashCode().CompareTo(Sprites[b].GetHashCode());
        }
    }
}
