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
            var rotation = MathF.Atan2(m._velocity.Y, m._velocity.X);
            if (!float.IsNaN(rotation))
                m._rotation = rotation;
        }
    }

    private Vector2 CalculateForce(float idt, ref Movable m)
    {
        var v = Vector2.Zero;

        if (m.Target != null)
        {
            var s = m.Target.Value - m.Position;
            var ss = s.LengthSquared();
            if (ss < m.Speed * m.Speed * 0.001f)
            {
                m.Target = null;
                return default;
            }
            v = Vector2.Normalize(s) * m.Speed;
        }

        var a = (v - m.Velocity) * idt;
        var aa = a.LengthSquared();
        if (aa > m.Acceleration * m.Acceleration)
        {
            return a * m.Acceleration / MathF.Sqrt(aa);
        }
        return a;
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
