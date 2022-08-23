// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

[Flags]
public enum MovableFlags : int
{
    None = 0,
    Awake = 1,
    Wake = 2,
}

public struct Movable
{
    public float Radius { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2 Force { get; set; }
    public MovableFlags Flags { get; set; }

    public float Speed { get; set; }
    public float Acceleration { get; set; }
    public float Decceleration { get; set; }
    public float RotationSpeed { get; set; }
    public float Rotation { get; set; }
    public Vector2? Target { get; set; }
    public PathGridFlowField? FlowField { get; internal set; }

    internal Vector2 _contactVelocity;
}

public struct Obstacle
{
    public float Size { get; init; }
    public Vector2 Position { get; init; }
    private IntPtr _body;
}

public sealed class Move
{
    private readonly MoveContactSolver _contactSolver = new();
    private readonly PathFinder _pathFinder = new();
    private readonly List<Obstacle> _obstacles = new();
    private PathGrid? _lastGrid;

    public unsafe void Update(float dt, Span<Movable> movables, PathGrid grid)
    {
        var idt = 1 / dt;

        for (var i = 0; i < movables.Length; i++)
        {
            ref var m = ref movables[i];

            if (m.Target != null && m.FlowField is null)
            {
                m.Flags |= MovableFlags.Wake;
            }
            else if ((m.Flags & MovableFlags.Awake) == 0)
            {
                m.Target = null;
                m.FlowField = null;
            }

            var desiredVelocity = GetDesiredVelocity(dt, grid, ref m);
            m.Force = CalculateForce(idt, m, desiredVelocity + m._contactVelocity);
        }

        if (grid != _lastGrid)
        {
            UpdateObstacles(grid);
            _lastGrid = grid;
        }

        _contactSolver.Step(dt, movables);

        for (var i = 0; i < movables.Length; i++)
        {
            ref var m = ref movables[i];
            m._contactVelocity = default;
            UpdateRotation(dt, ref m);
        }
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

    private Vector2 GetDesiredVelocity(float dt, PathGrid grid, ref Movable m)
    {
        var targetVector = GetTargetVector(grid, ref m);
        var distance = targetVector.TryNormalize();
        if (distance <= m.Speed * dt)
            return default;

        // Should we start decelerating?
        var speed = MathF.Sqrt(distance * m.Acceleration * 2);
        return targetVector * Math.Min(m.Speed, speed);
    }

    private Vector2 GetTargetVector(PathGrid grid, ref Movable m)
    {
        if (m.Target is null)
            return default;

        if (m.FlowField is null || m.Target.Value != m.FlowField.Target)
            m.FlowField = _pathFinder.GetFlowField(grid, m.Radius * 2, m.Target.Value);

        return m.FlowField.GetVector(m.Position);
    }

    private static Vector2 CalculateForce(float idt, in Movable m, in Vector2 desiredVelocity)
    {
        var force = (desiredVelocity - m.Velocity) * idt;
        var accelerationSq = force.LengthSquared();

        // Are we turning or following a straight line?
        var maxAcceleration = m.Acceleration;
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
            return force * maxAcceleration / MathF.Sqrt(accelerationSq);

        return force;
    }

    private static void UpdateRotation(float dt, ref Movable m)
    {
        if (m.Velocity.LengthSquared() <= m.Speed * m.Speed * dt * dt)
            return;

        var targetRotation = MathF.Atan2(m.Velocity.Y, m.Velocity.X);
        var offset = MathFHelper.NormalizeRotation(targetRotation - m.Rotation);
        var delta = m.RotationSpeed * dt;
        if (Math.Abs(offset) <= delta)
            m.Rotation = targetRotation;
        else if (offset > 0)
            m.Rotation += delta;
        else
            m.Rotation -= delta;
    }
}
