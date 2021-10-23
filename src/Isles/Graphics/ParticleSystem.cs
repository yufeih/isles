// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Isles.Graphics
{
    /// <summary>
    /// Settings class describes all the tweakable options used
    /// to control the appearance of a particle system.
    /// </summary>
    /// <example>
    /// ParticleSystem      -   Duplication of single type of particle
    /// ParticleSettings    -   Description of a single particle
    /// ParticleEmitter     -   Add new particles of any particle system
    /// ParticleEffect      -   Base class for all particle system effects
    ///
    /// ParticleEffect : IWorldObject
    /// world.Create("Fireball");
    ///
    /// new ParticleSystem(new ParticleSettings()).
    /// </example>
    public class ParticleSettings
    {
        /// <summary>
        /// Name of the texture used by this particle system.
        /// </summary>
        public string TextureName { get; set; }

        /// <summary>
        /// Maximum number of particles that can be displayed at one time.
        /// </summary>
        public int MaxParticles { get; set; } = 100;

        /// <summary>
        /// How long these particles will last.
        /// Double is used to represent time for xml serialization.
        /// </summary>
        public float Duration { get; set; } = 1;

        /// <summary>
        /// If greater than zero, some particles will last a shorter time than others.
        /// </summary>
        public float DurationRandomness { get; set; }

        /// <summary>
        /// Controls how much particles are influenced by the velocity of the object
        /// which created them. You can see this in action with the explosion effect,
        /// where the flames continue to move in the same direction as the source
        /// projectile. The projectile trail particles, on the other hand, set this
        /// value very low so they are less affected by the velocity of the projectile.
        /// </summary>
        public float EmitterVelocitySensitivity { get; set; } = 1;

        /// <summary>
        /// Range of values controlling how much X and Z axis velocity to give each
        /// particle. Values for individual particles are randomly chosen from somewhere
        /// between these limits.
        /// </summary>
        public float MinHorizontalVelocity { get; set; }
        public float MaxHorizontalVelocity { get; set; }

        /// <summary>
        /// Range of values controlling how much Y axis velocity to give each particle.
        /// Values for individual particles are randomly chosen from somewhere between
        /// these limits.
        /// </summary>
        public float MinVerticalVelocity { get; set; }
        public float MaxVerticalVelocity { get; set; }

        /// <summary>
        /// Direction and strength of the gravity effect. Note that this can point in any
        /// direction, not just down! The fire effect points it upward to make the flames
        /// rise, and the smoke plume points it sideways to simulate wind.
        /// </summary>
        public Vector3 Gravity { get; set; }

        /// <summary>
        /// Controls how the particle velocity will change over their lifetime. If set
        /// to 1, particles will keep going at the same speed as when they were created.
        /// If set to 0, particles will come to a complete stop right before they die.
        /// Values greater than 1 make the particles speed up over time.
        /// </summary>
        public float EndVelocity { get; set; } = 1;

        /// <summary>
        /// Range of values controlling the particle color and alpha. Values for
        /// individual particles are randomly chosen from somewhere between these limits.
        /// </summary>
        public Color MinColor { get; set; } = Color.White;
        public Color MaxColor { get; set; } = Color.White;

        /// <summary>
        /// Range of values controlling how fast the particles rotate. Values for
        /// individual particles are randomly chosen from somewhere between these
        /// limits. If both these values are set to 0, the particle system will
        /// automatically switch to an alternative shader technique that does not
        /// support rotation, and thus requires significantly less GPU power. This
        /// means if you don't need the rotation effect, you may get a performance
        /// boost from leaving these values at 0.
        /// </summary>
        public float MinRotateSpeed { get; set; }
        public float MaxRotateSpeed { get; set; }

        /// <summary>
        /// Range of values controlling how big the particles are when first created.
        /// Values for individual particles are randomly chosen from somewhere between
        /// these limits.
        /// </summary>
        public float MinStartSize { get; set; } = 100;
        public float MaxStartSize { get; set; } = 100;

        /// <summary>
        /// Range of values controlling how big particles become at the end of their
        /// life. Values for individual particles are randomly chosen from somewhere
        /// between these limits.
        /// </summary>
        public float MinEndSize { get; set; } = 100;
        public float MaxEndSize { get; set; } = 100;

        public bool Additive { get; set; } = true;
    }

    /// <summary>
    /// The main component in charge of displaying particles.
    /// </summary>
    public class ParticleSystem
    {
        public string ParticleName { get; set; }

        /// <summary>
        /// Gets or sets the settings for this particle system.
        /// </summary>
        public ParticleSettings Settings { get; }

        // For accessing view projection matrix
        private readonly Game game;

        private Texture2D texture;

        // Custom effect for drawing particles. This computes the particle
        // animation entirely in the vertex shader: no per-particle CPU work required!
        private Effect particleEffect;

        // Shortcuts for accessing frequently changed effect parameters.
        private EffectParameter effectViewParameter;
        private EffectParameter effectProjectionParameter;
        private EffectParameter effectViewportScaleParameter;
        private EffectParameter effectTimeParameter;

        // An array of particles, treated as a circular queue.
        private ParticleVertex[] particles;

        // A vertex buffer holding our particles. This contains the same data as
        // the particles array, but copied across to where the GPU can access it.
        private DynamicVertexBuffer vertexBuffer;

        // Vertex declaration describes the format of our ParticleVertex structure.
        private VertexDeclaration vertexDeclaration;

        // Index buffer turns sets of four vertices into particle quads (pairs of triangles).
        IndexBuffer indexBuffer;

        // The particles array and vertex buffer are treated as a circular queue.
        // Initially, the entire contents of the array are free, because no particles
        // are in use. When a new particle is created, this is allocated from the
        // beginning of the array. If more than one particle is created, these will
        // always be stored in a consecutive block of array elements. Because all
        // particles last for the same amount of time, old particles will always be
        // removed in order from the start of this active particle region, so the
        // active and free regions will never be intermingled. Because the queue is
        // circular, there can be times when the active particle region wraps from the
        // end of the array back to the start. The queue uses modulo arithmetic to
        // handle these cases. For instance with a four entry queue we could have:
        //
        //      0
        //      1 - first active particle
        //      2
        //      3 - first free particle
        //
        // In this case, particles 1 and 2 are active, while 3 and 4 are free.
        // Using modulo arithmetic we could also have:
        //
        //      0
        //      1 - first free particle
        //      2
        //      3 - first active particle
        //
        // Here, 3 and 0 are active, while 1 and 2 are free.
        //
        // But wait! The full story is even more complex.
        //
        // When we create a new particle, we add them to our managed particles array.
        // We also need to copy this new data into the GPU vertex buffer, but we don't
        // want to do that straight away, because setting new data into a vertex buffer
        // can be an expensive operation. If we are going to be adding several particles
        // in a single frame, it is faster to initially just store them in our managed
        // array, and then later upload them all to the GPU in one single call. So our
        // queue also needs a region for storing new particles that have been added to
        // the managed array but not yet uploaded to the vertex buffer.
        //
        // Another issue occurs when old particles are retired. The CPU and GPU run
        // asynchronously, so the GPU will often still be busy drawing the previous
        // frame while the CPU is working on the next frame. This can cause a
        // synchronization problem if an old particle is retired, and then immediately
        // overwritten by a new one, because the CPU might try to change the contents
        // of the vertex buffer while the GPU is still busy drawing the old data from
        // it. Normally the graphics driver will take care of this by waiting until
        // the GPU has finished drawing inside the VertexBuffer.SetData call, but we
        // don't want to waste time waiting around every time we try to add a new
        // particle! To avoid this delay, we can specify the SetDataOptions.NoOverwrite
        // flag when we write to the vertex buffer. This basically means "I promise I
        // will never try to overwrite any data that the GPU might still be using, so
        // you can just go ahead and update the buffer straight away". To keep this
        // promise, we must avoid reusing vertices immediately after they are drawn.
        //
        // So in total, our queue contains four different regions:
        //
        // Vertices between firstActiveParticle and firstNewParticle are actively
        // being drawn, and exist in both the managed particles array and the GPU
        // vertex buffer.
        //
        // Vertices between firstNewParticle and firstFreeParticle are newly created,
        // and exist only in the managed particles array. These need to be uploaded
        // to the GPU at the start of the next draw call.
        //
        // Vertices between firstFreeParticle and firstRetiredParticle are free and
        // waiting to be allocated.
        //
        // Vertices between firstRetiredParticle and firstActiveParticle are no longer
        // being drawn, but were drawn recently enough that the GPU could still be
        // using them. These need to be kept around for a few more frames before they
        // can be reallocated.
        private int firstActiveParticle;
        private int firstNewParticle;
        private int firstFreeParticle;
        private int firstRetiredParticle;

        // Store the current time, in seconds.
        private float currentTime;

        // Count how many times Draw has been called. This is used to know
        // when it is safe to retire old particles back into the free list.
        private int drawCounter;

        // Shared random number generator.
        private static readonly Random random = new();

        private static BaseGame baseGame;
        private static Dictionary<string, ParticleSystem> ParticleSystems;

        /// <summary>
        /// Initialize particle system.
        /// </summary>
        public static void LoadContent(BaseGame game)
        {
            baseGame = game;

            var particleSettings = JsonSerializer.Deserialize<Dictionary<string, ParticleSettings>>(
                    File.ReadAllText("data/settings/particles.json"),
                    new JsonSerializerOptions { IncludeFields = true });

            ParticleSystems = particleSettings.ToDictionary(settings => settings.Key, settings => new ParticleSystem(game, settings.Value));
        }

        /// <summary>
        /// Creates a new particle system with the specified type.
        /// </summary>
        public static ParticleSystem Create(string type)
        {
            return ParticleSystems[type];
        }

        /// <summary>
        /// Flush all the particle system effects onto the screen.
        /// </summary>
        /// <param name="gameTime"></param>
        public static void Present()
        {
            foreach (ParticleSystem ps in ParticleSystems.Values)
            {
                ps.SetCamera(baseGame.View, baseGame.Projection);
                ps.Draw();
            }
        }

        /// <summary>
        /// Update all particle systems.
        /// </summary>
        public static void UpdateAll(GameTime gameTime)
        {
            foreach (ParticleSystem ps in ParticleSystems.Values)
            {
                ps.Update(gameTime);
            }
        }

        private ParticleSystem(Game game, ParticleSettings settings)
        {
            this.game = game;

            Settings = settings;

            LoadParticleEffect();
            LoadContent();
        }

        /// <summary>
        /// Loads graphics for the particle system.
        /// </summary>
        protected void LoadContent()
        {
            vertexDeclaration = new VertexDeclaration(game.GraphicsDevice,
                                                      ParticleVertex.VertexElements);

            // Allocate the particle array, and fill in the corner fields (which never change).
            particles = new ParticleVertex[Settings.MaxParticles * 4];

            for (var i = 0; i < Settings.MaxParticles; i++)
            {
                particles[i * 4 + 0].Corner = new Short2(-1, -1);
                particles[i * 4 + 1].Corner = new Short2(1, -1);
                particles[i * 4 + 2].Corner = new Short2(1, 1);
                particles[i * 4 + 3].Corner = new Short2(-1, 1);
            }

            // Create a dynamic vertex buffer.
            vertexBuffer = new DynamicVertexBuffer(game.GraphicsDevice, typeof(ParticleVertex), particles.Length * 4, BufferUsage.WriteOnly);

            // Initialize the vertex buffer contents. This is necessary in order
            // to correctly restore any existing particles after a lost device.
            vertexBuffer.SetData(particles);

            // Create and populate the index buffer.
            var indices = new ushort[Settings.MaxParticles * 6];

            for (var i = 0; i < Settings.MaxParticles; i++)
            {
                indices[i * 6 + 0] = (ushort)(i * 4 + 0);
                indices[i * 6 + 1] = (ushort)(i * 4 + 1);
                indices[i * 6 + 2] = (ushort)(i * 4 + 2);

                indices[i * 6 + 3] = (ushort)(i * 4 + 0);
                indices[i * 6 + 4] = (ushort)(i * 4 + 2);
                indices[i * 6 + 5] = (ushort)(i * 4 + 3);
            }

            indexBuffer = new IndexBuffer(game.GraphicsDevice, typeof(ushort), indices.Length, BufferUsage.WriteOnly);

            indexBuffer.SetData(indices);
        }

        /// <summary>
        /// Helper for loading and initializing the particle effect.
        /// </summary>
        private void LoadParticleEffect()
        {
            particleEffect = game.Content.Load<Effect>("Effects/ParticleEffect");

            var parameters = particleEffect.Parameters;

            // Look up shortcuts for parameters that change every frame.
            effectViewParameter = parameters["View"];
            effectProjectionParameter = parameters["Projection"];
            effectViewportScaleParameter = parameters["ViewportScale"];
            effectTimeParameter = parameters["CurrentTime"];

            // Load the particle texture, and set it onto the effect.
            texture = game.Content.Load<Texture2D>(Settings.TextureName);
        }

        private void SetParameters()
        {
            var parameters = particleEffect.Parameters;

            // Set the values of parameters that do not change.
            parameters["Duration"].SetValue((float)Settings.Duration);
            parameters["DurationRandomness"].SetValue(Settings.DurationRandomness);
            parameters["Gravity"].SetValue(Settings.Gravity);
            parameters["EndVelocity"].SetValue(Settings.EndVelocity);
            parameters["MinColor"].SetValue(Settings.MinColor.ToVector4());
            parameters["MaxColor"].SetValue(Settings.MaxColor.ToVector4());

            parameters["RotateSpeed"].SetValue(
                new Vector2(Settings.MinRotateSpeed, Settings.MaxRotateSpeed));

            parameters["StartSize"].SetValue(
                new Vector2(Settings.MinStartSize, Settings.MaxStartSize));

            parameters["EndSize"].SetValue(
                new Vector2(Settings.MinEndSize, Settings.MaxEndSize));

            parameters["Texture"].SetValue(texture);
        }

        private bool presented;

        /// <summary>
        /// Updates the particle system.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            RetireActiveParticles();
            FreeRetiredParticles();

            // If we let our timer go on increasing for ever, it would eventually
            // run out of floating point precision, at which point the particles
            // would render incorrectly. An easy way to prevent this is to notice
            // that the time value doesn't matter when no particles are being drawn,
            // so we can reset it back to zero any time the active queue is empty.
            if (firstActiveParticle == firstFreeParticle)
            {
                currentTime = 0;
            }

            if (firstRetiredParticle == firstActiveParticle)
            {
                drawCounter = 0;
            }

            presented = false;
        }

        /// <summary>
        /// Helper for checking when active particles have reached the end of
        /// their life. It moves old particles from the active area of the queue
        /// to the retired section.
        /// </summary>
        private void RetireActiveParticles()
        {
            var particleDuration = (float)Settings.Duration;

            while (firstActiveParticle != firstNewParticle)
            {
                // Is this particle old enough to retire?
                // We multiply the active particle index by four, because each
                // particle consists of a quad that is made up of four vertices.
                var particleAge = currentTime - particles[firstActiveParticle * 4].Time;

                if (particleAge < particleDuration)
                {
                    break;
                }

                // Remember the time at which we retired this particle.
                particles[firstActiveParticle * 4].Time = drawCounter;

                // Move the particle from the active to the retired queue.
                firstActiveParticle++;

                if (firstActiveParticle >= Settings.MaxParticles)
                {
                    firstActiveParticle = 0;
                }
            }
        }

        /// <summary>
        /// Helper for checking when retired particles have been kept around long
        /// enough that we can be sure the GPU is no longer using them. It moves
        /// old particles from the retired area of the queue to the free section.
        /// </summary>
        private void FreeRetiredParticles()
        {
            while (firstRetiredParticle != firstActiveParticle)
            {
                // Has this particle been unused long enough that
                // the GPU is sure to be finished with it?
                // We multiply the retired particle index by four, because each
                // particle consists of a quad that is made up of four vertices.
                var age = drawCounter - (int)particles[firstRetiredParticle * 4].Time;

                // The GPU is never supposed to get more than 2 frames behind the CPU.
                // We add 1 to that, just to be safe in case of buggy drivers that
                // might bend the rules and let the GPU get further behind.
                if (age < 3)
                {
                    break;
                }

                // Move the particle from the retired to the free queue.
                firstRetiredParticle++;

                if (firstRetiredParticle >= Settings.MaxParticles)
                {
                    firstRetiredParticle = 0;
                }
            }
        }

        /// <summary>
        /// Draws the particle system.
        /// </summary>
        public void Draw()
        {
            if (presented)
            {
                return;
            }

            // Be sure update is always called before draw is called
            presented = true;

            GraphicsDevice device = game.GraphicsDevice;

            // If there are any particles waiting in the newly added queue,
            // we'd better upload them to the GPU ready for drawing.
            if (firstNewParticle != firstFreeParticle)
            {
                AddNewParticlesToVertexBuffer();
            }

            // If there are any active particles, draw them now!
            if (firstActiveParticle != firstFreeParticle)
            {
                // Setup view projection matrix
                effectViewParameter.SetValue(view);
                effectProjectionParameter.SetValue(projection);

                SetParameters();

                SetParticleRenderStates(device);

                // Set an effect parameter describing the viewport size. This is
                // needed to convert particle sizes into screen space point sizes.
                effectViewportScaleParameter.SetValue(new Vector2(0.5f / device.Viewport.AspectRatio, -0.5f));

                // Set an effect parameter describing the current time. All the vertex
                // shader particle animation is keyed off this value.
                effectTimeParameter.SetValue(currentTime);

                // Set the particle vertex buffer and vertex declaration.
                device.Vertices[0].SetSource(vertexBuffer, 0,
                                             ParticleVertex.SizeInBytes);

                device.VertexDeclaration = vertexDeclaration;

                device.Indices = indexBuffer;

                // Activate the particle effect.
                particleEffect.Begin();

                foreach (EffectPass pass in particleEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();

                    if (firstActiveParticle < firstFreeParticle)
                    {
                        // If the active particles are all in one consecutive range,
                        // we can draw them all in a single call.
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                            firstActiveParticle * 4, (firstFreeParticle - firstActiveParticle) * 4,
                            firstActiveParticle * 6, (firstFreeParticle - firstActiveParticle) * 2);
                    }
                    else
                    {
                        // If the active particle range wraps past the end of the queue
                        // back to the start, we must split them over two draw calls.
                        device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                            firstActiveParticle * 4, (Settings.MaxParticles - firstActiveParticle) * 4,
                            firstActiveParticle * 6, (Settings.MaxParticles - firstActiveParticle) * 2);

                        if (firstFreeParticle > 0)
                        {
                            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
                                0, firstFreeParticle * 4,
                                0, firstFreeParticle * 2);
                        }
                    }

                    pass.End();
                }

                particleEffect.End();
            }

            drawCounter++;
        }

        /// <summary>
        /// Helper for uploading new particles from our managed
        /// array to the GPU vertex buffer.
        /// </summary>
        private void AddNewParticlesToVertexBuffer()
        {
            var stride = ParticleVertex.SizeInBytes;

            if (firstNewParticle < firstFreeParticle)
            {
                // If the new particles are all in one consecutive range,
                // we can upload them all in a single call.
                vertexBuffer.SetData(firstNewParticle * stride * 4, particles,
                                     firstNewParticle * 4,
                                     (firstFreeParticle - firstNewParticle) * 4, stride);
            }
            else
            {
                // If the new particle range wraps past the end of the queue
                // back to the start, we must split them over two upload calls.
                vertexBuffer.SetData(firstNewParticle * stride * 4, particles,
                                     firstNewParticle * 4,
                                     (Settings.MaxParticles - firstNewParticle) * 4, stride);

                if (firstFreeParticle > 0)
                {
                    vertexBuffer.SetData(0, particles, 0, firstFreeParticle * 4, stride);
                }
            }

            // Move the particles we just uploaded from the new to the active queue.
            firstNewParticle = firstFreeParticle;
        }

        /// <summary>
        /// Helper for setting the renderstates used to draw particles.
        /// </summary>
        private void SetParticleRenderStates(GraphicsDevice device)
        {
            // Set the alpha blend mode.
            // Enable the depth buffer (so particles will not be visible through
            // solid objects like the ground plane), but disable depth writes
            // (so particles will not obscure other particles).
            device.SetRenderState(Settings.Additive ? BlendState.Additive : BlendState.AlphaBlend, DepthStencilState.DepthRead);
        }

        private Matrix view;
        private Matrix projection;

        public void SetCamera(Matrix view, Matrix projection)
        {
            this.view = view;
            this.projection = projection;
        }

        /// <summary>
        /// Adds a new particle to the system.
        /// </summary>
        public void AddParticle(Vector3 position, Vector3 velocity)
        {
            // Figure out where in the circular queue to allocate the new particle.
            var nextFreeParticle = firstFreeParticle + 1;

            if (nextFreeParticle >= Settings.MaxParticles)
            {
                nextFreeParticle = 0;
            }

            // If there are no free particles, we just have to give up.
            if (nextFreeParticle == firstRetiredParticle)
            {
                return;
            }

            // Adjust the input velocity based on how much
            // this particle system wants to be affected by it.
            velocity *= Settings.EmitterVelocitySensitivity;

            // Add in some random amount of horizontal velocity.
            var horizontalVelocity = MathHelper.Lerp(Settings.MinHorizontalVelocity,
                                                       Settings.MaxHorizontalVelocity,
                                                       (float)random.NextDouble());

            var horizontalAngle = random.NextDouble() * MathHelper.TwoPi;

            velocity.X += horizontalVelocity * (float)Math.Cos(horizontalAngle);
            velocity.Y += horizontalVelocity * (float)Math.Sin(horizontalAngle);

            // Add in some random amount of vertical velocity.
            velocity.Z += MathHelper.Lerp(Settings.MinVerticalVelocity,
                                          Settings.MaxVerticalVelocity,
                                          (float)random.NextDouble());

            // Choose four random control values. These will be used by the vertex
            // shader to give each particle a different size, rotation, and color.
            var randomValues = new Color((byte)random.Next(255),
                                           (byte)random.Next(255),
                                           (byte)random.Next(255),
                                           (byte)random.Next(255));

            // Fill in the particle vertex structure.
            for (var i = 0; i < 4; i++)
            {
                particles[firstFreeParticle * 4 + i].Position = position;
                particles[firstFreeParticle * 4 + i].Velocity = velocity;
                particles[firstFreeParticle * 4 + i].Random = randomValues;
                particles[firstFreeParticle * 4 + i].Time = currentTime;
            }

            firstFreeParticle = nextFreeParticle;
        }
    }

    /// <summary>
    /// Helper for objects that want to leave particles behind them as they
    /// move around the world. This emitter implementation solves two related
    /// problems:
    ///
    /// If an object wants to create particles very slowly, less than once per
    /// frame, it can be a pain to keep track of which updates ought to create
    /// a new particle versus which should not.
    ///
    /// If an object is moving quickly and is creating many particles per frame,
    /// it will look ugly if these particles are all bunched up together. Much
    /// better if they can be spread out along a line between where the object
    /// is now and where it was on the previous frame. This is particularly
    /// important for leaving trails behind fast moving objects such as rockets.
    ///
    /// This emitter class keeps track of a moving object, remembering its
    /// previous position so it can calculate the velocity of the object. It
    /// works out the perfect locations for creating particles at any frequency
    /// you specify, regardless of whether this is faster or slower than the
    /// game update rate.
    /// </summary>
    public class ParticleEmitter
    {
        private readonly ParticleSystem particleSystem;
        private float timeBetweenParticles;
        private Vector3 previousPosition;
        private float timeLeftOver;

        public string EmitterName;

        public Vector3 PreviousPosition
        {
            get => previousPosition;
            set => previousPosition = value;
        }

        /// <summary>
        /// Gets or sets how many particles will be generated per second.
        /// </summary>
        public float ParticlesPerSecond
        {
            get => 1.0f / timeBetweenParticles;
            set => timeBetweenParticles = 1.0f / value;
        }

        /// <summary>
        /// Constructs a new particle emitter object.
        /// </summary>
        public ParticleEmitter(ParticleSystem particleSystem,
                               float particlesPerSecond, Vector3 initialPosition)
        {
            this.particleSystem = particleSystem;

            timeBetweenParticles = 1.0f / particlesPerSecond;

            previousPosition = initialPosition;
        }

        /// <summary>
        /// This is the main update function. Derived classes should override this method.
        /// </summary>
        public virtual void Update(GameTime gameTime)
        {
            Update(gameTime, previousPosition, Vector3.Zero, true);
        }

        public virtual void Update(GameTime gameTime, Vector3 newPosition)
        {
            Update(gameTime, newPosition, null, true);
        }

        /// <summary>
        /// Updates the emitter, creating the appropriate number of particles
        /// in the appropriate positions.
        /// </summary>
        public void Update(GameTime gameTime, Vector3 newPosition, Vector3? newVelocity, bool lerpPosition)
        {
            if (gameTime == null)
            {
                throw new ArgumentNullException("gameTime");
            }

            // Work out how much time has passed since the previous update.
            var elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (elapsedTime > 0)
            {
                // Work out how fast we are moving.
                Vector3 velocity = newVelocity.HasValue ? newVelocity.Value :
                                   (newPosition - previousPosition) / elapsedTime;

                // If we had any time left over that we didn't use during the
                // previous update, add that to the current elapsed time.
                var timeToSpend = timeLeftOver + elapsedTime;

                // Counter for looping over the time interval.
                var currentTime = -timeLeftOver;

                // Create particles as long as we have a big enough time interval.
                while (timeToSpend > timeBetweenParticles)
                {
                    Vector3 position = newPosition;

                    currentTime += timeBetweenParticles;
                    timeToSpend -= timeBetweenParticles;

                    if (lerpPosition)
                    {
                        // Work out the optimal position for this particle. This will produce
                        // evenly spaced particles regardless of the object speed, particle
                        // creation frequency, or game update rate.
                        var mu = currentTime / elapsedTime;

                        position = Vector3.Lerp(previousPosition, newPosition, mu);
                    }

                    // Create the particle.
                    particleSystem.AddParticle(position, velocity);
                }

                // Store any time we didn't use, so it can be part of the next update.
                timeLeftOver = timeToSpend;
            }

            previousPosition = newPosition;
        }
    }

    /// <summary>
    /// Custom vertex structure for drawing point sprite particles.
    /// </summary>
    public struct ParticleVertex
    {
        // Stores which corner of the particle quad this vertex represents.
        public Short2 Corner;

        // Stores the starting position of the particle.
        public Vector3 Position;

        // Stores the starting velocity of the particle.
        public Vector3 Velocity;

        // Four random values, used to make each particle look slightly different.
        public Color Random;

        // The time (in seconds) at which this particle was created.
        public float Time;

        // Describe the layout of this vertex structure.
        public static readonly VertexElement[] VertexElements =
        {
            new VertexElement (0, 0, VertexElementFormat.Short2,
                                    VertexElementMethod.Default,
                                    VertexElementUsage.Position, 0),

            new VertexElement(0, 4, VertexElementFormat.Vector3,
                                    VertexElementMethod.Default,
                                    VertexElementUsage.Position, 1),

            new VertexElement(0, 16, VertexElementFormat.Vector3,
                                     VertexElementMethod.Default,
                                     VertexElementUsage.Normal, 0),

            new VertexElement(0, 28, VertexElementFormat.Color,
                                     VertexElementMethod.Default,
                                     VertexElementUsage.Color, 0),

            new VertexElement(0, 32, VertexElementFormat.Single,
                                     VertexElementMethod.Default,
                                     VertexElementUsage.TextureCoordinate, 0),
        };

        // Describe the size of this vertex structure.
        public const int SizeInBytes = 36;
    }

    /// <summary>
    /// This class demonstrates how to combine several different particle systems
    /// to build up a more sophisticated composite effect. It implements a rocket
    /// projectile, which arcs up into the sky using a ParticleEmitter to leave a
    /// steady stream of trail particles behind it. After a while it explodes,
    /// creating a sudden burst of explosion and smoke particles.
    /// </summary>
    public class Projectile
    {
        private const float trailParticlesPerSecond = 200;
        private const int numExplosionParticles = 30;
        private const int numExplosionSmokeParticles = 50;
        private const float projectileLifespan = 1.5f;
        private const float sidewaysVelocityRange = 60;
        private const float verticalVelocityRange = 40;
        private const float gravity = 15;

        private readonly ParticleSystem explosionParticles;
        private readonly ParticleSystem explosionSmokeParticles;
        private readonly ParticleEmitter trailEmitter;
        private Vector3 position;
        private Vector3 velocity;
        private float age;
        private static readonly Random random = new();

        /// <summary>
        /// Constructs a new projectile.
        /// </summary>
        public Projectile(ParticleSystem explosionParticles,
                          ParticleSystem explosionSmokeParticles,
                          ParticleSystem projectileTrailParticles)
        {
            this.explosionParticles = explosionParticles;
            this.explosionSmokeParticles = explosionSmokeParticles;

            // Start at the origin, firing in a random (but roughly upward) direction.
            position = Vector3.Zero;

            velocity.X = (float)(random.NextDouble() - 0.5) * sidewaysVelocityRange;
            velocity.Y = (float)(random.NextDouble() + 0.5) * verticalVelocityRange;
            velocity.Z = (float)(random.NextDouble() - 0.5) * sidewaysVelocityRange;

            // Use the particle emitter helper to output our trail particles.
            trailEmitter = new ParticleEmitter(projectileTrailParticles,
                                               trailParticlesPerSecond, position);
        }

        /// <summary>
        /// Updates the projectile.
        /// </summary>
        public bool Update(GameTime gameTime)
        {
            var elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Simple projectile physics.
            position += velocity * elapsedTime;
            velocity.Y -= elapsedTime * gravity;
            age += elapsedTime;

            // Update the particle emitter, which will create our particle trail.
            trailEmitter.Update(gameTime, position, null, true);

            // If enough time has passed, explode! Note how we pass our velocity
            // in to the AddParticle method: this lets the explosion be influenced
            // by the speed and direction of the projectile which created it.
            if (age > projectileLifespan)
            {
                for (var i = 0; i < numExplosionParticles; i++)
                {
                    explosionParticles.AddParticle(position, velocity);
                }

                for (var i = 0; i < numExplosionSmokeParticles; i++)
                {
                    explosionSmokeParticles.AddParticle(position, velocity);
                }

                return false;
            }

            return true;
        }
    }
}
