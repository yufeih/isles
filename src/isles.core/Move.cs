// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public struct Movable
{
    public float Radius;
    public float Speed;
    public Vector2 Position;
    public Vector2 Velocity;
    public Vector2? Target;
}

public class Move
{
    private const float PositionEpsilonSquared = 0.01f;
    private const float VelocityEpsilonSquared = 0.001f;
    private const float Bias = 0.5f;

    private ArrayBuilder<Contact> _contacts;
    private ArrayBuilder<EdgeContact> _edgeContacts;

    public void Update(float dt, Span<Movable> movables, PathGrid? grid = null)
    {
        var idt = 1.0f / dt;
        var contacts = FindContacts(movables);
        var edgeContacts = grid != null ? FindGridContacts(grid, movables) : Array.Empty<EdgeContact>();

        for (var i = 0; i < movables.Length; i++)
        {
            ref var m = ref movables[i];
            if (m.Target is null)
            {
                m.Velocity = default;
            }
            else
            {
                var v = m.Target.Value - m.Position;
                m.Velocity = v.LengthSquared() <= PositionEpsilonSquared
                    ? default
                    : Vector2.Normalize(v) * m.Speed;
            }
        }

        UpdateContacts(idt, movables, contacts);

        UpdateEdgeContacts(idt, movables, edgeContacts);

        foreach (ref var m in movables)
        {
            if (m.Velocity.LengthSquared() < VelocityEpsilonSquared)
            {
                m.Velocity = default;
                m.Target = null;
            }
            else
            {
                m.Position += m.Velocity * dt;
            }
        }
    }

    private static void UpdateContacts(float idt, Span<Movable> movables, ReadOnlySpan<Contact> contacts)
    {
        foreach (ref readonly var c in contacts)
        {
            ref var a = ref movables[c.A];
            ref var b = ref movables[c.B];

            var speed = a.Speed + b.Speed;
            var velocity = b.Velocity - a.Velocity;
            var impulse = Bias * c.Normal * c.Penetration * idt;

            if (a.Target is null && b.Target is null)
            {
                a.Velocity -= impulse * 0.5f;
                b.Velocity += impulse * 0.5f;
            }
            else if (a.Target != null && b.Target != null)
            {
                var perpendicular = Cross(velocity, c.Normal) > 0
                    ? new Vector2(c.Normal.Y, -c.Normal.X)
                    : new Vector2(-c.Normal.Y, c.Normal.X);

                var targetVelocity = perpendicular * speed;

                var p = targetVelocity - velocity + impulse;

                a.Velocity -= p * a.Speed / speed;
                b.Velocity += p * b.Speed / speed;
            }
            else
            {
                (a, b) = a.Target != null ? (a, b) : (b, a);

                var perpendicular = Cross(velocity, c.Normal) > 0
                    ? new Vector2(a.Velocity.Y, -a.Velocity.X)
                    : new Vector2(-a.Velocity.Y, a.Velocity.X);

                perpendicular.Normalize();

                if (!float.IsNaN(perpendicular.X))
                {
                    b.Velocity = perpendicular * b.Speed + impulse;
                }
            }
        }
    }

    private static void UpdateEdgeContacts(float idt, Span<Movable> movables, ReadOnlySpan<EdgeContact> contacts)
    {
        foreach (ref readonly var c in contacts)
        {
            ref var m = ref movables[c.Index];

            var impulse = Bias * c.Normal * c.Penetration * idt;

            if (m.Target is null)
            {
                m.Velocity += impulse;
            }
            else
            {
                var perpendicular = Cross(m.Velocity, c.Normal) > 0
                    ? new Vector2(c.Normal.Y, -c.Normal.X)
                    : new Vector2(-c.Normal.Y, c.Normal.X);

                m.Velocity = perpendicular * m.Speed + impulse;
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

                var normal = b.Position - a.Position;
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

        return _contacts.AsSpan();
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

            var p0 = new Vector2(m.Position.X - m.Radius, m.Position.Y - m.Radius);
            var p1 = new Vector2(m.Position.X + m.Radius, m.Position.Y + m.Radius);

            var gp0 = grid.GetPoint(p0);
            var gp1 = grid.GetPoint(p1);

            // left wall
            if (m.Velocity.X > 0)
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

            // right wall
            if (m.Velocity.X < 0)
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

            // upper wall
            if (m.Velocity.Y > 0)
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

            // down wall
            if (m.Velocity.Y < 0)
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

        return _edgeContacts.AsSpan();
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