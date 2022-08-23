// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

class MoveContactSolver
{
    private const int IterationCount = 8;
    private const float AllowedPenetration = 0.01f;

    class Contact
    {
        public bool IsDeleted;
        public Vector2 Normal;
        public float Penetration;

        public float Bias;
        public float Magnitude;

        public float? Weight;
        public Vector2 VelocityA;
        public Vector2 VelocityB;
    }

    private readonly Dictionary<(int a, int b), Contact> _contacts = new();

    public void Step(float dt, Span<Movable> movables)
    {
        // Sequential Impulse Solver:
        // https://ubm-twvideo01.s3.amazonaws.com/o1/vault/gdc09/slides/04-GDC09_Catto_Erin_Solver.pdf
        // http://www.richardtonge.com/presentations/Tonge-2012-GDC-solvingRigidBodyContacts.pdf
        IntegrateForce(dt, movables);
        FindContacts(dt, movables);
        SolveContacts(dt, movables);
        IntegratePosition(dt, movables);
    }

    private void IntegrateForce(float dt, Span<Movable> movables)
    {
        foreach (ref var c in movables)
            c.Velocity += c.Force * dt;
    }

    private void IntegratePosition(float dt, Span<Movable> movables)
    {
        foreach (ref var c in movables)
        {
            c.Position += c.Velocity * dt;
            c.Flags = MovableFlags.Awake;
        }
    }

    private void FindContacts(float dt, Span<Movable> movables)
    {
        foreach (var c in _contacts.Values)
            c.IsDeleted = true;

        for (var i = 0; i < movables.Length; i++)
        {
            for (var j = i + 1; j < movables.Length; j++)
            {
                ref var a = ref movables[i];
                ref var b = ref movables[j];

                var normal = b.Position - a.Position;
                var distanceSq = normal.LengthSquared();
                if (distanceSq >= (a.Radius + b.Radius) * (a.Radius + b.Radius) ||
                    distanceSq <= MathFHelper.Epsilon * MathFHelper.Epsilon)
                    continue;

                var distance = MathF.Sqrt(distanceSq);
                normal /= distance;
                var penetration = a.Radius + b.Radius - distance;
                var impulseBias = 0.2f / dt * Math.Max(0, penetration - AllowedPenetration);

                if (_contacts.TryGetValue((i, j), out var c))
                {
                    c.Normal = normal;
                    c.Penetration = penetration;
                    c.Bias = impulseBias;
                    c.IsDeleted = false;
                }
                else if (_contacts.TryGetValue((j, i), out c))
                {
                    c.Normal = -normal;
                    c.Penetration = penetration;
                    c.Bias = impulseBias;
                    c.IsDeleted = false;
                }
                else
                {
                    _contacts.Add((i, j), new()
                    {
                        Normal = normal,
                        Penetration = penetration,
                        Bias = impulseBias,
                        VelocityA = a.Velocity,
                        VelocityB = b.Velocity,
                    });
                }
            }
        }

        foreach (var key in _contacts.Where(c => c.Value.IsDeleted).Select(c => c.Key).ToArray())
            _contacts.Remove(key);
    }

    private void SolveContacts(float dt, Span<Movable> movables)
    {
        for (var iteration = 0; iteration < IterationCount; iteration++)
        {
            // Max speed constraint
            foreach (ref var m in movables)
            {
                var v = m.Velocity;
                var speed = v.Length();
                if (speed > m.Speed)
                {
                    speed -= 0.2f * (speed - m.Speed);
                    m.Velocity = Vector2.Normalize(v) * speed;
                }
            }

            foreach (var ((ia, ib), c) in _contacts)
            {
                ref var a = ref movables[ia];
                ref var b = ref movables[ib];

                var velocity = b.Velocity - a.Velocity;
                var magnitude = -Vector2.Dot(velocity, c.Normal) + c.Bias;

                var totalMagnitude = c.Magnitude;
                c.Magnitude = Math.Max(0, c.Magnitude + magnitude);
                var impulse = c.Normal * (c.Magnitude - totalMagnitude);

                if (Vector2.Dot(velocity, c.Normal) <= 0 && TryMaximizeSpeed(c, ref a, ref b, impulse))
                {
                    c.VelocityA = a.Velocity;
                    c.VelocityB = b.Velocity;
                    continue;
                }

                var weight = c.Weight != null ? c.Weight.Value : GetInitialWeight(a, b);
                var errorA = (a.Velocity - c.VelocityA).Length();
                var errorB = (b.Velocity - c.VelocityB).Length();
                if (errorA + errorB > MathFHelper.Epsilon)
                {
                    var targetWeight = errorB / (errorA + errorB);
                    weight += (targetWeight - weight) * 0.01f;
                }

                // Tunnel test

                a.Velocity -= impulse * weight;
                b.Velocity += impulse * (1 - weight);

                c.Weight = weight;
                c.VelocityA = a.Velocity;
                c.VelocityB = b.Velocity;
            }
        }
    }

    private bool TryMaximizeSpeed(in Contact c, ref Movable a, ref Movable b, Vector2 impulse)
    {
        if (Vector2.Dot(a.Velocity, c.Normal) <= 0)
        {
            var velocityA = a.Velocity - impulse * c.Normal;
            var speedA = velocityA.Length();
            if (speedA <= a.Speed)
            {
                a.Velocity = velocityA;
                return true;
            }
        }

        if (Vector2.Dot(b.Velocity, c.Normal) >= 0)
        {
            var velocityB = b.Velocity + impulse * c.Normal;
            var speedB = velocityB.Length();
            if (speedB <= b.Speed)
            {
                b.Velocity = velocityB;
                return true;
            }
        }

        return false;
    }

    private float GetInitialWeight(in Movable a, in Movable b)
    {
        var remainingSpeedA = Math.Max(0, a.Speed - a.Velocity.Length());
        var remainingSpeedB = Math.Max(0, b.Speed - b.Velocity.Length());
        if (remainingSpeedA + remainingSpeedB > MathFHelper.Epsilon)
            return remainingSpeedA / (remainingSpeedA + remainingSpeedB);
        return 0.5f;
    }
}
