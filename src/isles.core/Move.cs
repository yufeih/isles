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
    private const float VelocityEpsilonSquared = 0.01f;
    private const float Skin = 0.2f;
    private const int Iterations = 10;

    private readonly List<Contact> _contacts = new();

    public void Update(float dt, Span<Movable> movables)
    {
        var idt = 1.0f / dt;
        var contacts = FindContacts(idt, movables);

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

        for (var itr = 0; itr < Iterations; itr++)
        {
            foreach (ref readonly var c in contacts)
            {
                ref var a = ref movables[c.A];
                ref var b = ref movables[c.B];

                if (a.Target is null && b.Target is null)
                {
                    var dv = b.Velocity - a.Velocity;
                    var dpn = Vector2.Dot(-dv, c.Normal);
                    var pn = Math.Max(dpn, 0);
                    a.Velocity -= pn * c.Normal;
                    b.Velocity += pn * c.Normal;
                }
                else
                {
                    a.Velocity -= c.Normal * c.Lerp * idt;
                    b.Velocity += c.Normal * c.Lerp * idt;
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
                m.Position += m.Velocity * dt;
            }
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
                var distanceSq = normal.LengthSquared();
                var penetration = a.Radius + b.Radius - MathF.Sqrt(distanceSq);
                if (penetration > 0)
                {
                    normal.Normalize();
                    _contacts.Add(new()
                    {
                        A = i,
                        B = j,
                        Lerp = Math.Min(1, penetration / Skin),
                        Normal = float.IsNaN(normal.X) ? Vector2.One : normal
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
        public float Lerp;
        public Vector2 Normal;
    }
}