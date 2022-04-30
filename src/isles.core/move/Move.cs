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

    public unsafe void Update(float dt, Span<Movable> movables, PathGrid? grid = null)
    {
        var idt = 1 / dt;

        foreach (ref var m in movables)
        {
            m._force = CalculateForce(idt, ref m);
        }

        fixed (void* units = movables)
        {
            move_step(_world, units, movables.Length, Marshal.SizeOf<Movable>(), dt);
        }

        foreach (ref var m in movables)
        {
            if (m._velocity.LengthSquared() > m.Speed * m.Speed * 0.001f)
            {
                UpdateRotation(dt, ref m);
            }
        }
    }

    private Vector2 CalculateForce(float idt, ref Movable m)
    {
        var desiredVelocity = Vector2.Zero;

        if (m.Target != null)
        {
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
                desiredVelocity = Vector2.Normalize(toTarget) * m.Speed;
            }
        }

        // Calculate force and cap acceleration
        var force = (desiredVelocity - m.Velocity) * idt;
        var accelerationSq = force.LengthSquared();
        if (accelerationSq > m.Acceleration * m.Acceleration)
        {
            return force * m.Acceleration / MathF.Sqrt(accelerationSq);
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        move_delete(_world);
    }

    ~Move()
    {
        move_delete(_world);
    }

    [DllImport(LibName)] public static extern IntPtr move_new();
    [DllImport(LibName)] public static extern void move_delete(IntPtr world);
    [DllImport(LibName)] public static unsafe extern void move_step(IntPtr world, void* units, nint unitLength, nint unitSizeInBytes, float dt);
}
