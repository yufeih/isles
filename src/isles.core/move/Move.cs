// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public float Mass { get; set; }
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
    public int IslandId { get; internal set; }
    public PathGridFlowField? FlowField { get; internal set; }

    internal Vector2 _contactDirection;
    internal float _contactBlendWeight;
}

public sealed class Move : IDisposable
{
    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_new();
    private readonly MoveIsland _island = new();
    private readonly Dictionary<int, int> _flowFieldIslandIds = new();
    private readonly Dictionary<(Vector2, int), PathGridFlowField> _flowFields = new();
    private readonly List<Obstacle> _obstacles = new();
    private PathGrid? _lastGrid;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        move_delete(_world);
    }

    ~Move()
    {
        move_delete(_world);
    }

    public unsafe void Update(float dt, Span<Movable> movables, Span<Unit> units, PathGrid grid)
    {
        var idt = 1 / dt;

        UpdateFlowFields(movables, units, grid);

        // Update units
        for (var i = 0; i < units.Length; i++)
        {
            ref var m = ref movables[i];
            ref var u = ref units[i];
            var desiredVelocity = GetDesiredVelocity(dt, grid, m, ref u);
            if (u._contactDirection != default)
                //desiredVelocity = Vector2.Lerp(desiredVelocity, u._contactDirection * u.Speed, 0.5f);
                desiredVelocity = u._contactDirection * u.Speed;
            m.Mass = u.Target is null ? 0.1f : 1;
            m.Force = CalculateAcceleration(idt, m, u, desiredVelocity) * m.Mass;
        }

        if (grid != _lastGrid)
        {
            UpdateObstacles(grid);
            _lastGrid = grid;
        }

        fixed (Movable* pMovables = movables)
        fixed (Obstacle* pObstacles = CollectionsMarshal.AsSpan(_obstacles))
            move_step(_world, dt, pMovables, movables.Length, pObstacles, _obstacles.Count);

        for (var i = 0; i < units.Length; i++)
        {
            ref var u = ref units[i];
            u._contactDirection = default;
            u._contactBlendWeight = 0;
            u.IslandId = 0;
            UpdateRotation(dt, movables[i], ref u);
        }

        var contactLength = move_get_contact_count(_world);
        var contacts = ArrayPool<(int, int)>.Shared.Rent(contactLength);
        fixed ((int, int)* pContacts = contacts)
            contactLength = move_copy_contacts(_world, pContacts, contacts.Length);
        if (contactLength > 0)
            _island.Solve(contacts.AsSpan(0, contactLength), movables, units);
        ArrayPool<(int, int)>.Shared.Return(contacts);
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

    private void UpdateFlowFields(Span<Movable> movables, Span<Unit> units, PathGrid grid)
    {
        foreach (var (key, value) in _flowFields)
        {
            value.TargetIslandId = 0;

            for (var i = 0; i < units.Length; i++)
            {
                ref var m = ref movables[i];
                ref var u = ref units[i];

                if ((m.Position - value.Target).LengthSquared() < m.Radius * m.Radius)
                    value.TargetIslandId = u.IslandId;
            }
        }

        for (var i = 0; i < units.Length; i++)
        {
            ref var m = ref movables[i];
            ref var u = ref units[i];

            if (u.Target is null)
                continue;

            if (u.FlowField is null || u.FlowField.Target != u.Target)
            {
                m.Flags |= MovableFlags.Wake;
                u.FlowField = GetFlowField(grid, m.Radius * 2, u.Target.Value);
            }
            else
            {
                if (u.IslandId != 0)
                {
                    if (u.FlowField.TargetIslandId == u.IslandId && m.Velocity.LengthSquared() < u.Speed * u.Speed * 0.01f)
                    {
                        u.Target = null;
                        u.FlowField = null;
                    }
                }

                if ((m.Flags & MovableFlags.Awake) == 0)
                {
                    u.Target = null;
                    u.FlowField = null;
                }
            }
        }
    }

    private PathGridFlowField GetFlowField(PathGrid grid, float pathWidth, Vector2 target)
    {
        var size = (int)MathF.Ceiling(pathWidth / grid.Step);
        if (!_flowFields.TryGetValue((target, size), out var flowField))
            flowField = _flowFields[(target, size)] = new(
                target, grid, FlowField.Create(new PathGridGraph(grid, size), target));
        return flowField;
    }

    private Vector2 GetDesiredVelocity(float dt, PathGrid grid, in Movable m, ref Unit u)
    {
        if (u.FlowField is null || u.Target is null)
            return default;

        var targetVector = u.FlowField.GetVector(m.Position);
        var distance = targetVector.TryNormalize();
        if (distance <= u.Speed * dt)
            return default;

        // Should we start decelerating?
        var speed = MathF.Sqrt(distance * u.Acceleration * 2);
        return targetVector * Math.Min(u.Speed, speed);
    }

    private static Vector2 CalculateAcceleration(float idt, in Movable m, in Unit u, in Vector2 desiredVelocity)
    {
        var acceleration = (desiredVelocity - m.Velocity) * idt;
        var accelerationSq = acceleration.LengthSquared();

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
            return acceleration * maxAcceleration / MathF.Sqrt(accelerationSq);

        return acceleration;
    }

    private static void UpdateRotation(float dt, in Movable m, ref Unit u)
    {
        if (m.Velocity.LengthSquared() <= u.Speed * u.Speed * 0.01f)
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

    [DllImport(LibName)] private static extern IntPtr move_new();
    [DllImport(LibName)] private static extern void move_delete(IntPtr world);
    [DllImport(LibName)] private static unsafe extern void move_step(IntPtr world, float dt, Movable* units, int unitsLength, Obstacle* obstacles, int obstaclesLength);
    [DllImport(LibName)] private static extern int move_get_contact_count(IntPtr world);
    [DllImport(LibName)] private static unsafe extern int move_copy_contacts(IntPtr world, (int, int)* contacts, int length);
}
