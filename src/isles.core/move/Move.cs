// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

[Flags]
public enum MovableFlags : int
{
    None = 0,
    Awake = 1,
    Wake = 2,
    HasContact = 4,
    HasTouchingContact = 8,
}

[StructLayout(LayoutKind.Sequential)]
public struct Movable
{
    public float Radius { get; init; }
    public Vector2 Position { get; init; }
    public Vector2 Velocity { get; init; }
    public Vector2 Force { get; set; }
    public MovableFlags Flags { get; set; }
    private IntPtr _body;
}

[StructLayout(LayoutKind.Sequential)]
public struct Obstacle
{
    public float Size { get; init; }
    public Vector2 Position { get; init; }
    private IntPtr _body;
}

public struct Unit
{
    public float Speed { get; set; }
    public float Acceleration { get; set; }
    public float Decceleration { get; set; }
    public float RotationSpeed { get; set; }
    public float Rotation { get; set; }
    public Vector2? Target { get; set; }
    public PathGridFlowField? FlowField { get; internal set; }

    internal Vector2 _contactVelocity;
}

public sealed class Move : IDisposable
{
    private const float MaxInContactSeconds = 5;
    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_new();
    private readonly PathFinder _pathFinder = new();
    private readonly List<Obstacle> _obstacles = new();
    private PathGrid? _grid;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        move_delete(_world);
    }

    ~Move()
    {
        move_delete(_world);
    }

    public unsafe void Update(float dt, Span<Movable> moveUnits, Span<Unit> units, PathGrid grid)
    {
        var idt = 1 / dt;

        // Update units
        for (var i = 0; i < units.Length; i++)
        {
            ref var m = ref moveUnits[i];
            ref var u = ref units[i];

            if (u.Target != null && u.FlowField is null)
            {
                m.Flags |= MovableFlags.Wake;
            }
            else if ((m.Flags & MovableFlags.Awake) == 0)
            {
                u.Target = null;
                u.FlowField = null;
            }

            var desiredVelocity = GetDesiredVelocity(dt, grid, m, ref u);
            m.Force = CalculateForce(idt, m, u, desiredVelocity);
        }

        if (grid != _grid)
        {
            UpdateObstacles(grid);
            _grid = grid;
        }

        fixed (Movable* pMoveUnits = moveUnits)
        fixed (Obstacle* pObstacles = CollectionsMarshal.AsSpan(_obstacles))
            move_step(_world, dt, pMoveUnits, moveUnits.Length, pObstacles, _obstacles.Count);

        for (var i = 0; i < units.Length; i++)
        {
            ref var u = ref units[i];
            u._contactVelocity = default;
            UpdateRotation(dt, moveUnits[i], ref u);
        }

        UpdateFlowField(dt, grid, moveUnits, units);
    }

    private void UpdateObstacles(PathGrid grid)
    {
        _obstacles.Clear();
        for (var i = 0; i < grid.Bits.Length; i++)
        {
            if (grid.Bits[i])
            {
                var y = Math.DivRem(i, grid.Width, out var x);
                _obstacles.Add(new() { Size = grid.Step, Position = new((x + 0.5f) * grid.Step, (y + 0.5f) * grid.Step) });
            }
        }
    }

    private Vector2 GetDesiredVelocity(float dt, PathGrid grid, in Movable m, ref Unit u)
    {
        var targetVector = GetTargetVector(grid, m, ref u);
        var distance = targetVector.TryNormalize();
        if (distance <= u.Speed * dt)
            return default;

        // Should we start decelerating?
        var speed = MathF.Sqrt(distance * u.Acceleration * 2);
        return targetVector * Math.Min(u.Speed, speed);
    }

    private Vector2 GetTargetVector(PathGrid grid, in Movable m, ref Unit u)
    {
        if (u.Target is null)
            return default;

        if (u.FlowField is null || u.Target.Value != u.FlowField.Value.Target)
            u.FlowField = _pathFinder.GetFlowField(grid, m.Radius * 2, u.Target.Value);

        return u.FlowField.Value.GetVector(m.Position);
    }

    private static Vector2 CalculateForce(float idt, in Movable m, in Unit u, in Vector2 desiredVelocity)
    {
        var force = (desiredVelocity - m.Velocity) * idt;
        var accelerationSq = force.LengthSquared();

        // Are we turning or following a straight line?
        var maxAcceleration = u.Acceleration;
        if (u.Decceleration != 0)
        {
            var v = desiredVelocity.Length() * m.Velocity.Length();
            if (v != 0)
            {
                var lerp = (Vector2.Dot(desiredVelocity, m.Velocity) / v + 1) / 2;
                maxAcceleration = MathHelper.Lerp(u.Decceleration, u.Acceleration, lerp);
            }
        }

        // Cap max acceleration
        if (accelerationSq > maxAcceleration * maxAcceleration)
            return force * maxAcceleration / MathF.Sqrt(accelerationSq);

        return force;
    }

    private void UpdateFlowField(float dt, PathGrid grid, Span<Movable> movables, Span<Unit> units)
    {
        
    }

    private static void UpdateRotation(float dt, in Movable m, ref Unit u)
    {
        if (m.Velocity.LengthSquared() <= u.Speed * u.Speed * dt * dt)
            return;

        var targetRotation = MathF.Atan2(m.Velocity.Y, m.Velocity.X);
        var offset = MathFHelper.NormalizeRotation(targetRotation - u.Rotation);
        var delta = u.RotationSpeed * dt;
        if (Math.Abs(offset) <= delta)
            u.Rotation = targetRotation;
        else if (offset > 0)
            u.Rotation += delta;
        else
            u.Rotation -= delta;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NativeContact
    {
        public int a;
        public int b;
    }

    [DllImport(LibName)] private static extern IntPtr move_new();
    [DllImport(LibName)] private static extern void move_delete(IntPtr world);
    [DllImport(LibName)] private static unsafe extern void move_step(IntPtr world, float dt, Movable* units, int unitsLength, Obstacle* obstacles, int obstaclesLength);
    [DllImport(LibName)] private static extern int move_get_next_contact(IntPtr world, ref IntPtr iterator, out NativeContact contact);
}
