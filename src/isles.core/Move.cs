// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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
    private const float VelocityEpsilonSquared = 0.01f;

    private readonly List<Contact> _contacts = new();

    public void Update(float timeStep, Span<Movable> movables)
    {
        var inverseTimeStep = 1.0f / timeStep;
        var contacts = FindContacts(inverseTimeStep, movables);

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

                if (v.LengthSquared() <= PositionEpsilonSquared)
                {
                    m.Velocity = default;
                }
                else
                {
                    v.Normalize();
                    v *= m.Speed;
                    m.Velocity = v;
                }
            }
        }

        const int Iterations = 10;
        for (var itr = 0; itr < Iterations; itr++)
        {
            foreach (ref readonly var c in contacts)
            {
                movables[c.A].Velocity -= c.Impulse;
                movables[c.B].Velocity += c.Impulse;
            }

            foreach (ref var m in movables)
            {
                var v = m.Velocity;
                if (v.LengthSquared() < VelocityEpsilonSquared)
                {
                    m.Velocity = default;
                }
                else
                {
                    v.Normalize();
                    m.Velocity = v * m.Speed;
                }
            }
        }

        foreach (ref var m in movables)
        {
            if (m.Velocity == default)
            {
                m.Target = null;
            }
            else
            {
                Debug.Assert(!float.IsNaN(m.Velocity.X) && !float.IsNaN(m.Velocity.Y));
                m.Position += m.Velocity * timeStep;
            }
        }
    }

    private Span<Contact> FindContacts(float inverseTimeStep, ReadOnlySpan<Movable> movables)
    {
        const float BiasFactor = 0.2f;
        const float AllowedPenetration = 0.01f;

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
                if (distanceSq < PositionEpsilonSquared)
                {
                    continue;
                }

                var penetration = a.Radius + b.Radius - MathF.Sqrt(distanceSq);
                if (penetration > 0)
                {
                    normal.Normalize();
                    var bias = BiasFactor * inverseTimeStep * Math.Max(0.0f, penetration - AllowedPenetration);

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
