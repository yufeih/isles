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

namespace Isles
{
    #region ParticleEffect
    /// <summary>
    /// Base class for all particle system effect
    /// </summary>
    public abstract class ParticleEffect : BaseEntity
    {
        public ParticleEffect(GameWorld world)
            : base(world) { }

        public abstract ParticleSystem Particle { get; }
        public abstract float Emission { get; set; }

        public override void Update(GameTime gameTime) { }
        public override void Draw(GameTime gameTime) { }
    }
    #endregion

#if FALSE
#region Deleted
    #region ParticleEffectFire
    /// <summary>
    /// Fire
    /// </summary>
    public class ParticleEffectFire : ParticleEffect
    {
        #region Field

        ParticleSystem particleFire;
        ParticleEmitter emitterFire;
        ParticleSystem particleSmoke;
        ParticleEmitter emitterSmoke;


        /// <summary>
        /// Get the radius of the center of fire
        /// </summary>
        public float Radius
        {
            get { return radius; }
        }

        float radius;

        public float RotationRate
        {
            get { return rotationRate; }
        }

        public Vector3 Source
        {
            get
            {
                return this.source;
            }
        }

        Vector3 source;

        public Vector3 Destination
        {
            get
            {
                return this.destination;
            }
        }

        Vector3 destination;


        float rotationRate;

        #endregion

        #region Method
        public ParticleEffectFire(GameWorld world)
            : base(world)
        {
            

            ParticleSettings firesettings = new ParticleSettings();
            firesettings.TextureName = "Textures/Fire";

            firesettings.MaxParticles = 2400;

            firesettings.Duration = 2;

            firesettings.DurationRandomness = 1;

            firesettings.MinHorizontalVelocity = 0;
            firesettings.MaxHorizontalVelocity = 15;

            firesettings.MinVerticalVelocity = -10;
            firesettings.MaxVerticalVelocity = 10;

            // Set gravity upside down, so the flames will 'fall' upward.
            firesettings.Gravity = new Vector3(0, 15, 0);

            firesettings.MinColor = new Color(255, 255, 255, 10);
            firesettings.MaxColor = new Color(255, 255, 255, 40);

            firesettings.MinStartSize = 5;
            firesettings.MaxStartSize = 10;

            firesettings.MinEndSize = 10;
            firesettings.MaxEndSize = 40;

            // Use additive blending.
            firesettings.SourceBlend = Blend.SourceAlpha;
            firesettings.DestinationBlend = Blend.One;

            Position = new Vector3(
                1024, 1024, world.Landscape.GetHeight(1024, 1024) + 10);

            particleFire = ParticleSystem.Create("Fire");
            emitterFire = new ParticleEmitter(particleFire, 100, Position);
            particleFire.ParticleName = "Fire";
            emitterFire.EmitterName = "Fire";
            //this.Particles.Add(particleFire);
            //this.Emitters.Add(emitterFire);

            ParticleSettings smokesettings = new ParticleSettings();

            smokesettings.DestinationBlend = Blend.One;
            smokesettings.SourceBlend = Blend.SourceAlpha;

            smokesettings.Duration = 2;
            smokesettings.DurationRandomness = 1;

            smokesettings.MaxParticles = 25000;

            smokesettings.TextureName = "Textures/Smoke2";

            smokesettings.MaxHorizontalVelocity = 15;
            smokesettings.MinHorizontalVelocity = 0;

            smokesettings.MaxVerticalVelocity = 10;
            smokesettings.MinVerticalVelocity = -20;

            smokesettings.Gravity = new Vector3(0,-2,0);

            smokesettings.MaxColor = new Color(255,255,255,100);
            smokesettings.MinColor = new Color(255,255,255,80);

            smokesettings.MaxStartSize = 10;
            smokesettings.MaxEndSize = 40;

            smokesettings.MinStartSize = 5;
            smokesettings.MinEndSize = 10;

            particleSmoke = ParticleSystem.Create("Smoke");
            emitterSmoke = new ParticleEmitter(particleSmoke, 100, Position);

            particleSmoke.ParticleName = "Smoke";
            emitterSmoke.EmitterName = "Smoke";

            //this.Particles.Add(particleSmoke);
            //this.Emitters.Add(emitterSmoke);

            rotationRate = 0.25f * (float)Math.PI;
            radius = 30.0f;
        }

        public void Launch()
        {
            //this.State = S
        }

        public override void Update(GameTime gameTime)
        {
            //if(this.emitterSmoke != null)
            //{
            //    this.emitterSmoke.Update(gameTime, Position, null);
            //}

            //if (this.emitterFire != null)
            //{
            //    this.emitterFire.Update(gameTime, Position, null);
            //}

            //foreach(ParticleSystem particle in this.Particles )
            //{
            //    if (particle != null)
            //        particle.Update(gameTime);
            //}
        }

        public override void Draw(GameTime gameTime)
        {
            if(this.particleFire != null)
            {
                this.particleFire.SetCamera(World.Game.View, World.Game.Projection);
                //this.particleFire.Draw(gameTime);
            }

            if(this.particleSmoke != null)
            {
                this.particleSmoke.SetCamera(World.Game.View, World.Game.Projection);
                //this.particleSmoke.Draw(gameTime);
            }
        }

        /// <summary>
        /// Deserialized attributes: ParticleSettings
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);
        }
        #endregion
    }
    #endregion

    #region ParticleEffeectMissile

    /// <summary>
    /// Base class for all particle system effect
    /// </summary>
    public class ParticleEffectMissile: ParticleEffect
    {
        #region Field
        ParticleSystem particleTrail;
        ParticleEmitter emitterTrail;
        ParticleSystem particleExplosion;

        public MissileState State
        {
            get
            {
                return this.state;
            }
        }

        private MissileState state;

        public override Vector3 Velocity
        {
            get
            {
                return this.velocity;
            }
        }

        private Vector3 velocity;

        /// <summary>
        /// Gets or sets the target
        /// </summary>
        public IWorldObject Target
        {
            get { return target; }
            set { target = value; }
        }

        IWorldObject target;

        public float MaxAcceleration
        {
            get
            {
                return this.maxAcceleration;
            }

            set
            {
                this.maxAcceleration = value;
            }
        }
        private float maxAcceleration;

        private Vector3 accelerationVec;

        public Vector3 Source
        {
            get
            {
                return this.source;
            }
        }

        private Vector3 source;

        public Vector3 Destination
        {
            get
            {
                return this.destination;
            }
            set
            {
                this.destination = value;
            }
        }

        public float maxspeed;

        public float MaxSpeed
        {
            get
            {
                return this.maxspeed;
            }

            set
            {
                this.maxspeed = value;
            }
        }

        private Vector3 destination;

        private TimeSpan explosionAge;
        //private MissileState state;

        public event EventHandler Hit;
        public object Tag;
        #endregion

        #region Method
        public ParticleEffectMissile(GameWorld world)
            : base(world)
        {
            
            this.maxAcceleration = 0.0008f;
            this.destination = new Vector3(1024,1124,50);

            this.state = MissileState.Waiting;
            this.maxspeed = 0.12f;

            ParticleSettings trailsettings = this.GetTrailSetting();

            particleTrail = ParticleSystem.Create("Trail");
            emitterTrail= new ParticleEmitter(particleTrail, 200, Position);

            particleTrail.ParticleName = "Trail";
            emitterTrail.EmitterName = "Trail";

            ParticleSettings explosionsettings = this.GetExplosionSetting();

            this.particleExplosion = ParticleSystem.Create("Explosion");

            this.velocity = Vector3.Normalize(this.destination - this.Position) * this.maxspeed / 6;
            float speed = this.velocity.Length();
            this.velocity.Z += speed * 2;

            this.accelerationVec = Vector3.Normalize(this.Velocity) * this.maxAcceleration;
            //this.Particles.Add(particleTrail);
            //this.Particles.Add(particleExplosion);
            //this.Emitters.Add(emitterTrail);

            this.explosionAge = TimeSpan.Zero;

        }

        /// <summary>
        /// Get settings of missile effect
        /// </summary>
        /// <returns></returns>
        private ParticleSettings GetTrailSetting()
        {
            ParticleSettings trailsettings = new ParticleSettings();

            //sparksettings.DestinationBlend = Blend.InverseSourceAlpha;
            //sparksettings.SourceBlend = Blend.SourceAlpha;

            trailsettings.Duration = 1.2f;
            trailsettings.DurationRandomness = 0.2f;

            trailsettings.MaxParticles = 1000;

            trailsettings.EmitterVelocitySensitivity = 1.0f;

            trailsettings.TextureName = "Textures/Smoke2";

            trailsettings.MaxHorizontalVelocity = 0.1f;
            trailsettings.MinHorizontalVelocity = 0.1f;

            trailsettings.MaxVerticalVelocity = 0;
            trailsettings.MinVerticalVelocity = 0.1f;

            trailsettings.Gravity = new Vector3(0, 0, 0);

            trailsettings.MaxColor = new Color(64, 96, 128, 255);
            trailsettings.MinColor = new Color(255, 255, 255, 128);

            trailsettings.MinRotateSpeed = -4;
            trailsettings.MaxRotateSpeed = 4;

            trailsettings.MaxStartSize = 6;
            trailsettings.MaxEndSize = 4;

            trailsettings.MinStartSize = 5;
            trailsettings.MinEndSize = 2;

            return trailsettings;
        }

        /// <summary>
        /// Get settings of explosion effect
        /// </summary>
        /// <returns></returns>
        private ParticleSettings GetExplosionSetting()
        {
            ParticleSettings settings = new ParticleSettings();

            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;

            settings.MaxStartSize = 0.5f;
            settings.MinStartSize = 0.1f;

            settings.MinEndSize = 8.0f;
            settings.MaxEndSize = 12.0f;

            settings.MaxVerticalVelocity = 6.0f;
            settings.MinVerticalVelocity = 2.0f;

            settings.MinHorizontalVelocity = 1.0f;
            settings.MaxHorizontalVelocity = 5.0f;

            settings.MinColor = Color.DarkGray;
            settings.MaxColor = Color.Gray;

            settings.MaxRotateSpeed = 1;
            settings.MinRotateSpeed = -1;

            settings.TextureName = "Textures/fire";

            settings.DurationRandomness = 1;

            settings.Duration = 2;

            settings.MaxParticles = 200;

            settings.EndVelocity = 0;

            return settings;

        }

        public void Launch()
        {
            this.state = MissileState.Flying;
        }

        public override void Update(GameTime gameTime)
        {
            UpdateState();
            if(this.state == MissileState.Flying)
            {
                if (target != null)
                    Destination = target.Position;
                this.UpdateVelocity(gameTime);
                this.Position += this.velocity * gameTime.ElapsedGameTime.Milliseconds;
                if (this.emitterTrail != null)
                {
                    this.emitterTrail.Update(gameTime, Position, null);
                }

                //foreach (ParticleSystem particle in this.Particles)
                //{
                //    if (particle != null)
                //        particle.Update(gameTime);
                //}
            }else if(this.state == MissileState.Exploding)
            {
                foreach (ParticleSystem particle in this.Particles)
                {
                    if (particle != null)
                        particle.Update(gameTime);
                }

                this.explosionAge += gameTime.ElapsedGameTime;
                for (int i = 0; i < 15; i++ )
                {
                    this.particleExplosion.AddParticle(this.Position, Vector3.Zero);
                }
                this.particleExplosion.Update(gameTime);
            }
        }

        private void UpdateState()
        {
            Vector3 interval = this.destination - this.Position;
            if(interval.Length() < 4 && this.state == MissileState.Flying)
            {
                if (Hit != null)
                    Hit(this, null);
                this.state = MissileState.Exploding;
            }else if(this.explosionAge.TotalMilliseconds > 1100)
            {
                this.state = MissileState.Finished;
                World.Destroy(this);
            }

        }

        private void UpdateAcceleration(GameTime gameTime)
        {
            Vector3 interval = this.destination - this.Position;
            Vector3 desiredVelocity = interval / this.maxspeed;
            this.accelerationVec = (desiredVelocity - this.velocity) / gameTime.ElapsedGameTime.Milliseconds;
            if(this.accelerationVec.Length() > this.maxAcceleration)
            {
                this.accelerationVec.Normalize();
                this.accelerationVec *= this.maxAcceleration;
            }
        }

        private void UpdateVelocity(GameTime gameTime)
        {
            this.UpdateAcceleration(gameTime);
            this.velocity += this.accelerationVec * gameTime.ElapsedGameTime.Milliseconds;
            if(this.velocity.Length() > this.maxspeed)
            {
                this.velocity.Normalize();
                this.velocity *= this.maxspeed;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if(this.state == MissileState.Flying)
            {
                if (this.particleTrail != null)
                {
                    this.particleTrail.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleTrail.Draw(gameTime);
                }
            }else if(this.state == MissileState.Exploding)
            {
                if (this.particleTrail != null)
                {
                    this.particleTrail.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleTrail.Draw(gameTime);
                }
                if(this.state == MissileState.Exploding)
                {
                    this.particleExplosion.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleExplosion.Draw(gameTime);
                }
            }
        }

        /// <summary>
        /// Deserialized attributes: ParticleSettings
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            this.source = this.Position;
            base.Deserialize(xml);
            this.destination = Helper.StringToVector3(xml.GetAttribute("Destination"));
        }
        #endregion
    }
    public class ParticleEffectMissile2 : ParticleEffect
    {
        #region Field
        ParticleSystem particleTrail;
        ParticleEmitter emitterTrail;
        ParticleSystem particleExplosion;

        public MissileStateExtended State
        {
            get
            {
                return this.state;
            }
        }

        private MissileStateExtended state;

        public override Vector3 Velocity
        {
            get
            {
                return this.velocity;
            }
        }

        private Vector3 velocity;

        public float MaxAcceleration
        {
            get
            {
                return this.maxAcceleration;
            }

            set
            {
                this.maxAcceleration = value;
            }
        }
        private float maxAcceleration;

        private Vector3 accelerationVec;

        public Vector3 Source
        {
            get
            {
                return this.source;
            }
        }

        private Vector3 source;

        public Vector3 Destination
        {
            get
            {
                return this.destination;
            }
            set
            {
                this.destination = value;
               // Random r = new Random();
                
            }
        }

        public float maxspeed;

        public float MaxSpeed
        {
            get
            {
                return this.maxspeed;
            }

            set
            {
                this.maxspeed = value;
            }
        }

        private Vector3 destination;

        private Vector3 adaptedDestination;

        private TimeSpan explosionAge;
        //private MissileState state;

        #endregion

        #region Method
        public ParticleEffectMissile2(GameWorld world)
            : base(world)
        {

            this.maxAcceleration = 0.0005f;
            this.destination = new Vector3(1024, 1124, 50);

            this.state = MissileStateExtended.Waiting;
            this.maxspeed = 0.03f;

            ParticleSettings trailsettings = this.GetTrailSetting();

            particleTrail = ParticleSystem.Create("Trail");
            emitterTrail = new ParticleEmitter(particleTrail, 200, Position);

            particleTrail.ParticleName = "Trail";
            emitterTrail.EmitterName = "Trail";

            ParticleSettings explosionsettings = this.GetExplosionSetting();

            this.particleExplosion = ParticleSystem.Create("Explosion");

            this.velocity = Vector3.Normalize(this.destination - this.Position) * this.maxspeed / 6;
            float speed = this.velocity.Length();
            this.velocity.Z = 0;// speed * 0.707f;
            this.velocity.Y = 0;
            this.velocity.X = 0;

            this.accelerationVec = Vector3.Normalize(this.Velocity) * this.maxAcceleration;
            this.Particles.Add(particleTrail);
            this.Particles.Add(particleExplosion);
            this.Emitters.Add(emitterTrail);

            this.explosionAge = TimeSpan.Zero;

        }

        /// <summary>
        /// Get settings of missile effect
        /// </summary>
        /// <returns></returns>
        private ParticleSettings GetTrailSetting()
        {
            ParticleSettings trailsettings = new ParticleSettings();

            //sparksettings.DestinationBlend = Blend.InverseSourceAlpha;
            //sparksettings.SourceBlend = Blend.SourceAlpha;

            trailsettings.Duration = 1.40f;
            trailsettings.DurationRandomness = 1.5f;

            trailsettings.MaxParticles = 1000;

            trailsettings.EmitterVelocitySensitivity = 1.0f;

            trailsettings.TextureName = "Textures/Smoke2";

            trailsettings.MaxHorizontalVelocity = 0.1f;
            trailsettings.MinHorizontalVelocity = 0.1f;

            trailsettings.MaxVerticalVelocity = 0;
            trailsettings.MinVerticalVelocity = 0.1f;

            trailsettings.Gravity = new Vector3(0, 0, 0);

            trailsettings.MaxColor = new Color(64, 96, 128, 255);
            trailsettings.MinColor = new Color(255, 255, 255, 128);

            trailsettings.MinRotateSpeed = -4;
            trailsettings.MaxRotateSpeed = 4;

            trailsettings.MaxStartSize = 6;
            trailsettings.MaxEndSize = 4;

            trailsettings.MinStartSize = 6;
            trailsettings.MinEndSize = 4;

            return trailsettings;
        }

        /// <summary>
        /// Get settings of explosion effect
        /// </summary>
        /// <returns></returns>
        private ParticleSettings GetExplosionSetting()
        {
            ParticleSettings settings = new ParticleSettings();

            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;

            settings.MaxStartSize = 0.4f;
            settings.MinStartSize = 0.1f;

            settings.MinEndSize = 10.0f;
            settings.MaxEndSize = 20.0f;

            settings.MaxVerticalVelocity = 3.0f;
            settings.MinVerticalVelocity = 2.0f;

            settings.MinHorizontalVelocity = 1.0f;
            settings.MaxHorizontalVelocity = 2.0f;

            settings.MinColor = Color.DarkGray;
            settings.MaxColor = Color.Gray;

            settings.MaxRotateSpeed = 1;
            settings.MinRotateSpeed = -1;

            settings.TextureName = "Textures/fire";

            settings.DurationRandomness = 1;

            settings.Duration = 2;

            settings.MaxParticles = 100;

            settings.EndVelocity = 0;

            return settings;

        }

        public void Launch()
        {
            this.state = MissileStateExtended.Adapting;
            this.source = this.Position;
            Random r = new Random();
            double rad = r.NextDouble() * r.NextDouble() - 0.5;
            rad = rad * 60;
            float sin = (float)Math.Sin(rad);
            float cos = (float)Math.Cos(rad);
            Vector3 offset = Vector3.Normalize(this.Destination - this.Position) * 3;
            this.adaptedDestination = this.Position - (new Vector3(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos, 4));
            //this.adaptedDestination = this.Position - offset;
        }

        public override void Update(GameTime gameTime)
        {
            UpdateState();
            if (this.state == MissileStateExtended.Flying || this.state == MissileStateExtended.Adapting)
            {
                //this.Destination += new Vector3(-0.0005f, 0, 0) * gameTime.ElapsedGameTime.Milliseconds;
                this.UpdateVelocity(gameTime);
                this.Position += this.velocity * gameTime.ElapsedGameTime.Milliseconds;
                if (this.emitterTrail != null)
                {
                    this.emitterTrail.Update(gameTime, Position);
                }

                foreach (ParticleSystem particle in this.Particles)
                {
                    if (particle != null)
                        particle.Update(gameTime);
                }
            }
            else if (this.state == MissileStateExtended.Exploding)
            {
                foreach (ParticleSystem particle in this.Particles)
                {
                    if (particle != null)
                        particle.Update(gameTime);
                }

                this.explosionAge += gameTime.ElapsedGameTime;
                for (int i = 0; i < 20; i++)
                {
                    this.particleExplosion.AddParticle(this.Destination, Vector3.Zero);
                }
                this.particleExplosion.Update(gameTime);
            }
        }

        private void UpdateState()
        {
            Vector3 interval = this.destination - this.Position;
            Vector3 adaptingInterval = this.adaptedDestination - this.Position;
            if(adaptingInterval.Length() < 2 && this.state == MissileStateExtended.Adapting)
            {
                this.state = MissileStateExtended.Flying;
            }else if (interval.Length() < 2 && this.state == MissileStateExtended.Flying)
            {
                this.state = MissileStateExtended.Exploding;
            }
            else if (this.explosionAge.TotalMilliseconds > 1100)
            {
                this.state = MissileStateExtended.Finished;
                World.Destroy(this);
            }

        }

        private void UpdateAcceleration()
        {
            Vector3 currentDestination = Vector3.Zero; ;
            if (this.state == MissileStateExtended.Adapting)
            {
                currentDestination = this.adaptedDestination;
            }
            else if (this.state == MissileStateExtended.Flying)
            {
                currentDestination = this.destination;
            }
            Vector3 interval = currentDestination - this.Position;
            float intervalLen = interval.Length() / ((currentDestination - this.source).Length());
            float currentAcceleration = (float)(0.3 * Math.Pow((1 - intervalLen), 3) + 0.4 * (1 - intervalLen) + 0.3) * this.maxAcceleration;
            interval.Normalize();
            this.accelerationVec = interval * currentAcceleration;
        }

        private void UpdateVelocity(GameTime gameTime)
        {
            this.UpdateAcceleration();
            this.velocity += this.accelerationVec * gameTime.ElapsedGameTime.Milliseconds;
            if (this.velocity.Length() > this.maxspeed)
            {
                this.velocity.Normalize();
                this.velocity *= this.maxspeed;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (this.state == MissileStateExtended.Flying || this.state == MissileStateExtended.Adapting)
            {
                if (this.particleTrail != null)
                {
                    this.particleTrail.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleTrail.Draw(gameTime);
                }
            }
            else if (this.state == MissileStateExtended.Exploding)
            {
                if (this.particleTrail != null)
                {
                    this.particleTrail.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleTrail.Draw(gameTime);
                }
                this.particleExplosion.SetCamera(World.Game.View, World.Game.Projection);
                //this.particleExplosion.Draw(gameTime);
            }
        }

        /// <summary>
        /// Deserialized attributes: ParticleSettings
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            this.source = this.Position;
            base.Deserialize(xml);
            this.Destination = Helper.StringToVector3(xml.GetAttribute("Destination"));
            Random r = new Random();
            this.adaptedDestination = this.Position - Vector3.Normalize((this.destination - this.source)) * 10 + new Vector3(r.Next(10) - 5, r.Next(10) - 5, r.Next(10) - 5);
            
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public enum MissileState
    {
        Waiting = 0,
        Flying = 1,
        Exploding = 2,
        Finished = 3
    }

    public enum MissileStateExtended
    {
        Waiting = 0,
        Adapting = 1,
        Flying = 2,
        Exploding = 3,
        Finished = 4
    }

    #endregion

    #region ParticleEffectFireBall

    public class ParticleEffectFireBall : ParticleEffect
    {
        #region Field
        ParticleSystem particleTrail;
        ParticleEmitter emitterTrail;
        ParticleSystem particleExplosion;
        ParticleSystem particleFireBall;
        ParticleEmitter emitterFireBall;

        public MissileState State
        {
            get
            {
                return this.state;
            }
        }

        private MissileState state;

        public override Vector3 Velocity
        {
            get
            {
                return this.velocity;
            }
        }

        private Vector3 velocity;

        public float MaxAcceleration
        {
            get
            {
                return this.maxAcceleration;
            }

            set
            {
                this.maxAcceleration = value;
            }
        }
        private float maxAcceleration;

        private Vector3 accelerationVec;

        public Vector3 Source
        {
            get
            {
                return this.source;
            }
        }

        private Vector3 source;

        public Vector3 Destination
        {
            get
            {
                return this.destination;
            }
            set
            {
                this.destination = value;
            }
        }

        /// <summary>
        /// Gets or sets the target
        /// </summary>
        public IWorldObject Target
        {
            get { return target; }
            set { target = value; }
        }

        IWorldObject target;

        float maxspeed;

        public float MaxSpeed
        {
            get
            {
                return this.maxspeed;
            }

            set
            {
                this.maxspeed = value;
            }
        }

        private Vector3 destination;

        private TimeSpan explosionAge;
        //private MissileState state;

        public event EventHandler Hit;
        public object Tag;
        #endregion

        #region Method
        public ParticleEffectFireBall(GameWorld world)
            : base(world)
        {

            this.maxAcceleration = 0.0004f;
            this.destination = new Vector3(1024, 1124, 50);

            this.state = MissileState.Waiting;
            this.maxspeed = 0.08f;

            ParticleSettings trailsettings = this.GetTrailSetting();

            particleTrail = ParticleSystem.Create("FireballTrail"); ;
            emitterTrail = new ParticleEmitter(particleTrail, 200, Position);

            particleTrail.ParticleName = "Trail";
            emitterTrail.EmitterName = "Trail";

            ParticleSettings explosionsettings = this.GetExplosionSetting();

            this.particleExplosion = ParticleSystem.Create("Explosion");
            this.particleExplosion.ParticleName = "Explosion";


            this.accelerationVec = Vector3.Normalize(this.Velocity) * this.maxAcceleration;

            ParticleSettings fireballsettings = this.GetFireBallSettings();

            this.particleFireBall = ParticleSystem.Create("Fireball");
            this.emitterFireBall = new ParticleEmitter(particleFireBall, 100, Position);
            

            //this.Particles.Add(particleTrail);
            //this.Particles.Add(particleExplosion);
            //this.Particles.Add(particleFireBall);
            //this.Emitters.Add(emitterTrail);
            //this.Emitters.Add(emitterFireBall);

            this.velocity = Vector3.Normalize(this.destination - this.Position) * this.maxspeed / 6;
            float speed = this.velocity.Length();
            this.velocity.Z += speed * 2;

            this.explosionAge = TimeSpan.Zero;

        }

        /// <summary>
        /// Get the settings of fire ball effect
        /// </summary>
        /// <returns></returns>
        private ParticleSettings GetFireBallSettings()
        {
            ParticleSettings settings = new ParticleSettings();

            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;

            settings.MaxStartSize = 3f;
            settings.MinStartSize = 2f;

            settings.MinEndSize = 4.0f;
            settings.MaxEndSize = 12.0f;

            settings.MaxVerticalVelocity = 3.0f;
            settings.MinVerticalVelocity = 2.0f;

            settings.MinHorizontalVelocity = 2.0f;
            settings.MaxHorizontalVelocity = 3.0f;

            settings.MinColor = Color.White;
            settings.MaxColor = Color.White;

            settings.MaxRotateSpeed = 1;
            settings.MinRotateSpeed = -1;

            settings.TextureName = "Textures/fire";

            settings.DurationRandomness = 1;

            settings.Duration = 0.5f;

            settings.MaxParticles = 800;

            settings.EndVelocity = 1;

            return settings;

        }

        /// <summary>
        /// Get settings of missile effect
        /// </summary>
        /// <returns></returns>
        private ParticleSettings GetTrailSetting()
        {
            ParticleSettings trailsettings = new ParticleSettings();

            trailsettings.DestinationBlend = Blend.InverseSourceAlpha;
            trailsettings.SourceBlend = Blend.SourceAlpha;

            trailsettings.Duration = 1.0f;
            trailsettings.DurationRandomness = 1.5f;

            trailsettings.MaxParticles = 50;

            trailsettings.EmitterVelocitySensitivity = 1.0f;

            trailsettings.TextureName = "Textures/smoke";

            trailsettings.MaxHorizontalVelocity = 1.0f;
            trailsettings.MinHorizontalVelocity = 1.0f;

            trailsettings.MaxVerticalVelocity = 2f;
            trailsettings.MinVerticalVelocity = 1f;

            //trailsettings.Gravity = new Vector3(0, 0, 0);

            trailsettings.MaxColor = new Color(128, 128, 128, 255);
            trailsettings.MinColor = new Color(255, 255, 255, 128);

            trailsettings.MinRotateSpeed = -4;
            trailsettings.MaxRotateSpeed = 4;

            trailsettings.MaxStartSize = 4;
            trailsettings.MaxEndSize = 9;

            trailsettings.MinStartSize = 2;
            trailsettings.MinEndSize = 8;

            return trailsettings;
        }

        /// <summary>
        /// Get settings of explosion effect
        /// </summary>
        /// <returns></returns>
        private ParticleSettings GetExplosionSetting()
        {
            ParticleSettings settings = new ParticleSettings();

            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;

            settings.MaxStartSize = 0.5f;
            settings.MinStartSize = 0.1f;

            settings.MinEndSize = 12.0f;
            settings.MaxEndSize = 20.0f;

            settings.MaxVerticalVelocity = 5.0f;
            settings.MinVerticalVelocity = 2.0f;

            settings.MinHorizontalVelocity = 1.0f;
            settings.MaxHorizontalVelocity = 5.0f;

            settings.MinColor = Color.DarkGray;
            settings.MaxColor = Color.Gray;

            settings.MaxRotateSpeed = 1;
            settings.MinRotateSpeed = -1;

            settings.TextureName = "Textures/fire";

            settings.DurationRandomness = 1;

            settings.Duration = 2;

            settings.MaxParticles = 100;

            settings.EndVelocity = 0;

            return settings;

        }

        public void Launch()
        {
            this.state = MissileState.Flying;
        }

        public override void Update(GameTime gameTime)
        {
            UpdateState();
            if (this.state == MissileState.Flying)
            {
                if (target != null)
                    destination = target.Position;
                this.UpdateVelocity(gameTime);
                this.Position += this.velocity * gameTime.ElapsedGameTime.Milliseconds;
                if (this.emitterTrail != null)
                {
                    this.emitterTrail.Update(gameTime, Position);
                }

                if(this.emitterFireBall != null)
                {
                    this.emitterFireBall.Update(gameTime, Position);
                }

                //foreach (ParticleSystem particle in this.Particles)
                //{
                //    if (particle != null)
                //        particle.Update(gameTime);
                    
                //}


            }
            else if (this.state == MissileState.Exploding)
            {
                //foreach (ParticleSystem particle in this.Particles)
                //{
                //    if (particle != null)
                //        particle.Update(gameTime);
                //}

                this.explosionAge += gameTime.ElapsedGameTime;
                for (int i = 0; i < 20; i++)
                {
                    this.particleExplosion.AddParticle(this.Position, Vector3.Zero);
                }
                this.particleExplosion.Update(gameTime);
            }
        }

        private void UpdateState()
        {
            Vector3 interval = this.destination - this.Position;
            if (interval.Length() < 4 && this.state == MissileState.Flying)
            {                
                if (Hit != null)
                    Hit(this, null);
                this.state = MissileState.Exploding;
            }
            else if (this.explosionAge.TotalMilliseconds > 1100)
            {
                this.state = MissileState.Finished;
                World.Destroy(this);
            }

        }

        private void UpdateAcceleration(GameTime gameTime)
        {
            Vector3 interval = this.destination - this.Position;
            Vector3 desiredVelocity = interval / this.maxspeed;
            this.accelerationVec = (desiredVelocity - this.velocity) / gameTime.ElapsedGameTime.Milliseconds;
            if (this.accelerationVec.Length() > this.maxAcceleration)
            {
                this.accelerationVec.Normalize();
                this.accelerationVec *= this.maxAcceleration;
            }
        }

        private void UpdateVelocity(GameTime gameTime)
        {
            this.UpdateAcceleration(gameTime);
            this.velocity += this.accelerationVec * gameTime.ElapsedGameTime.Milliseconds;
            if (this.velocity.Length() > this.maxspeed)
            {
                this.velocity.Normalize();
                this.velocity *= this.maxspeed;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            //if (this.state == MissileState.Flying)
            //{
            //    if (this.particleTrail != null)
            //    {
            //        this.particleTrail.SetCamera(World.Game.View, World.Game.Projection);
            //        this.particleTrail.Draw(gameTime);
            //    }
            //}
            //else 
            if (this.state != MissileState.Waiting)
            {
                if (this.particleTrail != null)
                {
                    this.particleTrail.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleTrail.Draw(gameTime);
                }
                if(this.particleFireBall != null)
                {
                    this.particleFireBall.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleFireBall.Draw(gameTime);
                }
                if (this.state == MissileState.Exploding)
                {
                    this.particleExplosion.SetCamera(World.Game.View, World.Game.Projection);
                    //this.particleExplosion.Draw(gameTime);
                }
            }
        }

        /// <summary>
        /// Deserialized attributes: ParticleSettings
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            this.source = this.Position;
            base.Deserialize(xml);
            this.destination = Helper.StringToVector3(xml.GetAttribute("Destination"));
        }
        #endregion
    }

    #endregion

    #region ParticleEffectOnFire

    public class ParticleEffectOnFire : ParticleEffect
    {
        #region Field

        /// <summary>
        /// The max settings
        /// </summary>
        #region StaticField

        public static void RestoreDefaultSettings()
        {
            MaxFireDuration = 1.4f;
            MinFireDuration = 0.7f;

            MaxSmokeDuration = 4.6f;
            MinSmokeDuration = 4f;

            MaxSmokeParticlePerSecond = 16;
            MinSmokeParticlePerSecond = 6;

            MaxFireParticlePerSecond = 20;
            MinFireParticlePerSecond = 10;

            MaxFireSpeed = 2.9f;
            MinFireSpeed = 2.0f;

            MaxFireSize = 30.0f;
            MinFireSize = 20.0f;

            MaxSmokeSize = 50f;
            MinSmokeSize = 12f;

            MaxMinColor = new Color(250,250,250,255);
            MinMinColor = new Color(80,80,80,255);

            FirePerSquare = (float)1 / 240;
        }

        #region Smoke

        public static float MaxSmokeParticlePerSecond
        {
            get
            {
                return s_maxSmokeParticlePerSecond;
            }

            set
            {
                s_maxSmokeParticlePerSecond = value;
            }
        }

        private static float s_maxSmokeParticlePerSecond;

        public static float MinSmokeParticlePerSecond
        {
            get
            {
                return s_minSmokeParticlePerSecond;
            }
            set
            {
                if (value > 10)
                {
                    
                }
                else
                {
                    s_minSmokeParticlePerSecond = value;
                }
            }
        }

        private static float s_minSmokeParticlePerSecond;

        public static float MaxSmokeDuration
        {
            get
            {
                return s_maxSmokeDuration;
            }
            set
            {
                s_maxSmokeDuration = value;
            }
        }

        private static float s_maxSmokeDuration;

        public static float MinSmokeDuration
        {
            get
            {
                return s_minSmokeDuration;
            }
            set
            {
                s_minSmokeDuration = value;
            }
        }

        private static float s_minSmokeDuration;


        public static Color MaxMinColor
        {
            get
            {
                return s_maxMinColor;
            }

            set
            {
                s_maxMinColor = value;
            }
        }

        private static Color s_maxMinColor;

        public static Color MinMinColor
        {
            get
            {
                return s_minMinColor;
            }

            set
            {
                s_minMinColor = value;
            }
        }

        private static Color s_minMinColor;

        public static float MaxSmokeSize
        {
            get
            {
                return s_maxSmokeSize;
            }

            set
            {
                s_maxSmokeSize = value;
            }
        }

        private static float s_maxSmokeSize;

        public static float MinSmokeSize
        {
            get
            {
                return s_minSmokeSize;
            }

            set
            {
                s_minSmokeSize = value;
            }
        }

        private static float s_minSmokeSize;

        #endregion

        #region Fire

        public static float FirePerSquare
        {
            get
            {
                return firePerSquare;
            }
            set
            {
                firePerSquare = value;
            }
        }

        private static float firePerSquare;

        public static float MaxFireSpeed
        {
            get
            {
                return maxFireSpeed;
            }
            set
            {
                maxFireSpeed = value;
            }
        }

        private static float maxFireSpeed;

        public static float MinFireSpeed
        {
            get
            {
                return minFireSpeed;
            }
            set
            {
                minFireSpeed = value;
            }
        }

        private static float minFireSpeed;

        public static float MaxFireSize
        {
            get
            {
                return maxFireSize;
            }
            set
            {
                maxFireSize = value;
            }

        }

        private static float maxFireSize;

        public static float MinFireSize
        {
            get
            {
                return minFireSize;
            }
            set
            {
                minFireSize = value;
            }
        }

        private static float minFireSize;

        public static  float MaxFireParticlePerSecond
        {
            get
            {
                return s_maxFireParticlePerSecond;
            }

            set
            {
                s_maxFireParticlePerSecond = value;
            }
        }

        private static float s_maxFireParticlePerSecond;

        public static float MinFireParticlePerSecond
        {
            get
            {
                return s_minFireParticlePerSecond;
            }

            set
            {
                s_minFireParticlePerSecond = value;
            }
        }

        private static float s_minFireParticlePerSecond;

        public static float MaxFireDuration
        {
            get
            {
                return s_maxFireDuration;
            }
            set
            {
                s_maxFireDuration = value;
            }
        }

        private static float s_maxFireDuration;

        public static float MinFireDuration
        {
            get
            {
                return s_minFireDuration;
            }
            set
            {
                s_minFireDuration = value;
            }
        }

        private static float s_minFireDuration;


        #endregion

        #endregion

        ParticleSystem particleFire;
        ParticleSystem particleCenterSmoke;
        ParticleSystem particleFireSmoke;


        //---------Emitter Outline---------
        //There's a center emitter of smoke  
        //releasing bolocks of black smoke.
        //Each fire emitter is bound with a
        //smoke emitter generating smoke of 
        //fire
        //
        //The center emitter of smoke located 
        //in the center of the building
        //
        //The fire emitters locate on the
        //fringe of building outline with a
        //distance of Z to the terrain
        //
        //The center smoke is supposed to be
        //sick and black, relating to the 
        //intensity of burning
        //
        //Burning fire is also related with intensity
        //
        //Final Aim: Reduce total particles
        //-------------------------------

        ParticleEmitter centerSmokeEmitter;
        List<ParticleEmitter> emittersSmoke;
        List<ParticleEmitter> emittersFire;

        Vector3 smokePosition;

        //The total area of the outline and the parameter of intensity decide the number of emitters of fire

        /// <summary>
        /// Outline is the range, which is a circle or a rectangle where fire is burning
        /// </summary>
        public Outline Outline
        {
            get
            {
                return this.outline;
            }

            set
            {
                this.outline = value;
                this.RefreshFireEmitter();
            }
        }

        private Outline outline;

        /// <summary>
        /// the Inensity is in (0,1). 0 means no fire and 1 means the most intensive fire the system can present
        /// </summary>
        public float Intensity
        {
            get
            {
                return this.intensity;
            }

            set
            {
                if(value < 0)
                {
                    this.intensity = 0;
                }
                else if (value > 1)
                {
                    this.intensity = 1;
                }
                else
                {
                    this.intensity = value;
                    this.RefreshFireEmitter();
                    this.RefreshFire();
                    this.RefreshSmoke();
                }
            }
        }

        private float intensity;

        #region Fire

        public int MaxFireEmitters
        {
            get
            {
                int area = 0;
                if (this.outline != null)
                {
                    if(this.outline.Type == OutlineType.Circle)
                    {
                        area = (int)(Math.Pow(this.outline.Radius,2) * Math.PI);
                    }
                    else if (this.outline.Type == OutlineType.Rectangle)
                    {
                        Vector2 cross = this.outline.Max - this.outline.Min;
                        cross = Math2D.LocalToWorld(cross, Vector2.Zero, this.outline.Rotation);
                        area = (int)(cross.X * cross.Y);
                    }
                    else
                    {
                        return -1;
                    }
                    return ((float)area * FirePerSquare < 1.0f) ? 1 : (int)(area * FirePerSquare); 
                }
                else
                {
                    return -1;
                }
            }
        }

        private Random radom;
        #endregion

        #endregion

        #region Method
        public ParticleEffectOnFire(GameWorld world)
            : base(world)
        {
            this.radom = new Random();

            this.emittersFire = new List<ParticleEmitter>();
            this.emittersSmoke = new List<ParticleEmitter>();

            ParticleSettings firesettings = GetFireSettings();

            particleFire = ParticleSystem.Create("Fire"); ;
            particleFire.ParticleName = "Fire";
            this.Particles.Add(particleFire);

            ParticleSettings smokesettings = GetSmokeSettings();

            particleCenterSmoke = ParticleSystem.Create("Smoke");
            centerSmokeEmitter = new ParticleEmitter(particleCenterSmoke, 15, Position);

            particleCenterSmoke.ParticleName = "Smoke";
            centerSmokeEmitter.EmitterName = "Smoke";

            ParticleSettings fireSmokeSettings = this.GetFireSmokeSettings();

            particleFireSmoke = ParticleSystem.Create("FireSmoke");
            particleFireSmoke.ParticleName = "FireSmoke";

            this.Particles.Add(particleCenterSmoke);
            this.Particles.Add(particleFireSmoke);
            this.Emitters.Add(centerSmokeEmitter);
        }

        private ParticleSettings GetFireSettings()
        {
            ParticleSettings firesettings = new ParticleSettings();
            firesettings.TextureName = "Textures/Fire2";

            firesettings.MaxParticles = 1000;

            firesettings.Duration = 0.7f;

            firesettings.DurationRandomness = 1;

            firesettings.MinHorizontalVelocity = 0;
            firesettings.MaxHorizontalVelocity = 4;

            firesettings.MinVerticalVelocity = -4;
            firesettings.MaxVerticalVelocity = 4;

            // Set gravity upside down, so the flames will 'fall' upward.
            firesettings.Gravity = new Vector3(0, 0, 6);

            firesettings.MinColor = new Color(255, 255, 255, 40);
            firesettings.MaxColor = new Color(255, 255, 255, 120);

            firesettings.MinStartSize = 2;
            firesettings.MaxStartSize = 3;

            firesettings.MinEndSize = 6;
            firesettings.MaxEndSize = 10;

            firesettings.EndVelocity = 3;

            // Use additive blending.
            firesettings.SourceBlend = Blend.SourceAlpha;
            firesettings.DestinationBlend = Blend.One;

            return firesettings;
        }

        private ParticleSettings GetSmokeSettings()
        {
            ParticleSettings smokesettings = new ParticleSettings();

            smokesettings.DestinationBlend = Blend.InverseSourceAlpha;
            smokesettings.SourceBlend = Blend.SourceAlpha;

            smokesettings.Duration = 4;
            smokesettings.DurationRandomness = 1;

            smokesettings.MaxParticles = 1500;

            smokesettings.TextureName = "Textures/Smoke";

            smokesettings.MaxHorizontalVelocity = 6;
            smokesettings.MinHorizontalVelocity = 0;

            smokesettings.MaxVerticalVelocity = 6;
            smokesettings.MinVerticalVelocity = 4;

            smokesettings.Gravity = new Vector3(0, 0, 3);

            //smokesettings.MaxColor = new Color(200, 200, 200, 255);
            //smokesettings.MinColor = new Color(0, 0, 0, 255);

            smokesettings.MaxStartSize = 20;
            smokesettings.MaxEndSize = 100;

            smokesettings.MinStartSize = 10;
            smokesettings.MinEndSize = 100;

            smokesettings.EndVelocity = 1.0f;

            return smokesettings;
        }

        private ParticleSettings GetFireSmokeSettings()
        {
            ParticleSettings smokesettings = new ParticleSettings();

            smokesettings.DestinationBlend = Blend.InverseSourceAlpha;
            smokesettings.SourceBlend = Blend.SourceAlpha;

            smokesettings.Duration = 2;
            smokesettings.DurationRandomness = 0;

            smokesettings.MaxParticles = 1500;

            smokesettings.TextureName = "Textures/Smoke";

            smokesettings.MaxHorizontalVelocity = 3;
            smokesettings.MinHorizontalVelocity = 0;

            smokesettings.MaxVerticalVelocity = 3;
            smokesettings.MinVerticalVelocity = 3;

            smokesettings.Gravity = new Vector3(0, 0, 5);

            smokesettings.MaxColor = new Color(255, 255, 255, 255);
            //smokesettings.MinColor = new Color(0, 0, 0, 255);

            smokesettings.MaxStartSize = 2;
            smokesettings.MaxEndSize = 30;

            smokesettings.MinStartSize = 1;
            smokesettings.MinEndSize = 20;

            smokesettings.EndVelocity = 1.0f;

            return smokesettings;
        }

        private void RefreshFireEmitter()
        {
            if(this.outline != null)
            {
                int numEmitters = (int)(this.MaxFireEmitters * this.intensity) + 1;
                int currentEmitters = this.emittersFire.Count;
                if (numEmitters < currentEmitters)
                {
                    this.emittersFire.RemoveRange(numEmitters - 1, this.emittersFire.Count - numEmitters);
                    this.emittersSmoke.RemoveRange(numEmitters - 1, this.emittersSmoke.Count - numEmitters);
                }
                else
                {
                    if (this.outline.Type == OutlineType.Circle)
                    {
                        for (int i = 0; i < numEmitters - currentEmitters; i++)
                        {
                            float angle = (float)(360 * radom.NextDouble());
                            float radius = this.outline.Radius;
                            Vector3 emitterPosition = new Vector3(this.Position.X + (float)(radius * Math.Cos(angle)), this.Position.Y + (float)(radius * Math.Sin(angle)), this.Position.Z + 12);
                            bool tooNear = false;
                            foreach(ParticleEmitter emitter in this.emittersFire)
                            {
                                if((emitterPosition - emitter.PreviousPosition).Length() < 6)
                                {
                                    tooNear = true;
                                    break;
                                }
                            }
                            if (!tooNear)
                            {
                                this.emittersFire.Add(new ParticleEmitter(this.particleFire, 50, emitterPosition));
                                this.emittersSmoke.Add(new ParticleEmitter(this.particleFireSmoke, 50, emitterPosition));
                            }
                            else
                            {
                                i--;
                            }
                        }
                    }
                    else if(this.outline.Type == OutlineType.Rectangle)
                    {
                        for (int i = 0; i < numEmitters - currentEmitters; i++)
                        {
                            Vector2 dis = this.outline.Max - this.outline.Min;
                            Vector2 spc;
                            int r = radom.Next(4);
                            if(r == 0)
                            {
                                spc = new Vector2(dis.X * (float)radom.NextDouble(), 0);
                            }
                            else if (r == 1)
                            {
                                spc = new Vector2(0, dis.Y * (float)radom.NextDouble());
                            }
                            else if (r == 2)
                            {
                                spc = new Vector2(dis.X * (float)radom.NextDouble(), dis.Y);
                            }
                            else
                            {
                                spc = new Vector2(dis.X, dis.Y * (float)radom.NextDouble());
                            }

                            Vector3 emitterPosition = new Vector3(Math2D.LocalToWorld(this.outline.Min + spc, this.outline.Position, this.outline.Rotation), this.Position.Z+12);
                            bool tooNear = false;
                            foreach (ParticleEmitter emitter in this.emittersFire)
                            {
                                if ((emitterPosition - emitter.PreviousPosition).Length() < 6)
                                {
                                    tooNear = true;
                                    break;
                                }
                            }
                            if (!tooNear)
                            {
                                this.emittersFire.Add(new ParticleEmitter(this.particleFire, 50, emitterPosition));
                                this.emittersSmoke.Add(new ParticleEmitter(this.particleFireSmoke, 50, emitterPosition));
                            }
                            else
                            {
                                i--;
                            }

                        }
                    }
                }
                this.ResetSmokePosition();
            }
        }

        private void ResetSmokePosition()
        {
            Vector3 averagePosition = Vector3.Zero;
            foreach(ParticleEmitter emitter in this.emittersFire)
            {
                averagePosition += emitter.PreviousPosition;
            }
            this.smokePosition = averagePosition / this.emittersFire.Count;
            this.smokePosition.Z = this.Position.Z + 20;
        }

        private void RefreshFire()
        {
            if(this.emittersFire != null)
            {
                foreach(ParticleEmitter emitter in this.emittersFire)
                {
                    emitter.ParticlesPerSecond = MinFireParticlePerSecond + (MaxFireParticlePerSecond - MinFireParticlePerSecond) * this.intensity;
                }
            }
            if(this.particleFire != null)
            {
                particleFire.Settings.Duration = MinFireDuration + (MaxFireDuration - MinFireDuration) * this.intensity;
                particleFire.Settings.MaxEndSize = MinFireSize + (MaxFireSize - MinFireSize) * this.intensity;
                particleFire.Settings.MaxHorizontalVelocity = MinFireSpeed + (MaxFireSpeed - MinFireSpeed) * this.intensity;
                particleFire.Settings.MaxVerticalVelocity = particleFire.Settings.MaxHorizontalVelocity;
                particleFire.Settings.MinVerticalVelocity = -particleFire.Settings.MaxHorizontalVelocity;
                this.particleFire.Refresh();
            }
            
        }

        private void RefreshSmoke()
        {
            if(this.emittersSmoke != null)
            {
                foreach(ParticleEmitter emitter in this.emittersSmoke)
                {
                    emitter.ParticlesPerSecond = (MinFireParticlePerSecond + (MaxFireParticlePerSecond - MinFireParticlePerSecond) * this.intensity) / 2;
                }
            }
            if (this.centerSmokeEmitter != null)
            {
                this.centerSmokeEmitter.ParticlesPerSecond = MinSmokeParticlePerSecond + (MaxSmokeParticlePerSecond - MinSmokeParticlePerSecond) * intensity;
            }

            if(this.particleCenterSmoke != null)
            {
                this.particleCenterSmoke.Settings.Duration = MinSmokeDuration + (MaxSmokeDuration - MinSmokeDuration) * intensity;
                float revIntensity = 1 - intensity;
                this.particleCenterSmoke.Settings.MinColor = new Color((byte)(MinMinColor.R + (MaxMinColor.R - MinMinColor.R) * revIntensity), 
                                                                 (byte)(MinMinColor.G + (MaxMinColor.G - MinMinColor.G) * revIntensity), 
                                                                 (byte)(MinMinColor.B + (MaxMinColor.B - MinMinColor.B) * revIntensity), 
                                                                 255);
                this.particleCenterSmoke.Settings.MaxEndSize = MinSmokeSize + (MaxSmokeSize - MinSmokeSize) * intensity;
                this.particleCenterSmoke.Settings.MinEndSize = this.particleCenterSmoke.Settings.MaxEndSize / 5;
                this.particleCenterSmoke.Refresh();
            }
        }

        public override void Update(GameTime gameTime)
        {
            //if (centerSmokeEmitter != null)
            //{
            //    Vector3 distoringVec = new Vector3((float)(0.4 * radom.NextDouble() - 0.2), (float)(0.4 * radom.NextDouble() - 0.2), 0);
            //    centerSmokeEmitter.Update(gameTime, this.smokePosition);// + distoringVec);
            //}

            //foreach (ParticleEmitter emitter in this.emittersFire)
            //{
                
            //    if(emitter != null)
            //    {
            //        emitter.Update(gameTime, emitter.PreviousPosition);// + distoringVec);
            //    }
            //}

            //foreach(ParticleEmitter emitter in this.emittersSmoke)
            //{
            //    if(emitter != null)
            //    {
            //        emitter.Update(gameTime, emitter.PreviousPosition);
            //    }
            //}

            //foreach (ParticleSystem particle in this.Particles)
            //{
            //    if (particle != null)
            //        particle.Update(gameTime);
            //}
        }

        public override void Draw(GameTime gameTime)
        {
            if (this.particleFireSmoke != null)
            {
                this.particleFireSmoke.SetCamera(World.Game.View, World.Game.Projection);
                //this.particleFireSmoke.Draw(gameTime);
            }

            if (this.particleFire != null)
            {
                this.particleFire.SetCamera(World.Game.View, World.Game.Projection);
                //this.particleFire.Draw(gameTime);
            }


            if (this.particleCenterSmoke != null)
            {
                this.particleCenterSmoke.SetCamera(World.Game.View, World.Game.Projection);
               // this.particleCenterSmoke.Draw(gameTime);
            }
        }

        /// <summary>
        /// Deserialized attributes: ParticleSettings
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);
        }
        #endregion

        
    }

    #region PaticleEffectFireElemental

    public class ParticleEffectFireElemental : ParticleEffect
    {
        #region Fields

        ParticleSystem particleFire;
        List<ParticleEmitter> emittersFire;

        List<Vector3> pointList;

        public List<Vector3> PointList
        {
            get
            {
                return pointList;
            }
            set
            {
                if (value.Count % 2 == 0)
                {
                    this.pointList = value;
                }
                else
                {
                    throw new Exception("Fuck! The List Count is ODD!");
                }
            }
        }

        public Vector3 Direction
        {
            get
            {
                return this.direction;
            }
            set
            {
                this.direction = Vector3.Normalize(value);
            }
        }

        private Vector3 direction;

        Vector3 center;
        float angle;

        Random random = new Random();

        #endregion
        
        #region Methods

        public ParticleEffectFireElemental(GameWorld world)
            : base(world)
        {
            
            ParticleSettings fireSetting = GetFireSettings();
            this.particleFire = ParticleSystem.Create("Fire"); ;
            this.angle = 0;
            this.center = new Vector3(1024,1024,150);
            this.emittersFire = new List<ParticleEmitter>();

        }

        private ParticleSettings GetFireSettings()
        {

            ParticleSettings settings = new ParticleSettings();

            settings.SourceBlend = Blend.SourceAlpha;
            settings.DestinationBlend = Blend.One;

            settings.MaxStartSize = 4f;
            settings.MinStartSize = 2f;

            settings.MinEndSize = 0.1f;
            settings.MaxEndSize = 0.3f;

            settings.MaxVerticalVelocity = 0.0f;
            settings.MinVerticalVelocity = 0.0f;

            settings.MinHorizontalVelocity = 0.1f;
            settings.MaxHorizontalVelocity = 0.1f;

            //settings.MinColor = Color.White;
            //settings.MaxColor = Color.White;

            settings.MaxColor = new Color(255, 255, 255, 100);
            settings.MinColor = new Color(255, 255, 255, 60);

            settings.MaxRotateSpeed = 1;
            settings.MinRotateSpeed = -1;

            settings.TextureName = "Textures/fire4";

            settings.DurationRandomness = 0;

            settings.Duration = 1.0f;

            settings.MaxParticles = 10000;

            settings.EndVelocity = 0.0f;

            return settings;
        }

        private void RefreshGravity()
        {
            //this.particleFire.Settings.Gravity = -this.direction*50;
            //this.particleFire.Refresh();
        }

        public void RefreshEmitters()
        {
            int currentEmitters = this.emittersFire.Count;
            int numFireEmitters = this.CaculateNumEmitters();
            if ( currentEmitters > numFireEmitters)
            {
                this.emittersFire.RemoveRange(numFireEmitters - 1, currentEmitters - numFireEmitters);
            }
            else if(currentEmitters < numFireEmitters)
            {
                for (int i = 0; i < numFireEmitters - currentEmitters; i++)
                {
                    ParticleEmitter emitter = new ParticleEmitter(this.particleFire, 30, this.Position);
                    this.emittersFire.Add(emitter);
                }
            }
        }

        private int CaculateNumEmitters()
        {
            return this.pointList.Count;
        }

        
        public override void Update(GameTime gameTime)
        {
            this.Position = Vector3.Zero;
            foreach(Vector3 pos in this.pointList)
            {
                this.Position += pos;
            }
            this.Position /= this.pointList.Count;
            float length = (this.Position - this.center).Length();
            Vector3 offset = new Vector3((float)Math.Sin(length / 4) * 0.01f, 0.005f, 0.005f) * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            for (int i = 0; i < this.pointList.Count; i++ )
            {
                this.pointList[i] += offset;
            }
            for (int i = 0; i < this.pointList.Count; i++ )
            {
                this.emittersFire[i].Update(gameTime, this.pointList[i]);
            }
            if(this.particleFire != null)
            {
                particleFire.Update(gameTime);
            }
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (this.particleFire != null)
            {
                this.particleFire.SetCamera(World.Game.View, World.Game.Projection);
                //this.particleFire.Draw(gameTime);
            }
            base.Draw(gameTime);
        }

        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);
        }



        #endregion
    }

    #endregion

    #endregion

    #region ParticleEffectBuildingSmoke

    public class ParticleEffectBuildingSmoke : ParticleEffect
    {
        #region variables

        ParticleSystem particleSomke;

        ParticleEmitter emitterSmoke;

        Outline outLineSmoke;

        Outline outLineSmokeShrinked;

        BuildingSmokeState state;

        Random random;

        float maxHeight = 0;

        float cubic = 0;

        float fadingRate = 1;

        #endregion

        #region properties

        /// <summary>
        /// Gets the particle system of smoke
        /// </summary>
        public ParticleSystem ParticleSmoke
        {
            get
            {
                return this.particleSomke;
            }
        }

        /// <summary>
        /// Gets the emitter of smoke
        /// </summary>
        public ParticleEmitter EmitterSmoke
        {
            get
            {
                return this.emitterSmoke;
            }
        }

        /// <summary>
        /// Gets the outline where smoke locate
        /// </summary>
        public Outline SomkeOutline
        {
            get
            {
                return this.outLineSmoke;
            }

            set
            {
                this.outLineSmoke = value;
                this.outLineSmokeShrinked = this.outLineSmoke * 0.8f;
                this.RefreshCubic();
            }
        }

        /// <summary>
        /// Gets or sets the state of smoke
        /// </summary>
        public BuildingSmokeState SmokeState
        {
            get
            {
                return this.state;
            }
            set
            {
                this.state = value;
            }
        }

        /// <summary>
        /// Gets or set the fading rate
        /// </summary>
        public float FadingRate
        {
            get
            {
                return this.fadingRate;
            }
            set
            {
                this.fadingRate = value;
            }
        }

        /// <summary>
        /// Gets or sets the max height of smoke
        /// </summary>
        public float MaxHeight
        {
            get
            {
                return this.maxHeight + 4;
            }
            set
            {
                this.maxHeight = value - 4;
                this.RefreshCubic();
            }
        }

        #endregion

        #region method

        public ParticleEffectBuildingSmoke(GameWorld world) : base(world)
        {
            this.random = new Random();

            this.state = BuildingSmokeState.Standard;

            this.particleSomke = ParticleSystem.Create("Smoke");
            this.emitterSmoke = new ParticleEmitter(this.particleSomke, 30, this.Position);
        }

        public ParticleSettings GetSomkeSettings()
        {
            ParticleSettings setting = new ParticleSettings();

            setting.Duration = 4;

            setting.SourceBlend = Blend.SourceAlpha;
            setting.DestinationBlend = Blend.InverseSourceAlpha;

            setting.MaxColor = new Color(255,255,255,255);
            setting.MinColor = new Color(200,200,200,255);

            setting.MaxHorizontalVelocity = 0.5f;
            setting.MinHorizontalVelocity = 0.5f;

            setting.MaxVerticalVelocity = 5f;
            setting.MinVerticalVelocity = 1f;

            setting.MaxParticles = 500;

            setting.Gravity = new Vector3(0,0,-1f);

            setting.MaxStartSize = 14.0f;
            setting.MinStartSize = 14.0f;

            setting.MinEndSize = 18.0f;
            setting.MaxEndSize = 18.0f;

            setting.MaxRotateSpeed = 3f;
            setting.MinRotateSpeed = 2f;

            setting.DurationRandomness = 0.5f;

            setting.EndVelocity = 0.0f;

            setting.TextureName = "Textures/smoke";

            return setting;
        }

        private void RefreshCubic()
        {
            this.cubic = this.outLineSmokeShrinked.Area * this.maxHeight;
        }

        #endregion

        #region Update & Draw

        /// <summary>
        /// Let the Rate of smoke particle per second fit the generating cubic
        /// </summary>
        private void UpdateGeneratingRate()
        {
            if(this.state == BuildingSmokeState.Standard)
            {
                this.emitterSmoke.ParticlesPerSecond = 0.005f * this.cubic + 10;
            }
            else if (this.state == BuildingSmokeState.Fading)
            {
                this.emitterSmoke.ParticlesPerSecond = (0.005f * this.cubic + 10) * this.fadingRate;
                if (this.emitterSmoke.ParticlesPerSecond < 5)
                {
                    this.emitterSmoke.ParticlesPerSecond = 5;
                }
            }
            else
            {
                this.emitterSmoke.ParticlesPerSecond = 2;
            }
        }

        private void UpdateColor()
        {
            if(this.state == BuildingSmokeState.Fading)
            {
                byte val = (byte)(255 - 55 * this.fadingRate);
                this.particleSomke.Settings.MinColor = new Color(val,val,val,255);
            }
        }

        /// <summary>
        /// Update Smoke
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            this.UpdateGeneratingRate();
            this.UpdateColor();
            float r_x, r_y;
            Vector3 smokePosition;
            if (this.outLineSmoke.Type == OutlineType.Rectangle)
            {
                r_x = this.outLineSmokeShrinked.Min.X + (this.outLineSmokeShrinked.Max.X - this.outLineSmokeShrinked.Min.X) * (float)random.NextDouble();
                r_y = this.outLineSmokeShrinked.Min.Y + (this.outLineSmokeShrinked.Max.Y - this.outLineSmokeShrinked.Min.Y) * (float)random.NextDouble();
                Vector2 temp = Math2D.LocalToWorld(new Vector2(r_x, r_y), this.outLineSmokeShrinked.Position, this.outLineSmokeShrinked.Rotation);
                smokePosition = new Vector3(temp, this.Position.Z + (float)random.NextDouble() * this.maxHeight + 4);
            }
            else if (this.outLineSmoke.Type == OutlineType.Circle)
            {
                double angle = this.random.NextDouble() * 360;
                r_x = (float)(this.outLineSmokeShrinked.Radius * Math.Cos(angle));
                r_y = (float)(this.outLineSmokeShrinked.Radius * Math.Sin(angle));
                smokePosition = new Vector3(new Vector2(r_x, r_y) + this.outLineSmokeShrinked.Position, this.Position.Z + (float)random.NextDouble() * this.maxHeight + 4);
            }
            else
            {
                throw new Exception("妈的个！ 连outline都不对！");
            }
            if(this.emitterSmoke != null)
            {
                this.emitterSmoke.Update(gameTime, smokePosition);
            }
            if(this.particleSomke != null)
            {
                this.particleSomke.Update(gameTime);
            }
        }

        /// <summary>
        /// Add to manager and draw
        /// </summary>
        /// <param name="gameTime"></param>
        public override void  Draw(GameTime gameTime)
        {
            if(this.particleSomke != null)
            {
                this.particleSomke.SetCamera(World.Game.View, World.Game.Projection);
                //this.particleSomke.Draw(gameTime);
            }
        
        }
        
        #endregion
    }

    #region BuildingSmokeState

    public enum BuildingSmokeState
    {
        /// <summary>
        /// Building in progress
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Nearly down
        /// </summary>
        Fading = 1,

        /// <summary>
        /// Construction Complete
        /// </summary>
        End = 2
    }

    #endregion

    #endregion
#endregion
#endif

    #region Arrow
    public enum ArrowState
    {
        Waiting = 0,
        Flying = 1,
        Fading = 2,
        End = 3
    }

    public class Arrow : BaseEntity
    {
        #region variable

        private Vector3 destination;

        private Vector3 source;

        private TrailEffect trail;

        private Vector3 acceleration;

        private Vector3 velocity;

        private float maxAcceleration;

        private float maxSpeed;

        private ArrowState state;

        private float fadingAge = 0;

        #endregion

        #region property

        /// <summary>
        /// The target the arrow aims
        /// </summary>
        public Vector3 Destination
        {
            get
            {
                return this.destination;
            }
            set
            {
                this.destination = value;
            }
        }

        /// <summary>
        /// The source position of arrow
        /// </summary>
        public Vector3 Source
        {
            get
            {
                return this.source;
            }
            set
            {
                this.source = value;
            }
        }

        public ArrowState State
        {
            get
            {
                return this.state;
            }
        }

        public float MaxAcceleration
        {
            get { return this.maxAcceleration; }
            set { this.maxAcceleration = value; }
        }

        public float MaxSpeed
        {
            get { return this.MaxSpeed; }
            set { this.maxSpeed = value; }
        }

        public Vector3 Accerlation
        {
            get { return this.acceleration; }
        }

        #endregion

        #region method

        #region Initialization

        public Arrow(GameWorld world)
            : base(world)
        {
            this.RestoreDefault();
            this.trail = new TrailEffect();
            this.trail.Length = 5;
            this.trail.Width = 2;
            this.trail.Texture = BaseGame.Singleton.ZipContent.Load<Texture2D>("Textures/ray2");


        }

        public void Launch()
        {
            this.source = this.Position;
            this.state = ArrowState.Flying;
            this.trail.Launch();
            this.velocity = Vector3.Normalize(this.destination - this.Position) * this.maxSpeed / 12;
            this.velocity.Z += this.maxSpeed / 1.5f;
        }

        private void RestoreDefault()
        {
            this.maxAcceleration = 0.0003f;
            this.maxSpeed = 0.08f;
            this.state = ArrowState.Waiting;
        }

        #endregion

        #region Update and Draw

        public override void Update(GameTime gameTime)
        {
            if (this.state == ArrowState.Flying)
            {
                this.UpdateVelocity(gameTime);
                this.UpdatePosition(gameTime);
                this.trail.Position = this.Position;
                this.trail.Update(gameTime);
                if ((this.Position - this.destination).Length() <= 4f || Vector3.Dot((this.Position - this.destination), ((this.destination - this.source))) > 0)
                {
                    this.state = ArrowState.Fading;
                }
            }
            if (this.state == ArrowState.Fading)
            {
                this.UpdatePosition(gameTime);
                this.fadingAge += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                this.trail.Alpha = 1 - this.fadingAge / 300;
                if (this.fadingAge >= 300)
                {
                    this.state = ArrowState.End;
                    this.NotifyEnd();
                }
            }
        }

        private void UpdatePosition(GameTime gameTime)
        {
            this.Position += this.velocity * gameTime.ElapsedGameTime.Milliseconds;
        }

        private void UpdateVelocity(GameTime gameTime)
        {
            this.UpdateAcceleration(gameTime);
            this.velocity += this.acceleration * gameTime.ElapsedGameTime.Milliseconds;
            if (this.velocity.Length() > this.maxSpeed)
            {
                this.velocity.Normalize();
                this.velocity *= this.maxSpeed;
            }
        }

        private void UpdateAcceleration(GameTime gameTime)
        {
            Vector3 interval = this.destination - this.Position;
            Vector3 interval2 = this.Position - this.source;
            Vector3 desiredVelocity = Vector3.Normalize(interval) * interval2.Length() / this.maxSpeed;
            this.acceleration = (desiredVelocity - this.velocity) / gameTime.ElapsedGameTime.Milliseconds;
            if (this.acceleration.Length() > this.maxAcceleration && (this.Position - this.destination).Length() > 20)
            {
                this.acceleration.Normalize();
                this.acceleration *= this.maxAcceleration;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (this.trail != null)
            {
                this.trail.SetCamera(BaseGame.Singleton.View, BaseGame.Singleton.Projection);
                this.trail.Draw(gameTime);
            }
        }

        private void NotifyEnd()
        {
            GameServer.Singleton.Destroy(this);
        }

        #endregion

        #endregion
    }
    #endregion

    #region AreaEmitter
    /// <summary>
    /// Generate particles randomly within the specified area
    /// </summary>
    public class AreaEmitter : ParticleEmitter
    {
        /// <summary>
        /// Gets or sets the destination area
        /// </summary>
        public Outline Area;

        public float MinimumHeight;
        public float MaximumHeight;

        /// <summary>
        /// Creates a new area emitter
        /// </summary>
        public AreaEmitter(ParticleSystem particleSystem,  float particlesPerSecond,
                           Outline area, float minHeight, float maxHeight)
            : base(particleSystem, particlesPerSecond,
                new Vector3(area.Position.X, area.Position.Y, 
                    Helper.RandomInRange(minHeight, maxHeight)))
        {
            Area = area;
            MinimumHeight = minHeight;
            MaximumHeight = maxHeight;
        }

        public override void Update(GameTime gameTime)
        {
            Vector3 position = Vector3.Zero;

            position.Z = Helper.RandomInRange(MinimumHeight, MaximumHeight);

            // Find a random point on the outline
            if (Area.Type == OutlineType.Circle)
            {
                float angle = Helper.RandomInRange(0, 2 * MathHelper.Pi);
                float radius = Helper.RandomInRange(0, Area.Radius);

                position.X = Area.Position.X + radius * (float)Math.Cos(angle);
                position.Y = Area.Position.Y + radius * (float)Math.Sin(angle);
            }
            else if (Area.Type == OutlineType.Rectangle)
            {
                Vector2 local;
                
                local.X = Helper.RandomInRange(Area.Min.X, Area.Max.X);
                local.Y = Helper.RandomInRange(Area.Min.Y, Area.Max.Y);

                Vector2 world = Math2D.LocalToWorld(local, Area.Position, Area.Rotation);

                position.X = world.X;
                position.Y = world.Y;
            }

            Update(gameTime, position, Vector3.Zero, true);
        }
    }
    #endregion

    #region CircularEmitter
    public class CircularEmitter : ParticleEmitter
    {
        /// <summary>
        /// Gets or sets the destination area
        /// </summary>
        public Vector3 Position;
        public float Radius;

        /// <summary>
        /// Creates a new area emitter
        /// </summary>
        public CircularEmitter(ParticleSystem particleSystem, float particlesPerSecond)
            : base(particleSystem, particlesPerSecond, Vector3.Zero) { }

        public CircularEmitter(ParticleSystem particleSystem, float particlesPerSecond,
                               Outline area, float height)
            : base(particleSystem, particlesPerSecond,
                new Vector3(area.Position.X, area.Position.Y, height))
        {
            Position.X = area.Position.X;
            Position.Y = area.Position.Y;
            Position.Z = height;

            if (area.Type == OutlineType.Circle)
                Radius = area.Radius;
            else if (area.Type == OutlineType.Rectangle)
                Radius = Vector2.Subtract(area.Max, area.Min).Length() / 2;

        }

        public override void Update(GameTime gameTime)
        {
            Vector3 position = Vector3.Zero;

            // Find a random point on the circle
            float angle = Helper.RandomInRange(0, 2 * MathHelper.Pi);
            float radius = Helper.RandomInRange(Radius * 0.9f, Radius * 1.1f);

            position.X = Position.X + radius * (float)Math.Cos(angle);
            position.Y = Position.Y + radius * (float)Math.Sin(angle);
            position.Z = Helper.RandomInRange(Position.Z, Position.Z + 5);

            Update(gameTime, position, Vector3.Zero, true);
        }

        public override void Update(GameTime gameTime, Vector3 newPosition)
        {
            Position = newPosition;

            Update(gameTime);
        }
    }
    #endregion

    #region ProjectileEmitter
    public class ProjectileEmitter : ParticleEmitter, IProjectile
    {
        Vector3 position;

        Vector3 velocity;

        IWorldObject target;

        public IWorldObject Target
        {
            get { return target; }
        }

        public Vector3 Position
        {
            get { return position; }
        }

        public Vector3 Velocity
        {
            get { return velocity; }
        }
        
        public event EventHandler Hit;

        public float MaxSpeed = 120;
        public float MaxForce = 500;
        public float Mass = 0.4f;

        /// <summary>
        /// Create a new projectile emitter
        /// </summary>
        public ProjectileEmitter(ParticleSystem particleSystem, float particlesPerSecond,
                                 Vector3 initialPosition, Vector3 initialVelocity, IWorldObject target)
            : base(particleSystem, particlesPerSecond, initialPosition)
        {
            this.target = target;
            this.position = initialPosition;
            this.velocity = initialVelocity;
        }


        public override void Update(GameTime gameTime)
        {
            Vector3 destination;

            destination.X = target.Position.X;
            destination.Y = target.Position.Y;
            destination.Z = target.BoundingBox.Max.Z;

            // For test only
            if (destination.Z <= 0)
                destination.Z = target.Position.Z;

            // Creates a force that steer the emitter towards the target position
            Vector3 desiredVelocity = destination - position;

            desiredVelocity.Normalize();
            desiredVelocity *= MaxSpeed;

            Vector3 force = desiredVelocity - velocity;

            if (force.Length() > MaxForce)
            {
                force.Normalize();
                force *= MaxForce;
            }

            // Update velocity & position
            float elapsedSecond = (float)gameTime.ElapsedGameTime.TotalSeconds;

            velocity += elapsedSecond / Mass * force;
            position += elapsedSecond * velocity;

            // Hit test
            Vector2 toTarget, facing;

            toTarget.X = destination.X - Position.X;
            toTarget.Y = destination.Y - Position.Y;

            facing.X = velocity.X;
            facing.Y = velocity.Y;

            if (Vector2.Dot(toTarget, facing) <= 0)
            {
                if (Hit != null)
                    Hit(this, null);
            }

            base.Update(gameTime, position, Vector3.Zero, true);
        }
    }
    #endregion

    #region EffectTest
    public class EffectTest : ParticleEffect
    {
        ProjectileEmitter emitter;
        ParticleSystem particle;

        public override ParticleSystem Particle
        {
            get { return particle; }
        }

        public override float Emission
        {
            get { return emitter.ParticlesPerSecond; }
            set { emitter.ParticlesPerSecond = value; }
        }

        public EffectTest(GameWorld world, IWorldObject target, Vector3 position)
            : base(world)
        {
            particle = ParticleSystem.Create("Fireball");
            emitter = new ProjectileEmitter(particle, 100, position, Vector3.Zero, target);
        }

        public override void Update(GameTime gameTime)
        {
            emitter.Update(gameTime);
        }
    }
    #endregion

    #region EffectConstruct
    public class EffectConstruct : ParticleEffect
    {
        AreaEmitter emitter;
        ParticleSystem particle;

        public override ParticleSystem Particle
        {
            get { return particle; }
        }

        public override float Emission
        {
            get { return emitter.ParticlesPerSecond; }
            set { emitter.ParticlesPerSecond = value; }
        }

        public EffectConstruct(GameWorld world, Outline outline, float minHeight, float maxHeight)
            : base(world)
        {
            particle = ParticleSystem.Create("Construct");
            emitter = new AreaEmitter(particle, 0.075f * outline.Area, outline, minHeight, maxHeight);
        }

        public override void Update(GameTime gameTime)
        {
            emitter.Update(gameTime);
        }
    }
    #endregion

    #region EffectFire
    public class EffectFire : ParticleEffect
    {
        ParticleSystem fire;
        ParticleSystem smoke;
        ParticleEmitter fireEmitter;
        ParticleEmitter smokeEmitter;

        public override ParticleSystem Particle
        {
            get { return smoke; }
        }

        public override float Emission
        {
            get { return smokeEmitter.ParticlesPerSecond; }
            set { smokeEmitter.ParticlesPerSecond = value; }
        }

        public EffectFire(GameWorld world)
            : this(world, Vector3.Zero) { }

        public EffectFire(GameWorld world, Vector3 position)
            : base(world)
        {
            Position = position;
            fire = ParticleSystem.Create("Fire");
            smoke = ParticleSystem.Create("FireSmoke");
            fireEmitter = new ParticleEmitter(fire, 64, Position);
            smokeEmitter = new ParticleEmitter(smoke, 12, Position + Vector3.UnitZ * 4);
        }

        public override void Update(GameTime gameTime)
        {
            fireEmitter.Update(gameTime, Position, Vector3.Zero, true);
            smokeEmitter.Update(gameTime, Position + Vector3.UnitZ * 4, Vector3.Zero, true);
        }
    }
    #endregion

    #region EffectFireball
    public class EffectFireball : ParticleEffect
    {
        ParticleSystem fire;
        ParticleSystem explosion;
        ProjectileEmitter fireEmitter;

        public IProjectile Projectile
        {
            get { return fireEmitter; }
        }

        public override ParticleSystem Particle
        {
            get { return explosion; }
        }

        public override float Emission
        {
            get { return fireEmitter.ParticlesPerSecond; }
            set { fireEmitter.ParticlesPerSecond = value; }
        }

        public EffectFireball(GameWorld world, Vector3 position, Vector3 velocity, IWorldObject target)
            : this(world, position, velocity, target, "Fireball", "FireballExplosion") { }

        public EffectFireball(GameWorld world, Vector3 position, Vector3 velocity, IWorldObject target,
                              string fireballParticle, string explosionParticle)
            : base(world)
        {
            fire = ParticleSystem.Create(fireballParticle);
            explosion = ParticleSystem.Create(explosionParticle);
            fireEmitter = new ProjectileEmitter(fire, 150, position, velocity, target);
            fireEmitter.Hit += new EventHandler(delegate(object sender, EventArgs e)
            {
                // Fill up the particle system
                int n = (int)Helper.RandomInRange(20, 30);
                for (int i = 0; i < n; i++)
                {
                    explosion.AddParticle(fireEmitter.Position, fireEmitter.Velocity);
                }

                GameServer.Singleton.Destroy(this);
            });
        }

        public override void Update(GameTime gameTime)
        {
            fireEmitter.Update(gameTime);
        }
    }
    #endregion

    #region EffectExplosion
    public class EffectExplosion : ParticleEffect
    {
        ParticleSystem fire;
        ParticleSystem smoke;
        ParticleSystem spark;

        int ParticleCount = 50;

        public override ParticleSystem Particle
        {
            get { return spark; }
        }

        public override float Emission
        {
            get { return ParticleCount; }
            set { ParticleCount = (int)value; }
        }

        public EffectExplosion(GameWorld world)
            : this(world, Vector3.Zero) { }

        public EffectExplosion(GameWorld world, Vector3 position)
            : base(world)
        {
            Position = position;
            fire = ParticleSystem.Create("Explosion");
            smoke = ParticleSystem.Create("ExplosionSmoke");
            spark = ParticleSystem.Create("Spark");

            Trigger();
        }

        void Trigger()
        {
            for (int i = 0; i < 100; i++)
                fire.AddParticle(Position, Vector3.Zero);

            for (int i = 0; i < 100; i++)
                smoke.AddParticle(Position, Vector3.Zero);

            for (int i = 0; i < 40; i++)
                spark.AddParticle(Position, Vector3.Zero);
        }

        int counter;

        public override void Update(GameTime gameTime)
        {
            if (counter++ > 80)
            {
                Trigger();
                counter = 0;
            }

            //fireEmitter.Update(gameTime, Position, Vector3.Zero, true);
            //smokeEmitter.Update(gameTime, Position + Vector3.UnitZ * 4, Vector3.Zero);
        }
    }
    #endregion
    
    #region EffectStar
    public class EffectStar : ParticleEffect
    {
        GameObject target;
        ParticleSystem particle;
        CircularEmitter emitter;

        public override ParticleSystem Particle
        {
            get { return particle; }
        }

        public override float Emission
        {
            get { return emitter.ParticlesPerSecond; }
            set { emitter.ParticlesPerSecond = value; }
        }

        public EffectStar(GameWorld world)
            : this(world, null) { }

        public EffectStar(GameWorld world, GameObject gameObject)
            : base(world)
        {
            if (gameObject != null)
            {
                target = gameObject;
                Position = gameObject.Position;
            }

            particle = ParticleSystem.Create("Star");
            emitter = new CircularEmitter(particle, 12, gameObject.Outline * 0.75f, 
                                          gameObject.Position.Z);
        }

        public override void Update(GameTime gameTime)
        {
            if (target != null)
                Position = target.Position;

            emitter.Update(gameTime, Position);
        }
    }
    #endregion

    #region EffectGlow
    public class EffectGlow : ParticleEffect
    {
        GameObject target;
        ParticleSystem particle;
        ParticleEmitter emitter;

        public override ParticleSystem Particle
        {
            get { return particle; }
        }

        public override float Emission
        {
            get { return emitter.ParticlesPerSecond; }
            set { emitter.ParticlesPerSecond = value; }
        }

        public EffectGlow(GameWorld world)
            : this(world, null) { }

        public EffectGlow(GameWorld world, GameObject gameObject)
            : base(world)
        {
            if (gameObject != null)
            {
                target = gameObject;
                Position = gameObject.Position;
            }

            particle = ParticleSystem.Create("Glow");
            emitter = new ParticleEmitter(particle, 2, gameObject.Position);
        }

        public override void Update(GameTime gameTime)
        {
            if (target != null)
            {
                Vector3 position = target.Position;
                position.Z = MathHelper.Lerp(target.Position.Z, target.TopCenter.Z, 0.3f);
                Position = position;
            }

            emitter.Update(gameTime, Position, Vector3.Zero, true);
        }
    }
    #endregion

    #region EffectPunishOfNature
    public class EffectPunishOfNature : ParticleEffect
    {
        const float DropSpeed = 50;
        const float Height = 200;
        public const float Radius = 100;
        const int MaxRainDrops = 60;

        ParticleSystem rain;
        ParticleEmitter[] rainEmitters = new ParticleEmitter[MaxRainDrops];
        float[] sleepTimes = new float[MaxRainDrops];

        public override ParticleSystem Particle
        {
            get { return rain; }
        }

        public override float Emission
        {
            get { return 0; }
            set { }
        }

        public EffectPunishOfNature(GameWorld world, Vector3 position)
            : base(world)
        {
            Position = position;

            rain = ParticleSystem.Create("PunishOfNature");

            for (int i = 0; i < rainEmitters.Length; i++)
            {
                rainEmitters[i] = new ParticleEmitter(rain, 400, RandomPosition());
                sleepTimes[i] = Helper.RandomInRange(0, 10);
            }
        }

        Vector3 RandomPosition()
        {
            Vector3 v;

            float angle = Helper.RandomInRange(0, 2 * MathHelper.Pi);
            float radius = Helper.RandomInRange(0, Radius);

            v.X = Position.X + radius * (float)Math.Cos(angle);
            v.Y = Position.Y + radius * (float)Math.Sin(angle);
            v.Z = Position.Z + Height;

            return v;
        }

        public override void Update(GameTime gameTime)
        {
            Vector3 dropAmount = Vector3.Zero;

            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            dropAmount.Z = DropSpeed * elapsedSeconds;

            for (int i = 0; i < rainEmitters.Length; i++)
            {
                if (sleepTimes[i] > 0)
                {
                    sleepTimes[i] -= elapsedSeconds;
                }
                else
                {
                    Vector3 newPosition = rainEmitters[i].PreviousPosition - dropAmount;

                    if (newPosition.Z < 0)
                    {
                        sleepTimes[i] = Helper.RandomInRange(0, 10);
                        rainEmitters[i].PreviousPosition = newPosition = RandomPosition();
                    }
                    else
                    {
                        rainEmitters[i].Update(gameTime, newPosition, Vector3.Zero, true);
                    }
                }
            }
        }
    }
    #endregion

    #region EffectHalo
    public class EffectHalo : ParticleEffect
    {
        float angle = 0;
        public float Speed = 2.0f;
        public float Radius;

        Vector3 spawn;

        ParticleEmitter emitter;
        ParticleSystem particle;

        public override ParticleSystem Particle
        {
            get { return particle; }
        }

        public override float Emission
        {
            get { return emitter.ParticlesPerSecond; }
            set { emitter.ParticlesPerSecond = value; }
        }

        public EffectHalo(GameWorld world, Vector3 position, float radius, string particleSystem)
            : base(world)
        {
            Radius = radius;
            Position = position;
            position.X += radius;
            spawn = position;
            particle = ParticleSystem.Create(particleSystem);
            emitter = new ParticleEmitter(particle, 150, spawn);
        }

        public override void Update(GameTime gameTime)
        {
            angle += Speed * (float)(gameTime.ElapsedGameTime.TotalSeconds);

            spawn.X = Position.X + (float)(Radius * Math.Cos(angle));
            spawn.Y = Position.Y + (float)(Radius * Math.Sin(angle));

            emitter.Update(gameTime, spawn);
        }
    }
    #endregion

    #region EffectSpawn
    public class EffectSpawn : ParticleEffect
    {
        float angle = 0;
        public float Speed = 4.0f;
        public float Radius;

        Vector3 spawn;

        const int Count = 5;

        ParticleEmitter[] emitters;
        ParticleSystem particle;

        public override ParticleSystem Particle
        {
            get { return particle; }
        }

        public override float Emission
        {
            get { return 0; }
            set {  }
        }

        public EffectSpawn(GameWorld world, Vector3 position, float radius, string particleSystem)
            : base(world)
        {
            Radius = radius;
            Position = position;
            position.X += radius;
            spawn = position;
            particle = ParticleSystem.Create(particleSystem);
            emitters = new ParticleEmitter[Count];

            for (int i= 0; i < emitters.Length; i++)
                emitters[i] = new ParticleEmitter(particle, 150, spawn);
        }

        public override void Update(GameTime gameTime)
        {
            float elapsedSeconds = (float)(gameTime.ElapsedGameTime.TotalSeconds);
            angle += Speed * elapsedSeconds;
            spawn.Z += 18.0f * elapsedSeconds;

            for (int i = 0; i < Count; i++)
            {
                float realAngle = angle + i * MathHelper.Pi * 2 / Count;

                spawn.X = Position.X + (float)(Radius * Math.Cos(realAngle));
                spawn.Y = Position.Y + (float)(Radius * Math.Sin(realAngle));

                emitters[i].Update(gameTime, spawn);
            }

            if (spawn.Z - Position.Z > 15)
                GameServer.Singleton.Destroy(this);
        }
    }
    #endregion
}
