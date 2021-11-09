
namespace Isles;

public abstract class ParticleEffect : BaseEntity
{
    public ParticleEffect(GameWorld world)
        : base(world) { }

    public override void Update(GameTime gameTime) { }

    public override void Draw(GameTime gameTime) { }
}

/// <summary>
/// Generate particles randomly within the specified area.
/// </summary>
public class AreaEmitter : ParticleEmitter
{
    /// <summary>
    /// Gets or sets the destination area.
    /// </summary>
    public Outline Area;

    public float MinimumHeight;
    public float MaximumHeight;

    public AreaEmitter(ParticleSystem particleSystem, float particlesPerSecond,
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
            var angle = Helper.RandomInRange(0, 2 * MathHelper.Pi);
            var radius = Helper.RandomInRange(0, Area.Radius);

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

public class CircularEmitter : ParticleEmitter
{
    /// <summary>
    /// Gets or sets the destination area.
    /// </summary>
    public Vector3 Position;
    public float Radius;

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
        {
            Radius = area.Radius;
        }
        else if (area.Type == OutlineType.Rectangle)
        {
            Radius = Vector2.Subtract(area.Max, area.Min).Length() / 2;
        }
    }

    public override void Update(GameTime gameTime)
    {
        Vector3 position = Vector3.Zero;

        // Find a random point on the circle
        var angle = Helper.RandomInRange(0, 2 * MathHelper.Pi);
        var radius = Helper.RandomInRange(Radius * 0.9f, Radius * 1.1f);

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

public class ProjectileEmitter : ParticleEmitter, IProjectile
{
    private Vector3 position;
    private Vector3 velocity;

    public BaseEntity Target { get; }

    public Vector3 Position => position;

    public Vector3 Velocity => velocity;

    public event EventHandler Hit;

    public float MaxSpeed = 120;
    public float MaxForce = 500;
    public float Mass = 0.4f;

    /// <summary>
    /// Create a new projectile emitter.
    /// </summary>
    public ProjectileEmitter(ParticleSystem particleSystem, float particlesPerSecond,
                             Vector3 initialPosition, Vector3 initialVelocity, BaseEntity target)
        : base(particleSystem, particlesPerSecond, initialPosition)
    {
        Target = target;
        position = initialPosition;
        velocity = initialVelocity;
    }

    public override void Update(GameTime gameTime)
    {
        Vector3 destination;

        destination.X = Target.Position.X;
        destination.Y = Target.Position.Y;
        destination.Z = Target.BoundingBox.Max.Z;

        // For test only
        if (destination.Z <= 0)
        {
            destination.Z = Target.Position.Z;
        }

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
        var elapsedSecond = (float)gameTime.ElapsedGameTime.TotalSeconds;

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
            Hit?.Invoke(this, null);
        }

        base.Update(gameTime, position, Vector3.Zero, true);
    }
}

public class EffectConstruct : ParticleEffect
{
    private readonly AreaEmitter emitter;
    private readonly ParticleSystem particle;

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

public class EffectFire : ParticleEffect
{
    private readonly ParticleSystem fire;
    private readonly ParticleSystem smoke;
    private readonly ParticleEmitter fireEmitter;
    private readonly ParticleEmitter smokeEmitter;

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

public class EffectFireball : ParticleEffect
{
    private readonly ParticleSystem fire;
    private readonly ParticleSystem explosion;
    private readonly ProjectileEmitter fireEmitter;

    public IProjectile Projectile => fireEmitter;

    public EffectFireball(GameWorld world, Vector3 position, Vector3 velocity, BaseEntity target)
        : this(world, position, velocity, target, "Fireball", "FireballExplosion") { }

    public EffectFireball(GameWorld world, Vector3 position, Vector3 velocity, BaseEntity target,
                          string fireballParticle, string explosionParticle)
        : base(world)
    {
        fire = ParticleSystem.Create(fireballParticle);
        explosion = ParticleSystem.Create(explosionParticle);
        fireEmitter = new ProjectileEmitter(fire, 150, position, velocity, target);
        fireEmitter.Hit += (sender, e) =>
        {
                // Fill up the particle system
                var n = (int)Helper.RandomInRange(20, 30);
            for (var i = 0; i < n; i++)
            {
                explosion.AddParticle(fireEmitter.Position, fireEmitter.Velocity);
            }

            World.Destroy(this);
        };
    }

    public override void Update(GameTime gameTime)
    {
        fireEmitter.Update(gameTime);
    }
}

public class EffectExplosion : ParticleEffect
{
    private readonly ParticleSystem fire;
    private readonly ParticleSystem smoke;
    private readonly ParticleSystem spark;

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

    private void Trigger()
    {
        for (var i = 0; i < 100; i++)
        {
            fire.AddParticle(Position, Vector3.Zero);
        }

        for (var i = 0; i < 100; i++)
        {
            smoke.AddParticle(Position, Vector3.Zero);
        }

        for (var i = 0; i < 40; i++)
        {
            spark.AddParticle(Position, Vector3.Zero);
        }
    }

    private int counter;

    public override void Update(GameTime gameTime)
    {
        if (counter++ > 80)
        {
            Trigger();
            counter = 0;
        }
    }
}

public class EffectStar : ParticleEffect
{
    private readonly GameObject target;
    private readonly ParticleSystem particle;
    private readonly CircularEmitter emitter;

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
        {
            Position = target.Position;
        }

        emitter.Update(gameTime, Position);
    }
}

public class EffectGlow : ParticleEffect
{
    private readonly GameObject target;
    private readonly ParticleSystem particle;
    private readonly ParticleEmitter emitter;

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

public class EffectPunishOfNature : ParticleEffect
{
    private const float DropSpeed = 50;
    private const float Height = 200;
    public const float Radius = 100;
    private const int MaxRainDrops = 60;
    private readonly ParticleSystem rain;
    private readonly ParticleEmitter[] rainEmitters = new ParticleEmitter[MaxRainDrops];
    private readonly float[] sleepTimes = new float[MaxRainDrops];

    public EffectPunishOfNature(GameWorld world, Vector3 position)
        : base(world)
    {
        Position = position;

        rain = ParticleSystem.Create("PunishOfNature");

        for (var i = 0; i < rainEmitters.Length; i++)
        {
            rainEmitters[i] = new ParticleEmitter(rain, 400, RandomPosition());
            sleepTimes[i] = Helper.RandomInRange(0, 10);
        }
    }

    private Vector3 RandomPosition()
    {
        Vector3 v;

        var angle = Helper.RandomInRange(0, 2 * MathHelper.Pi);
        var radius = Helper.RandomInRange(0, Radius);

        v.X = Position.X + radius * (float)Math.Cos(angle);
        v.Y = Position.Y + radius * (float)Math.Sin(angle);
        v.Z = Position.Z + Height;

        return v;
    }

    public override void Update(GameTime gameTime)
    {
        Vector3 dropAmount = Vector3.Zero;

        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        dropAmount.Z = DropSpeed * elapsedSeconds;

        for (var i = 0; i < rainEmitters.Length; i++)
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
                    rainEmitters[i].PreviousPosition = _ = RandomPosition();
                }
                else
                {
                    rainEmitters[i].Update(gameTime, newPosition, Vector3.Zero, true);
                }
            }
        }
    }
}

public class EffectHalo : ParticleEffect
{
    private float angle;
    public float Speed = 2.0f;
    public float Radius;
    private Vector3 spawn;
    private readonly ParticleEmitter emitter;
    private readonly ParticleSystem particle;

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
        angle += Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        spawn.X = Position.X + (float)(Radius * Math.Cos(angle));
        spawn.Y = Position.Y + (float)(Radius * Math.Sin(angle));

        emitter.Update(gameTime, spawn);
    }
}

public class EffectSpawn : ParticleEffect
{
    private float angle;
    public float Speed = 4.0f;
    public float Radius;
    private Vector3 spawn;
    private const int Count = 5;
    private readonly ParticleEmitter[] emitters;
    private readonly ParticleSystem particle;

    public EffectSpawn(GameWorld world, Vector3 position, float radius, string particleSystem)
        : base(world)
    {
        Radius = radius;
        Position = position;
        position.X += radius;
        spawn = position;
        particle = ParticleSystem.Create(particleSystem);
        emitters = new ParticleEmitter[Count];

        for (var i = 0; i < emitters.Length; i++)
        {
            emitters[i] = new ParticleEmitter(particle, 150, spawn);
        }
    }

    public override void Update(GameTime gameTime)
    {
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        angle += Speed * elapsedSeconds;
        spawn.Z += 18.0f * elapsedSeconds;

        for (var i = 0; i < Count; i++)
        {
            var realAngle = angle + i * MathHelper.Pi * 2 / Count;

            spawn.X = Position.X + (float)(Radius * Math.Cos(realAngle));
            spawn.Y = Position.Y + (float)(Radius * Math.Sin(realAngle));

            emitters[i].Update(gameTime, spawn);
        }

        if (spawn.Z - Position.Z > 15)
        {
            World.Destroy(this);
        }
    }
}
