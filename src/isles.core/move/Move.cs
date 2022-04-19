// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public struct Movable
{
    public float Radius { get; init; }
    public float Speed { get; set; }
    public float RotationSpeed { get; set; }

    public Vector2 Position
    {
        get => _position;
        init => _position = value;
    }
    internal Vector2 _position;

    public Vector2 Velocity => _velocity;
    internal Vector2 _velocity;

    public float Rotation
    {
        get => _rotation;
        init => _rotation = value;
    }
    internal float _rotation;

    public Vector2? Target { get; set; }

    internal ArrayBuilder<Vector2> _path;
    internal int _pathIndex;
}

public class Move
{
    private readonly PathFinder _pathFinder = new();

    private ArrayBuilder<Contact> _contacts;
    private ArrayBuilder<EdgeContact> _edgeContacts;

    public void Update(float dt, Span<Movable> movables, PathGrid? grid = null)
    {
        UpdateTarget(dt, movables, grid);

        var idt = 1.0f / dt;
        var contacts = FindContacts(movables);
        var edgeContacts = grid != null ? FindGridContacts(grid, movables) : Array.Empty<EdgeContact>();

        UpdateContacts(idt, movables, contacts);
        UpdateEdgeContacts(idt, movables, edgeContacts);
        UpdatePositions(dt, movables);
    }

    private void UpdateTarget(float dt, Span<Movable> movables, PathGrid? grid)
    {
        foreach (ref var m in movables)
        {
            if (m.Target is null)
            {
                m._velocity = default;
                continue;
            }

            var target = m.Target.Value;

            // Find path if there is no path to follow
            if (grid != null)
            {
                if (m._path.Length == 0 || m._path[^1] != m.Target.Value)
                {
                    m._path.Clear();
                    m._pathIndex = 0;
                    var path = _pathFinder.FindPath(grid, m.Radius * 2, m.Position, target);
                    if (path.Length == 0)
                    {
                        m._path.Add(target);
                    }
                    else
                    {
                        m._path.AddRange(path);
                    }
                }

                target = m._path[m._pathIndex];
            }

            var offset = target - m._position;
            var distanceSquared = offset.LengthSquared();

            // Follow next waypoint
            if (m._pathIndex < m._path.Length - 1 && distanceSquared <= m.Radius * m.Radius)
            {
                target = m._path[m._pathIndex++];
                offset = target - m._position;
                distanceSquared = offset.LengthSquared();
            }

            // Update velocity
            var distanceEpsilon = m.Speed * dt * 2;
            if (distanceSquared <= distanceEpsilon * distanceEpsilon)
            {
                m.Target = null;
                m._velocity = default;
                m._path.Clear();
                m._pathIndex = 0;
            }
            else
            {
                m._velocity = Vector2.Normalize(offset) * m.Speed;
            }
        }
    }

    private static void UpdatePositions(float dt, Span<Movable> movables)
    {
        foreach (ref var m in movables)
        {
            if (m._velocity.LengthSquared() < m.Speed * m.Speed * 0.001f)
            {
                m._velocity = default;
            }
            else
            {
                m._position += m._velocity * dt;
                UpdateRotation(ref m, dt);
            }
        }
    }

    private static void UpdateRotation(ref Movable m, float dt)
    {
        while (m._rotation > MathHelper.Pi)
        {
            m._rotation -= MathF.PI + MathF.PI;
        }
        while (m._rotation < -MathHelper.Pi)
        {
            m._rotation += MathF.PI + MathF.PI;
        }

        var targetRotation = MathF.Atan2(m._velocity.Y, m._velocity.X);
        var rotationOffset = targetRotation - m._rotation;
        var rotationDelta = m.RotationSpeed * dt;

        if (Math.Abs(rotationOffset) <= rotationDelta)
        {
            m._rotation = targetRotation;
        }
        else
        {
            if ((rotationOffset >=0 && rotationOffset < MathF.PI) ||
                (rotationOffset >= -MathF.PI * 2 && rotationOffset < -MathF.PI))
            {
                m._rotation += rotationDelta;
            }
            else
            {
                m._rotation -= rotationDelta;
            }
        }
    }

    private static void UpdateContacts(float idt, Span<Movable> movables, ReadOnlySpan<Contact> contacts)
    {
        foreach (ref readonly var c in contacts)
        {
            ref var a = ref movables[c.A];
            ref var b = ref movables[c.B];

            var impulse = c.Normal * c.Penetration * idt;

            if (a.Target is null && b.Target is null)
            {
                a._velocity -= impulse * 0.5f;
                b._velocity += impulse * 0.5f;
            }
            else if (a.Target != null && b.Target != null)
            {
                var target = b.Target.Value - a.Target.Value;
                var radius = a.Radius + b.Radius;

                if (target.LengthSquared() <= radius * radius && (
                    (a.Position - a.Target.Value).LengthSquared() <= a.Radius * a.Radius ||
                    (b.Position - b.Target.Value).LengthSquared() <= b.Radius * b.Radius))
                {
                    a.Target = b.Target = null;
                }
                else
                {
                    var velocity = b._velocity - a._velocity;
                    var perpendicular = Cross(velocity, c.Normal) > 0
                        ? new Vector2(c.Normal.Y, -c.Normal.X)
                        : new Vector2(-c.Normal.Y, c.Normal.X);

                    var speed = a.Speed + b.Speed;
                    var targetVelocity = perpendicular * speed;
                    var p = targetVelocity - velocity + impulse;

                    a._velocity -= p * a.Speed / speed;
                    b._velocity += p * b.Speed / speed;
                }
            }
            else
            {
                var normal = c.Normal;

                if (b.Target != null)
                {
                    ref var temp = ref a;
                    a = ref b;
                    b = ref temp;
                    impulse = -impulse;
                    normal = -normal;
                }

                var direction = b.Position - a.Target!.Value;
                if (direction.LengthSquared() > (a.Radius + b.Radius) * (a.Radius + b.Radius))
                {
                    var velocity = b._velocity - a._velocity;
                    direction = Cross(velocity, normal) > 0
                        ? new Vector2(a._velocity.Y, -a._velocity.X)
                        : new Vector2(-a._velocity.Y, a._velocity.X);
                }

                direction.Normalize();
                if (!float.IsNaN(direction.X))
                {
                    b._velocity = direction * b.Speed + impulse;
                }
            }
        }
    }

    private static void UpdateEdgeContacts(float idt, Span<Movable> movables, ReadOnlySpan<EdgeContact> contacts)
    {
        foreach (ref readonly var c in contacts)
        {
            ref var m = ref movables[c.Index];

            var impulse = c.Normal * c.Penetration * idt;

            if (m.Target is null)
            {
                m._velocity += impulse;
            }
            else
            {
                var perpendicular = Cross(m._velocity, c.Normal) > 0
                    ? new Vector2(c.Normal.Y, -c.Normal.X)
                    : new Vector2(-c.Normal.Y, c.Normal.X);

                m._velocity = perpendicular * m.Speed + impulse;
            }
        }
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - b.X * a.Y;
    }

    private ReadOnlySpan<Contact> FindContacts(ReadOnlySpan<Movable> movables)
    {
        _contacts.Clear();
        _contacts.EnsureCapacity(movables.Length);

        for (var i = 0; i < movables.Length; i++)
        {
            for (var j = i + 1; j < movables.Length; j++)
            {
                ref readonly var a = ref movables[i];
                ref readonly var b = ref movables[j];

                var normal = b._position - a._position;
                var distanceSq = normal.LengthSquared();
                var penetration = a.Radius + b.Radius - MathF.Sqrt(distanceSq);
                if (penetration > 0)
                {
                    normal.Normalize();

                    _contacts.Add(new()
                    {
                        A = i,
                        B = j,
                        Penetration = penetration,
                        Normal = float.IsNaN(normal.X) ? Vector2.UnitX : normal
                    });
                }
            }
        }

        return _contacts;
    }

    private ReadOnlySpan<EdgeContact> FindGridContacts(PathGrid grid, ReadOnlySpan<Movable> movables)
    {
        _edgeContacts.Clear();
        _edgeContacts.EnsureCapacity(movables.Length);

        // 0 --
        // |   |
        //  -- 1
        for (var i = 0; i < movables.Length; i++)
        {
            ref readonly var m = ref movables[i];

            var p0 = new Vector2(m._position.X - m.Radius, m._position.Y - m.Radius);
            var p1 = new Vector2(m._position.X + m.Radius, m._position.Y + m.Radius);

            var gp0 = grid.GetPoint(p0);
            var gp1 = grid.GetPoint(p1);

            // East
            if (m._velocity.X > 0)
            {
                for (var yy = gp0.y; yy <= gp1.y; yy++)
                {
                    if (grid.Bits[gp1.x + yy * grid.Width])
                    {
                        var penetration = p1.X - gp1.x * grid.Step;
                        if (penetration > 0)
                        {
                            _edgeContacts.Add(new() { Index = i, Normal = -Vector2.UnitX, Penetration = penetration });
                        }
                    }
                }
            }

            // West
            if (m._velocity.X < 0)
            {
                for (var yy = gp0.y; yy <= gp1.y; yy++)
                {
                    if (grid.Bits[gp0.x + yy * grid.Width])
                    {
                        var penetration = (gp0.x + 1) * grid.Step - p0.X;
                        if (penetration > 0)
                        {
                            _edgeContacts.Add(new() { Index = i, Normal = Vector2.UnitX, Penetration = penetration });
                        }
                    }
                }
            }

            // South
            if (m._velocity.Y > 0)
            {
                for (var xx = gp0.x; xx <= gp1.x; xx++)
                {
                    if (grid.Bits[xx + gp1.y * grid.Width])
                    {
                        var penetration = p1.Y - gp1.y * grid.Step;
                        if (penetration > 0)
                        {
                            _edgeContacts.Add(new() { Index = i, Normal = -Vector2.UnitY, Penetration = penetration });
                        }
                    }
                }
            }

            // North
            if (m._velocity.Y < 0)
            {
                for (var xx = gp0.x; xx <= gp1.x; xx++)
                {
                    if (grid.Bits[xx + gp0.y * grid.Width])
                    {
                        var penetration = (gp0.y + 1) * grid.Step - p0.Y;
                        if (penetration > 0)
                        {
                            _edgeContacts.Add(new() { Index = i, Normal = Vector2.UnitY, Penetration = penetration });
                        }
                    }
                }
            }
        }

        return _edgeContacts;
    }

    struct Contact
    {
        public int A;
        public int B;
        public float Penetration;
        public Vector2 Normal;
    }

    struct EdgeContact
    {
        public int Index;
        public Vector2 Normal;
        public float Penetration;
     }
}