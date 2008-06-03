using System;
using System.Collections.Generic;
using System.Text;
using Isles.Engine;
using Isles.Graphics;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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
        private int elapsedTime = 0;

        /// <summary>
        /// The span to update the blocks
        /// </summary>
        private int updateSpan = 0;

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
        private float halfWidth = 0;

        /// <summary>
        /// 
        /// </summary>
        bool animationStarted = false;

        /// <summary>
        /// 
        /// </summary>
        float alpha = 1;


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
        /// 
        /// </summary>
        public VertexPositionTexture LeadVertex1
        {
            get
            {
                return this.leadVertex1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public VertexPositionTexture LeadVertex2
        {
            get
            {
                return this.leadVertex2;
            }
        }

        /// <summary>
        /// The width of the trail
        /// </summary>
        public float Width
        {
            get
            {
                return this.halfWidth * 2;
            }
            set
            {
                this.halfWidth = value / 2;
            }
        }


        /// <summary>
        /// The number of blocks owned by the trail
        /// </summary>
        public int Length
        {
            get
            {
                return this.length;
            }
            set
            {
                this.length = value;
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

        /// <summary>
        /// View Matrix
        /// </summary>
        public Matrix View
        {
            get
            {
                return this.view;
            }
        }

        /// <summary>
        /// Projection Matrix
        /// </summary>
        public Matrix Projection
        {
            get
            {
                return this.projection;
            }
        }

        /// <summary>
        /// Gets or sets the alpha property of trail
        /// </summary>
        public float Alpha
        {
            get
            {
                return this.alpha;
            }
            set
            {
                this.alpha = value;
            }
        }

        #endregion

        #region method

        public TrailEffect()
        {
            this.trailQueue = new Queue<VertexPositionTexture>();
            this.updateSpan = 50;
        }

        public void Launch()
        {
            this.animationStarted = true;
        }


        public void SetCamera(Matrix view, Matrix projection)
        {
            this.view = view;
            this.projection = projection;
        }

        public void UpdateLeadingVertex()
        {
            Vector3 vec = this.position - this.lastPosition;
            if (vec != Vector3.Zero)
            {
                float dy = this.halfWidth / (float)Math.Sqrt((vec.Y / vec.X) * (vec.Y / vec.X) + 1);
                float dx = dy * vec.Y / vec.X;

                this.leadVertex1 = new VertexPositionTexture();
                this.leadVertex1.Position.X = this.Position.X + dx;
                this.leadVertex1.Position.Y = this.Position.Y + dy;
                this.leadVertex1.Position.Z = this.Position.Z;

                this.leadVertex2 = new VertexPositionTexture();
                this.leadVertex2.Position.X = this.Position.X - dx;
                this.leadVertex2.Position.Y = this.Position.Y - dy;
                this.leadVertex2.Position.Z = this.Position.Z;
            }
        }

        public void Update(GameTime gameTime)
        {
            if (this.animationStarted)
            {
                this.UpdateLeadingVertex();
                this.elapsedTime += gameTime.ElapsedGameTime.Milliseconds;
                if (this.elapsedTime > this.updateSpan)
                {
                    this.elapsedTime = 0;
                    this.UpdateQueue();
                }
            }
        }

        private void UpdateQueue()
        {
            this.trailQueue.Enqueue(this.leadVertex1);
            this.trailQueue.Enqueue(this.leadVertex2);
            if (this.trailQueue.Count > this.Length * 2)
            {
                //2 vertex should be quited from queue at a time
                this.trailQueue.Dequeue();
                this.trailQueue.Dequeue();
            }
            this.lastPosition = new Vector3(this.position.X, this.position.Y, this.position.Z);
        }

        public void Draw(GameTime gameTime)
        {
            if (this.trailQueue.Count >= 4)
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


        private List<TrailEffect> trailList;

        /// <summary>
        /// Vertexes
        /// </summary>
        private VertexPositionTexture[] vertexList;

        /// <summary>
        /// Declaration
        /// </summary>
        private VertexDeclaration vertexDeclaration;

        /// <summary>
        /// VertexBuffer
        /// </summary>
        private DynamicVertexBuffer vertexBuffer;

        /// <summary>
        /// indexbiffer
        /// </summary>
        private DynamicIndexBuffer indexBuffer;

        /// <summary>
        /// Graphics Device
        /// </summary>
        private GraphicsDevice device;

        /// <summary>
        /// Content
        /// </summary>
        private ContentManager content;


        /// <summary>
        /// effect
        /// </summary>
        private BasicEffect effect;

        private float updateSpan = 0;

        private int chunckSize = 100;

        #endregion

        #region property



        /// <summary>
        /// Time span to update the blocks
        /// </summary>
        public float UpdateSpan
        {
            get
            {
                return this.updateSpan;
            }
            set
            {
                this.updateSpan = value;
            }
        }



        #endregion

        #region method

        /// <summary>
        /// Construction method
        /// </summary>
        public TrailEffectManager()
        {
            this.device = BaseGame.Singleton.GraphicsDevice;
            this.content = BaseGame.Singleton.ZipContent;
            this.trailList = new List<TrailEffect>();
            this.vertexDeclaration = new VertexDeclaration(this.device, VertexPositionTexture.VertexElements);
            this.vertexBuffer = new DynamicVertexBuffer(this.device, typeof(VertexPositionTexture), (this.chunckSize + 1) * 2, BufferUsage.WriteOnly);
            this.indexBuffer = new DynamicIndexBuffer(this.device, typeof(Int16), (this.chunckSize + 1) * 2, BufferUsage.WriteOnly);
            this.vertexList = new VertexPositionTexture[(this.chunckSize + 1) * 2];

            this.LoadContent();
        }

        public void LoadContent()
        {
            //this.device.VertexDeclaration = this.vertexDeclaration;
            this.effect = new BasicEffect(this.device, null);
        }


        #region update & draw
        public void Update(GameTime gameTime)
        {
        }

        public void Draw(TrailEffect trail)
        {
            this.trailList.Add(trail);
        }

        public void Present(GameTime gameTime)
        {
            foreach (TrailEffect trail in this.trailList)
            {
                this.device.Vertices[0].SetSource(null, 0, 0);

                this.UpdateVertexList(trail);
                this.BindingTexture(trail);

                this.vertexBuffer.SetData<VertexPositionTexture>(this.vertexList, 0, trail.trailQueue.Count + 2);
                this.device.Vertices[0].SetSource(this.vertexBuffer, 0, VertexPositionTexture.SizeInBytes);
                this.device.VertexDeclaration = this.vertexDeclaration;

                this.effect.Alpha = trail.Alpha;
                this.effect.Texture = trail.Texture;
                this.effect.View = trail.View;
                this.effect.Projection = trail.Projection;
                this.effect.World = Matrix.Identity;
                this.effect.TextureEnabled = true;

                this.device.RenderState.AlphaBlendEnable = true;
                this.device.RenderState.SourceBlend = Blend.SourceAlpha;
                this.device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

                this.effect.Begin();

                this.effect.CurrentTechnique.Passes[0].Begin();

                this.device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, trail.trailQueue.Count);

                this.effect.CurrentTechnique.Passes[0].End();

                this.effect.End();
            }
            this.trailList.Clear();
        }

        private void BindingTexture(TrailEffect trail)
        {
            if (trail.trailQueue.Count % 2 == 0)
            {
                float df = 1.0f / (trail.trailQueue.Count / 2);
                float sumy = 0;
                for (int i = 0; i < trail.trailQueue.Count + 2; i = i + 2)
                {
                    this.vertexList[i].TextureCoordinate.X = 0;
                    this.vertexList[i].TextureCoordinate.Y = sumy;
                    this.vertexList[i + 1].TextureCoordinate.X = 1;
                    this.vertexList[i + 1].TextureCoordinate.Y = sumy;
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
            int vertexCount = trail.trailQueue.Count;
            this.vertexList[0] = trail.LeadVertex1;
            this.vertexList[1] = trail.LeadVertex2;
            VertexPositionTexture[] tempList = new VertexPositionTexture[vertexCount];
            trail.trailQueue.CopyTo(tempList, 0);
            for (int i = 0; i < vertexCount; i++)
            {
                this.vertexList[2 + i] = tempList[vertexCount - i - 1];
            }

        }
        #endregion

        #endregion
    }
    #endregion
}
