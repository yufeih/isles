//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    #region Billboard
    /// <summary>
    /// Type of a billboard
    /// </summary>
    [Flags]
    public enum BillboardType
    {
        /// <summary>
        /// Uses a two pass rendering technique for vegetation rendering
        /// </summary>
        Vegetation = 1,

        /// <summary>
        /// Rotate around the center
        /// </summary>
        CenterOriented = (1 << 2),

        /// <summary>
        /// Rotate around a normal vector
        /// </summary>
        NormalOriented = (1 << 3),

        /// <summary>
        /// Whether depth buffer is enabled when rendering the billboard
        /// </summary>
        DepthBufferEnable = (1 << 4),
    }

    public enum AnchorType
    {
        /// <summary>
        /// Position anchor on top
        /// </summary>
        Top = 0,

        /// <summary>
        /// Position anchor on Center
        /// </summary>
        Center = 1,

        /// <summary>
        /// Position anchor on bottom
        /// </summary>
        Bottom = 2

    }

    /// <summary>
    /// A billboard definition
    /// </summary>
    public class Billboard
    {

        /// <summary>
        /// Texture used to draw the billboard
        /// TODO: Implement animated texture
        /// </summary>
        public String TextureName
        {
            get
            {
                return this.textureName;
            }
            set
            {
                texture = BaseGame.Singleton.ZipContent.Load<Texture2D>(value);
                textureName = value;
            }
        }

        public Texture2D Texture
        {
            get
            {
                return this.texture;
            }
            set
            {
                this.texture = value;
            }
        }
        private String textureName;
        private Texture2D texture;

        /// <summary>
        /// Position of the billboard. Bottom center in the texture.
        /// </summary>
        private Vector3 position;
        public Vector3 Position
        {
            get
            {
                return this.position;
            }

            set
            {
                this.position = value;
            }
        }
        

        /// <summary>
        /// Type of the billboard
        /// </summary>
        public BillboardType Type
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
            }
        }
        private BillboardType type;

        /// <summary>
        /// Type of position anchor
        /// </summary>
        public AnchorType AchorType
        {
            get
            {
                return this.achorType;
            }
            set
            {
                this.achorType = value;
            }
        }
        private AnchorType achorType;
        

        /// <summary>
        /// Normalized vector around which the billboard is rotating
        /// </summary>
        public Vector3 Normal;
       

        /// <summary>
        /// Size of the billboard
        /// </summary>
        public Vector2 Size;
        

        /// <summary>
        /// Source rectangle (min, max). Measured in float [0 ~ 1]
        /// </summary>
        public Vector4 SourceRectangle;
        

        /// <summary>
        /// Default source rectangle
        /// </summary>
        public static readonly Vector4 DefaultSourceRectangle = new Vector4(0, 0, 1, 1);
        public static readonly Vector4 DefaultSourceRectangleXInversed = new Vector4(1, 0, 0, 1);

        public Billboard()
        {
            this.achorType = AnchorType.Center;
        }

        public void Draw()
        {
            BaseGame.Singleton.Billboard.Draw(this);
        }
    }
    #endregion

    #region BillboardManager
    /// <summary>
    /// Manager class for billboard
    /// </summary>
    public class BillboardManager : IDisposable
    {
        private static Vector3 GetCenterPosition(Billboard billboard)
        {
            if(billboard.AchorType == AnchorType.Top)
            {
                return billboard.Position - new Vector3(0, 0, billboard.Size.Y / 2);
            }
            else if (billboard.AchorType == AnchorType.Bottom)
            {
                return billboard.Position + new Vector3(0, 0, billboard.Size.Y / 2);
            }
            else
            {
                return billboard.Position;
            }
        }

        /// <summary>
        /// Max number of quads that can be rendered in one draw call
        /// </summary>
        public const int ChunkSize = 1024;

        #region Fields
        /// <summary>
        /// Billboard effect
        /// </summary>
        Effect effect;

        /// <summary>
        /// Billboard effect techniques
        /// </summary>
        EffectTechnique techniqueVegetation;
        EffectTechnique techniqueNormal;
        EffectTechnique techniqueCenter;

        /// <summary>
        /// Internal billboard list
        /// </summary>
        List<Billboard> billboards = new List<Billboard>();

        /// <summary>
        /// Quad vertices
        /// </summary>
        DynamicVertexBuffer vertices;

        /// <summary>
        /// Quad indices
        /// </summary>
        DynamicIndexBuffer indices;

        /// <summary>
        /// Vertex buffer used to generate vertices
        /// </summary>
        VertexPositionNormalDuoTexture[] workingVertices =
            new VertexPositionNormalDuoTexture[ChunkSize * 4];

        /// <summary>
        /// Index buffer used to generate indices
        /// </summary>
        Int16[] workingIndices = new short[ChunkSize * 6];

        /// <summary>
        /// Graphics device
        /// </summary>
        BaseGame game;
        #endregion

        #region Methods
        /// <summary>
        /// Create a billboard manager
        /// </summary>
        public BillboardManager(BaseGame game)
        {
            this.game = game;

            // Initialize billboard effect
            effect = game.ZipContent.Load<Effect>("Effects/Billboard");

            techniqueVegetation = effect.Techniques["Vegetation"];
            techniqueNormal = effect.Techniques["Normal"];
            techniqueCenter = effect.Techniques["Center"];

            // Create vertices & indices
            vertices = new DynamicVertexBuffer(game.GraphicsDevice,
                typeof(VertexPositionNormalDuoTexture), ChunkSize * 4, BufferUsage.WriteOnly);

            indices = new DynamicIndexBuffer(game.GraphicsDevice,
                typeof(Int16), ChunkSize * 6, BufferUsage.WriteOnly);
        }

        #region Draw
        /// <summary>
        /// Draw a billboard
        /// </summary>
        public void Draw(Texture2D texture, Vector3 position, Vector2 size, AnchorType achorType)
        {
            Billboard billboard = new Billboard();

            billboard.AchorType = achorType;
            billboard.Texture = texture;
            billboard.Position = position;
            billboard.Normal = Vector3.UnitZ;
            billboard.Size = size;
            billboard.Type = BillboardType.CenterOriented;
            billboard.SourceRectangle = Billboard.DefaultSourceRectangle;

            Draw(billboard);
        }

        public void Draw(Texture2D texture, Vector3 position,
            Vector2 size, Vector3 normal, Vector4 sourceRectangle, AnchorType achorType)
        {
            Billboard billboard = new Billboard() ;

            billboard.AchorType = achorType;
            billboard.Texture = texture;
            billboard.Position = position;
            billboard.Normal = normal;
            billboard.Size = size;
            billboard.SourceRectangle = sourceRectangle;
            billboard.Type = BillboardType.NormalOriented;

            Draw(billboard);
        }

        /// <summary>
        /// Draw a billboard
        /// </summary>
        /// <param name="billboard"></param>
        public void Draw(Billboard billboard)
        {
            billboards.Add(billboard);
        }

        /// <summary>
        /// Draw all billboards in this frame
        /// </summary>
        /// <param name="gameTime"></param>
        public void Present(GameTime gameTime)
        {
            if (billboards.Count <= 0)
                return;

            game.GraphicsDevice.VertexDeclaration = new VertexDeclaration(
                game.GraphicsDevice, VertexPositionNormalDuoTexture.VertexElements);

            BillboardType currentType = BillboardType.NormalOriented;

            // Set effect parameters
            effect.Parameters["View"].SetValue(game.View);
            effect.Parameters["Projection"].SetValue(game.Projection);

            // Make sure normal oriented is 0 and center oriented is 1
            effect.CurrentTechnique = effect.Techniques[0];

            // It's not fast to sort all billboards using texture and distance,
            // we just check if texture is changed. So always draw a bounch of 
            // billboards using the same texture.
            //
            // Divide all billboards into small chunks. A chunk ends when the chunk
            // size reaches MaxChunkSize or when the texture is changed.
            // After the division, we setup effect paramters and render each chunk
            // in one draw call

            Texture2D texture = billboards[0].Texture;

            int baseIndex = 0, baseVertex = 0;
            int begin = 0, end = 0;
            for (int i = 1; i <= billboards.Count; i++)
            {
                // We are at the end of the chunk
                if (i != billboards.Count &&    // End of list
                   (i - begin) < ChunkSize && texture == billboards[i].Texture)
                {
                    continue;
                }

                end = i;
                if (i != billboards.Count)
                    texture = billboards[i].Texture;

                // Setup graphics device
                game.GraphicsDevice.Vertices[0].SetSource(null, 0, 0);
                game.GraphicsDevice.Indices = null;

                // Build the mesh for this chunk of billboards
                baseIndex = baseVertex = 0;
                for (int k = begin; k < end; k++)
                {
                    CreateQuad(billboards[k],
                        ref workingVertices, ref baseVertex, ref workingIndices, ref baseIndex);
                }

                if (baseVertex <= 0 || baseIndex <= 0)
                    continue;

                // Update vertex/index buffer
                vertices.SetData(workingVertices, 0, baseVertex);
                indices.SetData(workingIndices, 0, baseIndex);

                // Setup graphics device
                game.GraphicsDevice.Vertices[0].SetSource(
                    vertices, 0, VertexPositionNormalDuoTexture.SizeInBytes);
                game.GraphicsDevice.Indices = indices;

                // Set effect texture
                effect.Parameters["Texture"].SetValue(billboards[begin].Texture);

                // Set effect technique
                if (billboards[begin].Type != currentType)
                {
                    currentType = billboards[begin].Type;

                    if ((currentType & BillboardType.Vegetation) == BillboardType.Vegetation)
                        effect.CurrentTechnique = techniqueVegetation;
                    else if ((currentType & BillboardType.CenterOriented) == BillboardType.CenterOriented)
                        effect.CurrentTechnique = techniqueCenter;
                    else if ((currentType & BillboardType.NormalOriented) == BillboardType.NormalOriented)
                        effect.CurrentTechnique = techniqueNormal;

                    game.GraphicsDevice.RenderState.DepthBufferEnable = (
                        (currentType & BillboardType.DepthBufferEnable) == BillboardType.DepthBufferEnable);
                }

                // Draw the chunk
                effect.Begin(SaveStateMode.SaveState);
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    game.GraphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList, 0, 0, baseVertex, 0, baseIndex / 3);

                    pass.End();
                }
                effect.End();

                // Increment begin pointer
                begin = end;
            }

            // The billboard effect sets some unusual renderstates for
            // alphablending and depth testing the vegetation. We need to
            // put these back to the right settings for the ground geometry.
            game.GraphicsDevice.RenderState.AlphaBlendEnable = false;
            game.GraphicsDevice.RenderState.AlphaTestEnable = false;
            game.GraphicsDevice.RenderState.DepthBufferEnable = true;
            game.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            game.GraphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;

            // Clear internal list after drawing
            billboards.Clear();
        }

        private static void CreateQuad(Billboard billboard,
            ref VertexPositionNormalDuoTexture[] vertices, ref int baseVertex,
            ref Int16[] indices, ref int baseIndex)
        {
            // Quad:
            //
            // 0 --- 1
            // | \   |
            // |  \  |
            // |   \ |
            // 3 --- 2

            for (int i = 0; i < 4; i++)
            {
                vertices[baseVertex + i].Position = GetCenterPosition(billboard);
                vertices[baseVertex + i].Normal = billboard.Normal;
            }

            // Use UV0 to store source rectangle
            vertices[baseVertex + 3].TextureCoordinate0.X =
            vertices[baseVertex + 0].TextureCoordinate0.X = billboard.SourceRectangle.X;

            vertices[baseVertex + 1].TextureCoordinate0.Y =
            vertices[baseVertex + 0].TextureCoordinate0.Y = billboard.SourceRectangle.Y;

            vertices[baseVertex + 1].TextureCoordinate0.X = 
            vertices[baseVertex + 2].TextureCoordinate0.X = billboard.SourceRectangle.Z;

            vertices[baseVertex + 3].TextureCoordinate0.Y =
            vertices[baseVertex + 2].TextureCoordinate0.Y = billboard.SourceRectangle.W;

            // Use UV1 to store size
            if ((billboard.Type & BillboardType.Vegetation) == BillboardType.Vegetation)
            {
                vertices[baseVertex + 2].TextureCoordinate1.Y =
                vertices[baseVertex + 3].TextureCoordinate1.Y = 0;

                vertices[baseVertex + 0].TextureCoordinate1.Y =
                vertices[baseVertex + 1].TextureCoordinate1.Y = billboard.Size.Y;
            }
            else
            {
                vertices[baseVertex + 2].TextureCoordinate1.Y =
                vertices[baseVertex + 3].TextureCoordinate1.Y = -billboard.Size.Y / 2;

                vertices[baseVertex + 0].TextureCoordinate1.Y =
                vertices[baseVertex + 1].TextureCoordinate1.Y = billboard.Size.Y / 2;
            }

            vertices[baseVertex + 0].TextureCoordinate1.X =
            vertices[baseVertex + 3].TextureCoordinate1.X = -billboard.Size.X / 2;

            vertices[baseVertex + 1].TextureCoordinate1.X =
            vertices[baseVertex + 2].TextureCoordinate1.X = billboard.Size.X / 2;


            // Fill indices
            indices[baseIndex + 0] = (short)(baseVertex + 0);
            indices[baseIndex + 1] = (short)(baseVertex + 1);
            indices[baseIndex + 2] = (short)(baseVertex + 2);
            indices[baseIndex + 3] = (short)(baseVertex + 0);
            indices[baseIndex + 4] = (short)(baseVertex + 2);
            indices[baseIndex + 5] = (short)(baseVertex + 3);

            // Increment base vertex/index
            baseVertex += 4;
            baseIndex += 6;
        }
        #endregion

        #endregion

        #region Dispose

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (effect != null)
                    effect.Dispose();

                if (vertices != null)
                    vertices.Dispose();

                if (indices != null)
                    indices.Dispose();
            }
        }

        #endregion
    }
    #endregion
}
