// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

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

    private readonly List<Contact> _contacts = new();

    public void Update(float dt, Span<Movable> movables)
    {
        var idt = 1.0f / dt;
        var contacts = FindContacts(movables);

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

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - b.X * a.Y;
    }

    private Span<Contact> FindContacts(ReadOnlySpan<Movable> movables)
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

        return CollectionsMarshal.AsSpan(_contacts);
    }

    struct Contact
    {
        public int A;
        public int B;
        public float Penetration;
        public Vector2 Normal;
    }
}