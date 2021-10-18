using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    #region TrailEffect

    public class TrailEffect
    {
        #region variables
        /// <summary>
        /// The queue
        /// </summary>
        public Queue<VertexPositionTexture> trailQueue;
        /// <summary>
        /// The ealpsed time
        /// </summary>
        private int elapsedTime;

        /// <summary>
        /// The span to update the blocks
        /// </summary>
        private readonly int updateSpan;

        /// <summary>
        /// Leading Position of the trail
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// The leading vertex position
        /// </summary>
        private VertexPositionTexture leadVertex1;
        private VertexPositionTexture leadVertex2;

        /// <summary>
        /// The Last Position
        /// </summary>
        private Vector3 lastPosition;

        /// <summary>
        /// Number of blocks
        /// </summary>
        private int length;

        /// <summary>
        /// 
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// 
        /// </summary>
        private float halfWidth;

        /// <summary>
        /// 
        /// </summary>
        private bool animationStarted;

        /// <summary>
        /// 
        /// </summary>
        private float alpha = 1;

        /// <summary>
        /// ViewMatrix
        /// </summary>
        private Matrix view;

        /// <summary>
        /// Projection Matrix
        /// </summary>
        private Matrix projection;

        #endregion

        #region property

        /// <summary>
        /// The poisiton here means the leading position of the trail
        /// </summary>
        public Vector3 Position
        {
            get => position;
            set => position = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public VertexPositionTexture LeadVertex1 => leadVertex1;

        /// <summary>
        /// 
        /// </summary>
        public VertexPositionTexture LeadVertex2 => leadVertex2;

        /// <summary>
        /// The width of the trail
        /// </summary>
        public float Width
        {
            get => halfWidth * 2;
            set => halfWidth = value / 2;
        }

        /// <summary>
        /// The number of blocks owned by the trail
        /// </summary>
        public int Length
        {
            get => length;
            set => length = value;
        }

        public Texture2D Texture
        {
            get => texture;
            set => texture = value;
        }

        /// <summary>
        /// View Matrix
        /// </summary>
        public Matrix View => view;

        /// <summary>
        /// Projection Matrix
        /// </summary>
        public Matrix Projection => projection;

        /// <summary>
        /// Gets or sets the alpha property of trail
        /// </summary>
        public float Alpha
        {
            get => alpha;
            set => alpha = value;
        }

        #endregion

        #region method

        public TrailEffect()
        {
            trailQueue = new Queue<VertexPositionTexture>();
            updateSpan = 50;
        }

        public void Launch()
        {
            animationStarted = true;
        }

        public void SetCamera(Matrix view, Matrix projection)
        {
            this.view = view;
            this.projection = projection;
        }

        public void UpdateLeadingVertex()
        {
            Vector3 vec = position - lastPosition;
            if (vec != Vector3.Zero)
            {
                var dy = halfWidth / (float)Math.Sqrt((vec.Y / vec.X) * (vec.Y / vec.X) + 1);
                var dx = dy * vec.Y / vec.X;

                leadVertex1 = new VertexPositionTexture();
                leadVertex1.Position.X = Position.X + dx;
                leadVertex1.Position.Y = Position.Y + dy;
                leadVertex1.Position.Z = Position.Z;

                leadVertex2 = new VertexPositionTexture();
                leadVertex2.Position.X = Position.X - dx;
                leadVertex2.Position.Y = Position.Y - dy;
                leadVertex2.Position.Z = Position.Z;
            }
        }

        public void Update(GameTime gameTime)
        {
            if (animationStarted)
            {
                UpdateLeadingVertex();
                elapsedTime += gameTime.ElapsedGameTime.Milliseconds;
                if (elapsedTime > updateSpan)
                {
                    elapsedTime = 0;
                    UpdateQueue();
                }
            }
        }

        private void UpdateQueue()
        {
            trailQueue.Enqueue(leadVertex1);
            trailQueue.Enqueue(leadVertex2);
            if (trailQueue.Count > Length * 2)
            {
                //2 vertex should be quited from queue at a time
                trailQueue.Dequeue();
                trailQueue.Dequeue();
            }
            lastPosition = new Vector3(position.X, position.Y, position.Z);
        }

        public void Draw(GameTime gameTime)
        {
            if (trailQueue.Count >= 4)
            {
                //TODO: draw code add here
                //BaseGame.Singleton.TrailEffect.Draw(this);
            }
        }

        #endregion
    }

    #endregion

    #region TrailEffectManager

    public class TrailEffectManager
    {
        //-----TrailEffectOutline----
        //1-We'll keep a path of the 
        //arrow going as a queue of a 
        //fix length
        //2- The new key point was 
        //determined by current position 
        //and last position the last 
        //position will be update with a
        //span of time
        //
        //
        //
        //---------------------------
        #region variable

        private readonly List<TrailEffect> trailList;

        /// <summary>
        /// Vertexes
        /// </summary>
        private readonly VertexPositionTexture[] vertexList;

        /// <summary>
        /// Declaration
        /// </summary>
        private readonly VertexDeclaration vertexDeclaration;

        /// <summary>
        /// VertexBuffer
        /// </summary>
        private readonly DynamicVertexBuffer vertexBuffer;

        /// <summary>
        /// indexbiffer
        /// </summary>
        private readonly DynamicIndexBuffer indexBuffer;

        /// <summary>
        /// Graphics Device
        /// </summary>
        private readonly GraphicsDevice device;

        /// <summary>
        /// Content
        /// </summary>
        private readonly ContentManager content;

        /// <summary>
        /// effect
        /// </summary>
        private BasicEffect effect;

        private float updateSpan;

        private readonly int chunckSize = 100;

        #endregion

        #region property

        /// <summary>
        /// Time span to update the blocks
        /// </summary>
        public float UpdateSpan
        {
            get => updateSpan;
            set => updateSpan = value;
        }

        #endregion

        #region method

        /// <summary>
        /// Construction method
        /// </summary>
        public TrailEffectManager()
        {
            device = BaseGame.Singleton.GraphicsDevice;
            content = BaseGame.Singleton.ZipContent;
            trailList = new List<TrailEffect>();
            vertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);
            vertexBuffer = new DynamicVertexBuffer(device, typeof(VertexPositionTexture), (chunckSize + 1) * 2, BufferUsage.WriteOnly);
            indexBuffer = new DynamicIndexBuffer(device, typeof(Int16), (chunckSize + 1) * 2, BufferUsage.WriteOnly);
            vertexList = new VertexPositionTexture[(chunckSize + 1) * 2];

            LoadContent();
        }

        public void LoadContent()
        {
            //this.device.VertexDeclaration = this.vertexDeclaration;
            effect = new BasicEffect(device, null);
        }

        #region update & draw
        public void Update(GameTime gameTime)
        {
        }

        public void Draw(TrailEffect trail)
        {
            trailList.Add(trail);
        }

        public void Present(GameTime gameTime)
        {
            foreach (TrailEffect trail in trailList)
            {
                device.Vertices[0].SetSource(null, 0, 0);

                UpdateVertexList(trail);
                BindingTexture(trail);

                vertexBuffer.SetData(vertexList, 0, trail.trailQueue.Count + 2);
                device.Vertices[0].SetSource(vertexBuffer, 0, VertexPositionTexture.SizeInBytes);
                device.VertexDeclaration = vertexDeclaration;

                effect.Alpha = trail.Alpha;
                effect.Texture = trail.Texture;
                effect.View = trail.View;
                effect.Projection = trail.Projection;
                effect.World = Matrix.Identity;
                effect.TextureEnabled = true;

                device.RenderState.AlphaBlendEnable = true;
                device.RenderState.SourceBlend = Blend.SourceAlpha;
                device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

                effect.Begin();

                effect.CurrentTechnique.Passes[0].Begin();

                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, trail.trailQueue.Count);

                effect.CurrentTechnique.Passes[0].End();

                effect.End();
            }
            trailList.Clear();
        }

        private void BindingTexture(TrailEffect trail)
        {
            if (trail.trailQueue.Count % 2 == 0)
            {
                var df = 1.0f / (trail.trailQueue.Count / 2);
                float sumy = 0;
                for (var i = 0; i < trail.trailQueue.Count + 2; i = i + 2)
                {
                    vertexList[i].TextureCoordinate.X = 0;
                    vertexList[i].TextureCoordinate.Y = sumy;
                    vertexList[i + 1].TextureCoordinate.X = 1;
                    vertexList[i + 1].TextureCoordinate.Y = sumy;
                    sumy += df;
                }
            }
            else
            {
                throw new Exception();
            }
        }

        public void UpdateVertexList(TrailEffect trail)
        {
            var vertexCount = trail.trailQueue.Count;
            vertexList[0] = trail.LeadVertex1;
            vertexList[1] = trail.LeadVertex2;
            var tempList = new VertexPositionTexture[vertexCount];
            trail.trailQueue.CopyTo(tempList, 0);
            for (var i = 0; i < vertexCount; i++)
            {
                vertexList[2 + i] = tempList[vertexCount - i - 1];
            }

        }
        #endregion

        #endregion
    }
    #endregion
}
