// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Isles;

[Flags]
public enum UnitFlags
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
    public float Radius { get; init; }
    public Vector2 Position { get; init; }
    public Vector2 Velocity { get; init; }
    public Vector2 Force { get; set; }
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
    public UnitFlags Flags => _flags;
    internal UnitFlags _flags;

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

    internal PathGridFlowField? _flowField;
    internal Vector2 _contactVelocity;
    internal float _inContactSeconds;
}

public sealed class Move : IDisposable
{
    private const float MaxInContactSeconds = 5;
    private const string LibName = "isles.native";

    private readonly IntPtr _world = move_new();
    private readonly PathFinder _pathFinder = new();
    private readonly List<Obstacle> _obstacles = new();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        move_delete(_world);
    }

    ~Move()
    {
        move_delete(_world);
    }

    public unsafe void Update(float dt, Span<Movable> moveUnits, Span<Unit> units, PathGrid? grid = null)
    {
        var idt = 1 / dt;

        // Update units
        for (var i = 0; i < units.Length; i++)
        {
            ref var m = ref moveUnits[i];
            ref var u = ref units[i];

            var desiredVelocity = u._contactVelocity;
            UpdateInContactSeconds(dt, ref u);
            desiredVelocity += MoveToTarget(dt, grid, m, ref u);
            m.Force = CalculateForce(idt, m, u, desiredVelocity);
        }

        if (grid != null && _obstacles.Count == 0)
            UpdateObstacles(grid);

        fixed (Movable* pMoveUnits = moveUnits)
        fixed (Obstacle* pObstacles = CollectionsMarshal.AsSpan(_obstacles))
            move_step(_world, dt, pMoveUnits, moveUnits.Length, pObstacles, _obstacles.Count);

        for (var i = 0; i < units.Length; i++)
        {
            ref var u = ref units[i];
            u._flags = 0;
            u._contactVelocity = default;
            UpdateRotation(dt, moveUnits[i], ref u);
        }

        IntPtr contactItr = default;
        while (move_get_next_contact(_world, ref contactItr, out var c) != 0)
            UpdateContact(moveUnits, units, c);
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

    private Vector2 MoveToTarget(float dt, PathGrid? grid, in Movable m, ref Unit u)
    {
        if (u.Target is null)
            return default;

        // Have we arrived at the target exactly?
        var toTarget = u.Target.Value - m.Position;
        var distanceSq = toTarget.LengthSquared();
        if (distanceSq <= u.Speed * dt * u.Speed * dt)
        {
            ClearTarget(ref u);
            return default;
        }

        // Have we reached the target and there is an non-idle unit along the way?
        if ((u._flags & UnitFlags.HasMovingContact) != 0 && distanceSq <= m.Radius * m.Radius)
        {
            ClearTarget(ref u);
            return default;
        }

        // Should we start decelerating?
        var distance = MathF.Sqrt(distanceSq);
        var speed = MathF.Sqrt(distance * u.Acceleration * 2);

        var flowFieldDirection = FollowFlowField(grid, m, ref u);
        var direction = flowFieldDirection == default ? toTarget : flowFieldDirection;

        return Vector2.Normalize(direction) * Math.Min(u.Speed, speed);
    }

    private Vector2 FollowFlowField(PathGrid? grid, in Movable m, ref Unit u)
    {
        if (grid is null || u.Target is null)
            return default;

        if (u._flowField is null || u.Target.Value != u._flowField.Value.Target)
            u._flowField = _pathFinder.GetFlowField(grid, m.Radius * 2, u.Target.Value);

        return u._flowField.Value.GetDirection(m.Position);
    }

    private Vector2 CalculateForce(float idt, in Movable m, in Unit u, in Vector2 desiredVelocity)
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

    private void UpdateContact(Span<Movable> moveUnits, Span<Unit> units, in NativeContact c)
    {
        ref var ma = ref moveUnits[c.a];
        ref var mb = ref moveUnits[c.b];
        ref var ua = ref units[c.a];
        ref var ub = ref units[c.b];

        ua._flags |= UnitFlags.HasContact;
        ub._flags |= UnitFlags.HasContact;

        if (ua.Target != null && ub.Target != null)
            UpdateContactBothBuzy(ma, ref ua, mb, ref ub);
        else if (ua.Target != null)
            UpdateContactOneBuzyOneIdle(ma, ref ua, mb, ref ub);
        else if (ub.Target != null)
            UpdateContactOneBuzyOneIdle(mb, ref ub, ma, ref ua);
    }

    private void UpdateInContactSeconds(float dt, ref Unit u)
    {
        if (u.Target is null)
            return;

        if ((u._flags & UnitFlags.HasContact) == 0)
        {
            u._inContactSeconds = Math.Max(0, u._inContactSeconds - dt);
            return;
        }

        // Give up target if we have keeps bumping into other units for enough time
        if ((u._inContactSeconds += dt) >= 20)
            ClearTarget(ref u);
    }

    private void UpdateContactBothBuzy(in Movable ma, ref Unit ua, in Movable mb, ref Unit ub)
    {
        ua._flags |= UnitFlags.HasMovingContact;
        ub._flags |= UnitFlags.HasMovingContact;

        var velocity = mb.Velocity - ma.Velocity;
        var normal = mb.Position - ma.Position;
        if (!normal.TryNormalize())
            return;

        var perpendicular = Cross(velocity, normal) > 0
            ? new Vector2(normal.Y, -normal.X)
            : new Vector2(-normal.Y, normal.X);

        if (Vector2.Dot(ma.Velocity, mb.Velocity) < 0)
        {
            // Try circle around each other on meeting
            ua._contactVelocity -= perpendicular * ua.Speed;
            ub._contactVelocity += perpendicular * ub.Speed;
        }
        else if (ua.Speed > ub.Speed && Vector2.Dot(ma.Velocity, normal) > 0)
        {
            // Try surpass when A chase B
            ua._contactVelocity += perpendicular * ua.Speed;
        }
        else if (ub.Speed > ua.Speed && Vector2.Dot(mb.Velocity, normal) < 0)
        {
            // Try surpass when B chase A
            ub._contactVelocity += perpendicular * ub.Speed;
        }
    }

    private void UpdateContactOneBuzyOneIdle(in Movable ma, ref Unit ua, in Movable mb, ref Unit ub)
    {
        ub._flags |= UnitFlags.HasMovingContact;

        var velocity = ma.Velocity;
        var normal = mb.Position - ma.Position;

        // Are we occupying the target?
        var direction = mb.Position - ua.Target!.Value;
        if (direction.LengthSquared() > (ma.Radius + mb.Radius) * (ma.Radius + mb.Radius))
        {
            // Choose a perpendicular direction to give way to the moving unit.
            direction = Cross(velocity, normal) > 0
                ? new Vector2(-ma.Velocity.Y, ma.Velocity.X)
                : new Vector2(ma.Velocity.Y, -ma.Velocity.X);
        }

        if (!direction.TryNormalize())
            return;

        ub._contactVelocity += direction * ub.Speed;
    }

    private static void UpdateRotation(float dt, in Movable m, ref Unit u)
    {
        if (m.Velocity.LengthSquared() <= u.Speed * u.Speed * dt * dt)
            return;

        var targetRotation = MathF.Atan2(m.Velocity.Y, m.Velocity.X);
        var offset = MathFHelper.NormalizeRotation(targetRotation - u._rotation);
        var delta = u.RotationSpeed * dt;
        if (Math.Abs(offset) <= delta)
            u._rotation = targetRotation;
        else if (offset > 0)
            u._rotation += delta;
        else
            u._rotation -= delta;
    }

    private static void ClearTarget(ref Unit u)
    {
        u.Target = null;
        u._flowField = default;
        u._inContactSeconds = 0;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - b.X * a.Y;
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
