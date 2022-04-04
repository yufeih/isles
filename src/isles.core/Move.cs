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
                    var v = 0.2f * c.Normal * c.Penetration * idt;
                    var p = v - dv;
                    a.Velocity -= p * 0.5f;
                    b.Velocity += p * 0.5f;
                }
                else
                {
                    var dv = b.Velocity - a.Velocity;
                    var speed = dv.Length();
                    var v = Cross(dv, c.Normal) > 0
                        ? new Vector2(c.Normal.Y, -c.Normal.X) * speed
                        : new Vector2(-c.Normal.Y, c.Normal.X) * speed;
                    var p = v - dv + 0.2f * c.Normal * c.Penetration * idt;
                    a.Velocity -= p * 0.5f;
                    b.Velocity += p * 0.5f;
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

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - b.X * a.Y;
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