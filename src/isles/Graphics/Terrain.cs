// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles.Graphics;

public class Terrain : BaseLandscape
{
    private Effect terrainEffect;
    private IndexBuffer indexBuffer;
    private VertexBuffer[] vertexBuffers;

    private int vertexCount;
    private int primitiveCount;

    /// <summary>
    /// Gets or sets the fog texture used to draw the landscape.
    /// </summary>
    public Texture2D FogTexture { get; set; }

    /// <summary>
    /// A static texture applied to the water surface.
    /// </summary>
    private Texture waterTexture;

    /// <summary>
    /// This texture is used as a bump map to simulate water.
    /// </summary>
    private Texture waterDstortion;

    /// <summary>
    /// Render target used to draw the reflection & refraction texture.
    /// </summary>
    private RenderTarget2D reflectionRenderTarget;

    /// <summary>
    /// This texture is generated each frame for water reflection color sampling.
    /// </summary>
    private Texture2D waterReflection;

    /// <summary>
    /// Water mesh.
    /// </summary>
    private int waterVertexCount;
    private int waterPrimitiveCount;
    private VertexBuffer waterVertices;
    private IndexBuffer waterIndices;

    public Effect WaterEffect { get; set; }

    public override void Load(TerrainData data, TextureLoader textureLoader)
    {
        base.Load(data, textureLoader);

        waterTexture = textureLoader.LoadTexture(data.WaterTexture);
        waterDstortion = textureLoader.LoadTexture(data.WaterBumpTexture);

        Initialize(BaseGame.Singleton);
    }

    public override void Initialize(BaseGame game)
    {
        base.Initialize(game);

        // Load terrain effect
        terrainEffect = game.ShaderLoader.LoadShader("shaders/Terrain.cso");

        // Set patch LOD to highest
        foreach (Patch patch in Patches)
        {
            patch.LevelOfDetail = Patch.HighestLOD;
        }

        // Initialize index buffer.
        // All patches use the same index buffer since tiled landscape
        // do not deal with LOD stuff :)
        indexBuffer = new IndexBuffer(game.GraphicsDevice, typeof(ushort),
                                      6 * Patch.MaxPatchResolution *
                                          Patch.MaxPatchResolution,
                                      BufferUsage.WriteOnly);

        var indices = new ushort[6 * Patch.MaxPatchResolution *
                                          Patch.MaxPatchResolution];

        // Fill index buffer and
        Patches[0].FillIndices16(ref indices, 0);

        indexBuffer.SetData(indices);

        // Initialize vertices
        var vertexBufferElementCount = (Patch.MaxPatchResolution + 1) *
                                       (Patch.MaxPatchResolution + 1);

        vertexCount = vertexBufferElementCount;
        primitiveCount = Patch.MaxPatchResolution * Patch.MaxPatchResolution * 2;

        // Create a vertex buffer for each patch
        vertexBuffers = new VertexBuffer[PatchCountOnXAxis * PatchCountOnYAxis];

        // Create an array to store the vertices
        var vertices = new TerrainVertex[vertexBufferElementCount];

        // Initialize individual patch vertex buffer
        var patchIndex = 0;
        for (var yPatch = 0; yPatch < PatchCountOnYAxis; yPatch++)
        {
            for (var xPatch = 0; xPatch < PatchCountOnYAxis; xPatch++)
            {
                // Fill patch vertices
                Patches[patchIndex].FillVertices(0,
                delegate (int x, int y)
                {
                    return new Vector3(x * Size.X / (GridCountOnXAxis - 1),
                                       y * Size.Y / (GridCountOnYAxis - 1),
                                       HeightField[x, y]);
                },
                delegate (uint index, Vector3 position)
                {
                    vertices[index].Position = position;
                },
                delegate (uint index, int x, int y)
                {
                    vertices[index].Position = new Vector3(
                        x * Size.X / (GridCountOnXAxis - 1),
                        y * Size.Y / (GridCountOnYAxis - 1), HeightField[x, y]);

                        // Texture0 is the tile texture, which only covers half patch
                        vertices[index].TextureCoordinate0 = new Vector2(
                        2.0f * PatchCountOnXAxis * x / (GridCountOnXAxis - 1),
                        2.0f * PatchCountOnYAxis * y / (GridCountOnYAxis - 1));

                        // Texture1 is the visibility texture, which covers the entire terrain
                        vertices[index].TextureCoordinate1 = new Vector2(
                        1.0f * x / (GridCountOnXAxis - 1),
                        1.0f * y / (GridCountOnYAxis - 1));
                });

                // Create a vertex buffer for the patch
                vertexBuffers[patchIndex] = new VertexBuffer(game.GraphicsDevice,
                                                             typeof(TerrainVertex),
                                                             vertexBufferElementCount,
                                                             BufferUsage.WriteOnly);

                // Set vertex buffer vertices
                vertexBuffers[patchIndex].SetData(vertices);

                // Next patch
                patchIndex++;
            }
        }

        WaterEffect = game.ShaderLoader.LoadShader("shaders/Water.cso");
        InitializeWater();

        surfaceEffect = game.ShaderLoader.LoadShader("shaders/Surface.cso");
        surfaceVertexBuffer = new DynamicVertexBuffer(game.GraphicsDevice,
            typeof(VertexPositionColorTexture), MaxSurfaceVertices, BufferUsage.WriteOnly);
        surfaceIndexBuffer = new DynamicIndexBuffer(game.GraphicsDevice,
            typeof(ushort), MaxSurfaceIndices, BufferUsage.WriteOnly);
    }

    private struct TexturedSurface
    {
        public Texture2D Texture;
        public Vector3 Position;
        public Color Color;
        public float Width;
        public float Height;
    }

    private const int MaxSurfaceVertices = 512;
    private const int MaxSurfaceIndices = 768;
    private Effect surfaceEffect;
    private DynamicVertexBuffer surfaceVertexBuffer;
    private DynamicIndexBuffer surfaceIndexBuffer;
    private readonly List<ushort> surfaceIndices = new();
    private readonly List<VertexPositionColorTexture> surfaceVertices = new();
    private readonly LinkedList<TexturedSurface> texturedSurfaces = new();

    /// <summary>
    /// Draw a textured surface on top of the landscape. (But below all world objects).
    /// </summary>
    public void DrawSurface(Texture2D texture, Vector3 position, float width, float height, Color color)
    {
        TexturedSurface surface;
        surface.Color = color;
        surface.Texture = texture ?? throw new ArgumentNullException();
        surface.Position = position;

        // Plus a little offset
        surface.Position.Z = GetHeight(position.X, position.Y) + 6;

        // Divided by 2 so we don't have to do this during presentation
        surface.Width = width / 2;
        surface.Height = height / 2;

        // Add a new textured surface to the list.
        // Sort the entries by texture.
        LinkedListNode<TexturedSurface> p = texturedSurfaces.First;

        while (p != null)
        {
            if (p.Value.Texture != texture)
            {
                break;
            }

            p = p.Next;
        }

        if (p == null)
        {
            texturedSurfaces.AddFirst(surface);
        }
        else
        {
            texturedSurfaces.AddBefore(p, surface);
        }
    }

    public void PresentSurface(in ViewMatrices matrices)
    {
        if (texturedSurfaces.Count <= 0)
        {
            return;
        }

        game.GraphicsDevice.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default);

        surfaceEffect.Parameters["WorldViewProjection"].SetValue(matrices.ViewProjection);

        surfaceEffect.CurrentTechnique.Passes[0].Apply();

        LinkedListNode<TexturedSurface> start = texturedSurfaces.First;
        LinkedListNode<TexturedSurface> end = texturedSurfaces.First;
        Texture2D texture = start.Value.Texture;

        while (end != null)
        {
            if (end.Value.Texture != texture && start != end)
            {
                PresentSurface(start, end);

                start = end;
                texture = end.Value.Texture;
            }

            end = end.Next;
        }

        PresentSurface(start, end);

        texturedSurfaces.Clear();
    }

    private void PresentSurface(LinkedListNode<TexturedSurface> start, LinkedListNode<TexturedSurface> end)
    {
        Texture2D texture = start.Value.Texture;
        VertexPositionColorTexture vertex;

        surfaceIndices.Clear();
        surfaceVertices.Clear();

        while (start != end)
        {
            // Quad indices
            surfaceIndices.Add((ushort)(surfaceVertices.Count + 0));
            surfaceIndices.Add((ushort)(surfaceVertices.Count + 1));
            surfaceIndices.Add((ushort)(surfaceVertices.Count + 3));
            surfaceIndices.Add((ushort)(surfaceVertices.Count + 3));
            surfaceIndices.Add((ushort)(surfaceVertices.Count + 1));
            surfaceIndices.Add((ushort)(surfaceVertices.Count + 2));

            // Create 4 quad vertices
            vertex.Color = start.Value.Color;
            vertex.Position = start.Value.Position;
            vertex.Position.X -= start.Value.Width;
            vertex.Position.Y += start.Value.Height;
            vertex.TextureCoordinate.X = 0.02f;
            vertex.TextureCoordinate.Y = 0.98f;
            surfaceVertices.Add(vertex);

            vertex.Position = start.Value.Position;
            vertex.Position.X += start.Value.Width;
            vertex.Position.Y += start.Value.Height;
            vertex.TextureCoordinate.X = 0.98f;
            vertex.TextureCoordinate.Y = 0.98f;
            surfaceVertices.Add(vertex);

            vertex.Position = start.Value.Position;
            vertex.Position.X += start.Value.Width;
            vertex.Position.Y -= start.Value.Height;
            vertex.TextureCoordinate.X = 0.98f;
            vertex.TextureCoordinate.Y = 0.02f;
            surfaceVertices.Add(vertex);

            vertex.Position = start.Value.Position;
            vertex.Position.X -= start.Value.Width;
            vertex.Position.Y -= start.Value.Height;
            vertex.TextureCoordinate.X = 0.02f;
            vertex.TextureCoordinate.Y = 0.02f;
            surfaceVertices.Add(vertex);

            start = start.Next;
        }

        // Draw user primitives
        surfaceEffect.Parameters["BasicTexture"].SetValue(texture);

        surfaceIndexBuffer.SetData(surfaceIndices.ToArray());
        surfaceVertexBuffer.SetData(surfaceVertices.ToArray());

        game.GraphicsDevice.SetRenderState(depthStencilState: DepthStencilState.DepthRead);

        game.GraphicsDevice.Indices = surfaceIndexBuffer;
        game.GraphicsDevice.SetVertexBuffer(surfaceVertexBuffer);
        game.GraphicsDevice.DrawIndexedPrimitives(
            PrimitiveType.TriangleList, 0, 0, surfaceVertices.Count, 0, surfaceIndices.Count / 3);
    }

    private void InitializeWater()
    {
        // Reflection & Refraction textures
        reflectionRenderTarget = new RenderTarget2D(
            graphics, 1024, 1024, true, SurfaceFormat.Color, graphics.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.DiscardContents);

        // Create vb / ib
        const int CellCount = 16;
        const int TextureRepeat = 32;

        waterVertexCount = (CellCount + 1) * (CellCount + 1);
        waterPrimitiveCount = CellCount * CellCount * 2;
        var vertexData = new VertexPositionTexture[waterVertexCount];

        // Water height is zero at the 4 corners of the terrain quad
        var waterSize = Math.Max(Size.X, Size.Y) * 2;
        var cellSize = waterSize / CellCount;

        var i = 0;
        Vector2 pos;
        var center = new Vector2(Size.X / 2, Size.Y / 2);

        for (var y = 0; y <= CellCount; y++)
        {
            for (var x = 0; x <= CellCount; x++)
            {
                pos.X = (Size.X - waterSize) / 2 + cellSize * x;
                pos.Y = (Size.Y - waterSize) / 2 + cellSize * y;

                vertexData[i].Position.X = pos.X;
                vertexData[i].Position.Y = pos.Y;

                vertexData[i].TextureCoordinate.X = 1.0f * x * TextureRepeat / CellCount;
                vertexData[i].TextureCoordinate.Y = 1.0f * y * TextureRepeat / CellCount;

                i++;
            }
        }

        var indexData = new short[waterPrimitiveCount * 3];

        i = 0;
        for (var y = 0; y < CellCount; y++)
        {
            for (var x = 0; x < CellCount; x++)
            {
                indexData[i++] = (short)((CellCount + 1) * (y + 1) + x);     // 0
                indexData[i++] = (short)((CellCount + 1) * y + x + 1);       // 2
                indexData[i++] = (short)((CellCount + 1) * (y + 1) + x + 1); // 1
                indexData[i++] = (short)((CellCount + 1) * (y + 1) + x);     // 0
                indexData[i++] = (short)((CellCount + 1) * y + x);           // 3
                indexData[i++] = (short)((CellCount + 1) * y + x + 1);       // 2
            }
        }

        waterVertices = new VertexBuffer(
            graphics, typeof(VertexPositionTexture), waterVertexCount, BufferUsage.WriteOnly);

        waterVertices.SetData(vertexData);

        waterIndices = new IndexBuffer(
            graphics, typeof(short), waterPrimitiveCount * 3, BufferUsage.WriteOnly);

        waterIndices.SetData(indexData);
    }

    public void UpdateWaterReflectionAndRefraction(in ViewMatrices matrices)
    {
        graphics.PushRenderTarget(reflectionRenderTarget);

        graphics.Clear(Color.Black);

        // Create a reflection view matrix
        var viewReflect = Matrix.Multiply(
            Matrix.CreateReflection(new Plane(Vector3.UnitZ, 0)), matrices.View);

        DrawTerrain(viewReflect * matrices.Projection, true);

        // Present the model manager to draw those models
        game.ModelRenderer.Draw(viewReflect * matrices.Projection);

        // Draw refraction onto the reflection texture
        DrawTerrain(matrices.ViewProjection, false);

        graphics.PopRenderTarget();

        // Retrieve refraction texture
        waterReflection = reflectionRenderTarget;
    }

    public void DrawWater(GameTime gameTime, in ViewMatrices matrices)
    {
        graphics.SetRenderState(BlendState.Opaque, DepthStencilState.DepthRead, RasterizerState.CullNone);

        // Draw water mesh
        graphics.Indices = waterIndices;
        graphics.SetVertexBuffer(waterVertices);

        if (FogTexture != null)
        {
            WaterEffect.Parameters["FogTexture"].SetValue(FogTexture);
        }

        WaterEffect.Parameters["ColorTexture"].SetValue(waterTexture);

        if (game.Settings.ReflectionEnabled)
        {
            WaterEffect.CurrentTechnique = WaterEffect.Techniques["Realisic"];
            WaterEffect.Parameters["ReflectionTexture"].SetValue(waterReflection);
        }
        else
        {
            WaterEffect.CurrentTechnique = WaterEffect.Techniques["Default"];
        }

        WaterEffect.Parameters["DistortionTexture"].SetValue(waterDstortion);
        WaterEffect.Parameters["ViewInverse"].SetValue(matrices.ViewInverse);
        WaterEffect.Parameters["WorldViewProj"].SetValue(matrices.ViewProjection);
        WaterEffect.Parameters["WorldView"].SetValue(matrices.View);
        WaterEffect.Parameters["DisplacementScroll"].SetValue(MoveInCircle(gameTime, 0.01f));

        WaterEffect.CurrentTechnique.Passes[0].Apply();

        graphics.DrawIndexedPrimitives(
            PrimitiveType.TriangleList, 0, 0, waterVertexCount, 0, waterPrimitiveCount);
    }

    /// <summary>
    /// Helper for moving a value around in a circle.
    /// </summary>
    private static Vector2 MoveInCircle(GameTime gameTime, float speed)
    {
        var time = gameTime.TotalGameTime.TotalSeconds * speed;

        var x = (float)Math.Cos(time);
        var y = (float)Math.Sin(time);

        return new Vector2(x, y);
    }

    public void DrawTerrain(Matrix viewProjection, bool upper)
    {
        EffectTechnique technique = upper ?
            terrainEffect.Techniques["FastUpper"] : terrainEffect.Techniques["FastLower"];

        DrawTerrain(viewProjection, technique);
    }

    public void DrawTerrain(ShadowEffect shadowEffect, in ViewMatrices matrices)
    {
        DrawTerrain(matrices.ViewProjection, terrainEffect.Techniques["Default"]);

        if (shadowEffect != null)
        {
            DrawTerrainShadow(shadowEffect);
        }
    }

    private void DrawTerrain(Matrix viewProjection, EffectTechnique technique)
    {
        graphics.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default, RasterizerState.CullNone);

        // Set parameters
        if (FogTexture != null)
        {
            terrainEffect.Parameters["FogTexture"].SetValue(FogTexture);
        }

        terrainEffect.Parameters["WorldViewProjection"].SetValue(viewProjection);

        var viewFrustum = new BoundingFrustum(viewProjection);

        // Set indices and vertices
        game.GraphicsDevice.Indices = indexBuffer;

        terrainEffect.CurrentTechnique = technique;

        terrainEffect.CurrentTechnique.Passes[0].Apply();

        // Draw each patch
        for (var iPatch = 0; iPatch < Patches.Count; iPatch++)
        {
            Patches[iPatch].Visible = viewFrustum.Intersects(Patches[iPatch].BoundingBox);
            if (Patches[iPatch].Visible)
            {
                // Set patch vertex buffer
                game.GraphicsDevice.SetVertexBuffer(vertexBuffers[iPatch]);

                // Draw each layer
                foreach (Layer layer in Layers)
                {
                    // Set textures
                    terrainEffect.Parameters["ColorTexture"].SetValue(layer.ColorTexture);
                    terrainEffect.Parameters["AlphaTexture"].SetValue(layer.AlphaTexture);
                    terrainEffect.CurrentTechnique.Passes[0].Apply();

                    // Draw patch primitives
                    game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                              0, 0, vertexCount, 0, primitiveCount);
                }
            }
        }
    }

    private void DrawTerrainShadow(ShadowEffect shadowEffect)
    {
        graphics.SetRenderState(BlendState.NonPremultiplied, DepthStencilState.Default, RasterizerState.CullNone);

        terrainEffect.Parameters["ShadowMap"].SetValue(shadowEffect.ShadowMap);
        terrainEffect.Parameters["LightViewProjection"].SetValue(shadowEffect.LightViewProjection);
        terrainEffect.CurrentTechnique = terrainEffect.Techniques["ShadowMapping"];

        terrainEffect.CurrentTechnique.Passes[0].Apply();

        // Draw each patch
        for (var iPatch = 0; iPatch < Patches.Count; iPatch++)
        {
            if (Patches[iPatch].Visible)
            {
                // Set patch vertex buffer
                game.GraphicsDevice.SetVertexBuffer(vertexBuffers[iPatch]);

                // Draw patch primitives
                game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                                          0, 0, vertexCount, 0, primitiveCount);
            }
        }
    }

    public struct TerrainVertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate0;
        public Vector2 TextureCoordinate1;

        public static readonly VertexDeclaration VertexDeclaration = new(new VertexElement[]
        {
                new(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        });

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
