// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

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
}

public sealed class Move : IDisposable
{
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

    public unsafe void Update(float dt, Span<Movable> movables, PathGrid? grid = null)
    {
        var idt = 1 / dt;

        foreach (ref var m in movables)
        {
            var desiredVelocity = Vector2.Zero;
            desiredVelocity += MoveToTarget(ref m);
            m._force = CalculateForce(idt, ref m, desiredVelocity);
        }

        fixed (void* units = movables)
        {
            move_step(_world, units, movables.Length, Marshal.SizeOf<Movable>(), dt);
        }

        foreach (ref var m in movables)
        {
            if (m._velocity.LengthSquared() > m.Speed * m.Speed * 0.001f)
                UpdateRotation(dt, ref m);
        }
    }

    private Vector2 MoveToTarget(ref Movable m)
    {
        if (m.Target is null)
            return default;

        var toTarget = m.Target.Value - m.Position;

        // Have we arrived at the target exactly?
        var distanceSq = toTarget.LengthSquared();
        if (distanceSq < m.Speed * m.Speed * 0.001f)
        {
            m.Target = null;
            return default;
        }

        // Should we start decelerating?
        var decelerationDistance = 0.5f * m.Velocity.LengthSquared() / m.Acceleration;
        if (distanceSq > decelerationDistance * decelerationDistance)
        {
            return Vector2.Normalize(toTarget) * m.Speed;
        }

        return default;
    }

    private Vector2 CalculateForce(float idt, ref Movable m, Vector2 desiredVelocity)
    {
        var force = (desiredVelocity - m.Velocity) * idt;
        var accelerationSq = force.LengthSquared();

        // Are we turning or following a straight line?
        var maxAcceleration = m.Acceleration == 0 ? m.Speed : m.Acceleration;
        if (m.Decceleration != 0)
        {
            var v = desiredVelocity.Length() * m.Velocity.Length();
            if (v != 0)
            {
                var lerp = (Vector2.Dot(desiredVelocity, m.Velocity) / v + 1) / 2;
                maxAcceleration = MathHelper.Lerp(m.Decceleration, m.Acceleration, lerp);
            }
        }

        // Cap max acceleration
        if (accelerationSq > maxAcceleration * maxAcceleration)
        {
            return force * maxAcceleration / MathF.Sqrt(accelerationSq);
        }

        return force;
    }

    private static void UpdateRotation(float dt, ref Movable m)
    {
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

    [StructLayout(LayoutKind.Sequential)]
    struct AABB
    {
        public Vector2 Min;
        public Vector2 Max;
    }

    [DllImport(LibName)] private static extern IntPtr move_new();
    [DllImport(LibName)] private static extern void move_delete(IntPtr world);
    [DllImport(LibName)] private static unsafe extern void move_step(IntPtr world, void* units, nint unitsLength, nint unitSizeInBytes, float dt);
    [DllImport(LibName)] private static unsafe extern nint move_query_aabb(IntPtr world, AABB* aabb, nint* units, nint unitsLength);
}
