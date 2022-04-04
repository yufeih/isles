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
    public Vector2 Acceleration;
    public Vector2? Target;
}

public class Move
{
    private const float PositionEpsilonSquared = 0.01f;
    private const float VelocityEpsilonSquared = 0.01f;
    private const int Iterations = 10;
    private const float BiasFactor = 0.2f;
    private const float AllowedPenetration = 0.01f;

    private readonly List<Contact> _contacts = new();

    public void Update(float dt, Span<Movable> movables)
    {
        var idt = 1.0f / dt;
        var contacts = FindContacts(idt, movables);

        foreach (ref var m in movables)
        {
            if (m.Target is null)
            {
                m.Acceleration = -m.Velocity * idt;
            }
            else
            {
                var v = m.Target.Value - m.Position;
                if (v.LengthSquared() <= PositionEpsilonSquared)
                {
                    m.Acceleration = -m.Velocity * idt;
                }
                else
                {
                    v.Normalize();
                    m.Acceleration = (v * m.Speed - m.Velocity) * idt;
                }
            }
        }

        for (var itr = 0; itr < Iterations; itr++)
        {
            foreach (ref readonly var c in contacts)
            {
                movables[c.A].Acceleration -= c.Impulse;
                movables[c.B].Acceleration += c.Impulse;
            }

            foreach (ref var m in movables)
            {
                var v = m.Velocity;
                if (v.LengthSquared() < VelocityEpsilonSquared)
                {
                    m.Acceleration = -m.Velocity * idt;
                }
                else
                {
                    v.Normalize();
                    m.Acceleration = (v * m.Speed - m.Velocity) * idt;
                }
            }
        }

        foreach (ref var m in movables)
        {
            m.Velocity += m.Acceleration * dt;
            if (m.Velocity.LengthSquared() <= VelocityEpsilonSquared)
            {
                m.Velocity = default;
                m.Target = default;
            }

            m.Position += m.Velocity * dt;
        }
    }

    private Span<Contact> FindContacts(float idt, ReadOnlySpan<Movable> movables)
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
                var distanceSquared = normal.LengthSquared();
                if (distanceSquared < PositionEpsilonSquared)
                {
                    continue;
                }

                var penetration = a.Radius + b.Radius - MathF.Sqrt(distanceSquared);
                if (penetration > 0)
                {
                    normal.Normalize();
                    var bias = BiasFactor * idt * Math.Max(0.0f, penetration - AllowedPenetration);

                    _contacts.Add(new() { A = i, B = j, Impulse = normal * bias });
                }
            }
        }

        return CollectionsMarshal.AsSpan(_contacts);
    }

    struct Contact
    {
        public int A;
        public int B;
        public Vector2 Impulse;
    }
}
