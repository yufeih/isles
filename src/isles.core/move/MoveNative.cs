// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

public sealed class MoveNative : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MoveUnit
    {
        public float radius;
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 force;
    }

    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_new();
    private ArrayBuilder<MoveUnit> _units;

    public unsafe void Update(float dt, Span<Movable> movables, PathGrid? grid = null)
    {
        var idt = 1 / dt;

        _units.Clear();
        _units.EnsureCapacity(movables.Length);

        for (var i = 0; i < movables.Length; i++)
        {
            ref var m = ref movables[i];
            var force = UpdateUnit(idt, ref m);
            _units.Add(new() { radius = m.Radius, position = m.Position, force = force });
        }

        fixed (MoveUnit* units = _units.AsSpan())
        {
            move_step(_world, units, _units.Length, dt);
        }

        for (var i = 0; i < _units.Length; i++)
        {
            ref readonly var u = ref _units[i];
            ref var m = ref movables[i];
            m._position = u.position;
            m._velocity = u.velocity;
            var rotation = MathF.Atan2(u.velocity.Y, u.velocity.X);
            if (!float.IsNaN(rotation))
                m._rotation = rotation;
        }
    }

    private Vector2 UpdateUnit(float idt, ref Movable m)
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

    ~MoveNative()
    {
        move_delete(_world);
    }

    [DllImport(LibName)] public static extern IntPtr move_new();
    [DllImport(LibName)] public static extern void move_delete(IntPtr world);
    [DllImport(LibName)] public static unsafe extern void move_step(IntPtr world, MoveUnit* units, int unitsLength, float timeStep);
}
