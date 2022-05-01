// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

[Flags]
public enum MovableFlags
{
    None = 0,
    InContact = 1 << 0,
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

    public MovableFlags State => _state;
    internal MovableFlags _state;

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
}

public sealed class Move : IDisposable
{
    // Difference between 1 and the least value greater than 1 that is representable.
    // Epsilon (1E-45) represents the smallest positive value that is greater than zero
    // which is way too small to be practical.
    private const float Epsilon = 1.19209290e-7F;
    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_new();

    private ArrayBuilder<Contact> _contacts;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        move_delete(_world);
    }

    ~Move()
    {
        move_delete(_world);
    }

    public unsafe void Update(float dt, Span<Movable> movables, PathGrid? grid = null)
    {
        var idt = 1 / dt;

        foreach (ref var m in movables)
        {
            m._state &= ~MovableFlags.InContact;
            m._desiredVelocity = MoveToTarget(dt, ref m);
        }

        foreach (ref readonly var c in GetContacts())
            UpdateContact(movables, c);

        foreach (ref var m in movables)
            m._force = CalculateForce(idt, ref m);

        fixed (void* units = movables)
        {
            move_step(_world, units, movables.Length, Marshal.SizeOf<Movable>(), dt);
        }

        foreach (ref var m in movables)
            UpdateRotation(dt, ref m);
    }

    private unsafe ReadOnlySpan<Contact> GetContacts()
    {
        var contactCount = move_get_contacts(_world, null, 0);
        if (contactCount <= 0)
            return Array.Empty<Contact>();

        _contacts.SetLength((int)contactCount);
        fixed (Contact* contacts = _contacts.AsSpan())
        {
            move_get_contacts(_world, contacts, contactCount);
        }

        return _contacts.AsSpan();
    }

    private Vector2 MoveToTarget(float dt, ref Movable m)
    {
        if (m.Target is null)
            return default;

        var toTarget = m.Target.Value - m.Position;

        // Have we arrived at the target exactly?
        var distanceSq = toTarget.LengthSquared();
        if (distanceSq < m.Speed * m.Speed * dt * dt)
        {
            m.Target = null;
            return default;
        }

        // Should we start decelerating?
        var decelerationDistance = 0.5f * m.Velocity.LengthSquared() / m.Acceleration;
        if (distanceSq > decelerationDistance * decelerationDistance)
            return Vector2.Normalize(toTarget) * m.Speed;

        return default;
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

        a._state |= MovableFlags.InContact;
        b._state |= MovableFlags.InContact;

        if (a.Target != null && b.Target != null)
        {
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
        else if (a.Target != null || b.Target != null)
        {
            if (b.Target != null)
            {
                ref var temp = ref a;
                a = ref b;
                b = ref temp;
            }

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
        else if ((offset >=0 && offset < MathF.PI) ||
                (offset >= -MathF.PI * 2 && offset < -MathF.PI))
            m._rotation += delta;
        else
            m._rotation -= delta;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - b.X * a.Y;
    }


    [StructLayout(LayoutKind.Sequential)]
    struct AABB
    {
        public Vector2 Min;
        public Vector2 Max;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Contact
    {
        public int A;
        public int B;
    }

    [DllImport(LibName)] private static extern IntPtr move_new();
    [DllImport(LibName)] private static extern void move_delete(IntPtr world);
    [DllImport(LibName)] private static unsafe extern void move_step(IntPtr world, void* units, int unitsLength, int unitSizeInBytes, float dt);
    [DllImport(LibName)] private static unsafe extern int move_get_contacts(IntPtr world, Contact* contacts, int contactsLength);
    [DllImport(LibName)] private static unsafe extern int move_query_aabb(IntPtr world, AABB* aabb, int* units, int unitsLength);
    [DllImport(LibName)] private static unsafe extern int move_raycast(IntPtr world, Vector2* a, Vector2* b, int* unit);
}
