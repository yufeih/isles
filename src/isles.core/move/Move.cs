// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

[Flags]
public enum MovableFlags
{
    None = 0,

    /// <summary>
    /// Wether this unit has any touching contact with other units.
    /// </summary>
    HasContact = 1 << 0,

    /// <summary>
    /// Wether this unit has any touching contact with other moving unit.
    /// </summary>
    HasMovingContact = 1 << 1,
}

[StructLayout(LayoutKind.Sequential)]
public struct Movable
{
#region Interop
    internal float _radius;
    internal Vector2 _position;
    internal Vector2 _velocity;
    internal Vector2 _force;
#endregion

    public float Radius
    {
        get => _radius;
        init => _radius = value;
    }

    public Vector2 Position
    {
        get => _position;
        init => _position = value;
    }

    public Vector2 Velocity => _velocity;

    public MovableFlags Flags => _flags;
    internal MovableFlags _flags;

    public float Speed { get; set; }
    public float Acceleration { get; set; }
    public float Decceleration { get; set; }
    public float RotationSpeed { get; set; }

    public float Rotation
    {
        get => _rotation;
        init => _rotation = value;
    }
    internal float _rotation;

    public Vector2? Target { get; set; }

    internal Vector2 _desiredVelocity;

    internal float _inContactSeconds;
}

public struct MoveObstacle
{
    public Vector2[] Vertices { get; init; }
}

public sealed class Move : IDisposable
{
    // Difference between 1 and the least value greater than 1 that is representable.
    // Epsilon (1E-45) represents the smallest positive value that is greater than zero
    // which is way too small to be practical.
    private const float Epsilon = 1.19209290e-7F;
    private const float MaxInContactSeconds = 5;
    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_new();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        move_delete(_world);
    }

    ~Move()
    {
        move_delete(_world);
    }

    public unsafe void SetObstacles(ReadOnlySpan<MoveObstacle> obstacles)
    {
        foreach (ref readonly var o in obstacles)
        {
            fixed (Vector2* vertices = o.Vertices)
            {
                move_add_obstacle(_world, vertices, o.Vertices.Length);
            }
        }
    }

    public unsafe void Update(float dt, Span<Movable> units, PathGrid? grid = null)
    {
        var idt = 1 / dt;

        foreach (ref var unit in units)
        {
            unit._flags = 0;
            unit._desiredVelocity = default;
        }

        void* contactItr;
        while (move_get_next_contact(_world, &contactItr, out var c) != 0)
        {
            UpdateContact(units, c);
        }

        foreach (ref var unit in units)
        {
            UpdateInContactSeconds(dt, ref unit);
            unit._desiredVelocity += MoveToTarget(dt, ref unit);
            unit._force = CalculateForce(idt, ref unit);
        }

        fixed (void* pUnits = units)
        {
            move_step(_world, dt, pUnits, units.Length, Marshal.SizeOf<Movable>());
        }

        foreach (ref var unit in units)
        {
            UpdateRotation(dt, ref unit);
        }
    }

    private Vector2 MoveToTarget(float dt, ref Movable m)
    {
        if (m.Target is null)
            return default;

        // Have we arrived at the target exactly?
        var toTarget = m.Target.Value - m.Position;
        var distanceSq = toTarget.LengthSquared();
        if (distanceSq <= m.Speed * dt * m.Speed * dt)
        {
            ClearTarget(ref m);
            return default;
        }

        // Have we reached the target and there is an non-idle unit along the way?
        if ((m.Flags & MovableFlags.HasMovingContact) != 0 && distanceSq <= m.Radius * m.Radius)
        {
            ClearTarget(ref m);
            return default;
        }

        // Should we start decelerating?
        var distance = MathF.Sqrt(distanceSq);
        var speed = MathF.Sqrt(distance * m.Acceleration * 2);

        return Vector2.Normalize(toTarget) * Math.Min(m.Speed, speed);
    }

    private Vector2 CalculateForce(float idt, ref Movable m)
    {
        var force = (m._desiredVelocity - m.Velocity) * idt;
        var accelerationSq = force.LengthSquared();

        // Are we turning or following a straight line?
        var maxAcceleration = m.Acceleration;
        if (m.Decceleration != 0)
        {
            var v = m._desiredVelocity.Length() * m.Velocity.Length();
            if (v != 0)
            {
                var lerp = (Vector2.Dot(m._desiredVelocity, m.Velocity) / v + 1) / 2;
                maxAcceleration = MathHelper.Lerp(m.Decceleration, m.Acceleration, lerp);
            }
        }

        // Cap max acceleration
        if (accelerationSq > maxAcceleration * maxAcceleration)
            return force * maxAcceleration / MathF.Sqrt(accelerationSq);

        return force;
    }

    private void UpdateContact(Span<Movable> movables, in Contact c)
    {
        ref var a = ref movables[c.A];
        ref var b = ref movables[c.B];

        a._flags |= MovableFlags.HasContact;
        b._flags |= MovableFlags.HasContact;

        if (a.Target != null && b.Target != null)
            UpdateContactBothBuzy(ref a, ref b);
        else if (a.Target != null)
            UpdateContactOneBuzyOneIdle(ref a, ref b);
        else if (b.Target != null)
            UpdateContactOneBuzyOneIdle(ref b, ref a);
    }

    private void UpdateInContactSeconds(float dt, ref Movable m)
    {
        if (m.Target is null)
            return;

        if ((m.Flags & MovableFlags.HasContact) == 0)
        {
            m._inContactSeconds = Math.Max(0, m._inContactSeconds - dt);
            return;
        }

        // Give up target if we have keeps bumping into other units for enough time
        if ((m._inContactSeconds += dt) >= MaxInContactSeconds)
        {
            ClearTarget(ref m);
        }
    }

    private void UpdateContactBothBuzy(ref Movable a, ref Movable b)
    {
        a._flags |= MovableFlags.HasMovingContact;
        b._flags |= MovableFlags.HasMovingContact;

        var velocity = b.Velocity - a.Velocity;
        var normal = b.Position - a.Position;

        if (normal.LengthSquared() <= Epsilon * Epsilon)
            return;

        normal.Normalize();

        // Try circle around each other
        var perpendicular = Cross(velocity, normal) > 0
            ? new Vector2(normal.Y, -normal.X)
            : new Vector2(-normal.Y, normal.X);

        a._desiredVelocity -= perpendicular * a.Speed;
        b._desiredVelocity += perpendicular * b.Speed;
    }

    private void UpdateContactOneBuzyOneIdle(ref Movable a, ref Movable b)
    {
        b._flags |= MovableFlags.HasMovingContact;

        var velocity = a.Velocity;
        var normal = b.Position - a.Position;

        // Are we occupying the target?
        var direction = b.Position - a.Target!.Value;
        if (direction.LengthSquared() > (a.Radius + b.Radius) * (a.Radius + b.Radius))
        {
            // Choose a perpendicular direction to give way to the moving unit.
            direction = Cross(velocity, normal) > 0
                ? new Vector2(-a.Velocity.Y, a.Velocity.X)
                : new Vector2(a.Velocity.Y, -a.Velocity.X);
        }

        if (direction.LengthSquared() <= Epsilon * Epsilon)
            return;

        b._desiredVelocity += Vector2.Normalize(direction) * b.Speed;
    }

    private static void UpdateRotation(float dt, ref Movable m)
    {
        if (m._velocity.LengthSquared() <= m.Speed * m.Speed * dt * dt)
            return;

        while (m._rotation > MathHelper.Pi)
            m._rotation -= MathF.PI + MathF.PI;
        while (m._rotation < -MathHelper.Pi)
            m._rotation += MathF.PI + MathF.PI;

        var targetRotation = MathF.Atan2(m._velocity.Y, m._velocity.X);
        var offset = targetRotation - m._rotation;
        var delta = m.RotationSpeed * dt;

        if (Math.Abs(offset) <= delta)
            m._rotation = targetRotation;
        else if ((offset >=0 && offset < MathF.PI) || (offset >= -MathF.PI * 2 && offset < -MathF.PI))
            m._rotation += delta;
        else
            m._rotation -= delta;
    }

    private static void ClearTarget(ref Movable m)
    {
        m.Target = null;
        m._inContactSeconds = 0;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - b.X * a.Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Contact
    {
        public int A;
        public int B;
    }

    [DllImport(LibName)] private static extern IntPtr move_new();
    [DllImport(LibName)] private static extern void move_delete(IntPtr world);
    [DllImport(LibName)] private static unsafe extern void move_step(IntPtr world, float dt, void* units, int length, int sizeInBytes);
    [DllImport(LibName)] private static unsafe extern void move_add_obstacle(IntPtr world, Vector2* vertices, int length);
    [DllImport(LibName)] private static unsafe extern int move_get_next_contact(IntPtr world, void** iterator, out Contact contact);
}
