// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

class MoveIsland
{
    private readonly Stack<int> _stack = new();
    private readonly List<int> _island = new();

    public void Solve(ReadOnlySpan<(int a, int b)> contacts, ReadOnlySpan<Movable> movables, Span<Unit> units)
    {
        var islandCount = 1;
        for (var i = 0; i < contacts.Length; i++)
        {
            var (a, b) = contacts[i];
            if (units[a].IslandId != 0 || units[b].IslandId != 0)
                continue;

            _stack.Push(a);
            _stack.Push(b);

            while (_stack.TryPop(out var x))
            {
                _island.Add(x);
                units[x].IslandId = islandCount;
                for (var j = i + 1; j < contacts.Length; j++)
                {
                    var (aa, bb) = contacts[j];
                    if (aa == x && units[bb].IslandId == 0)
                        _stack.Push(bb);
                    if (bb == x && units[aa].IslandId == 0)
                        _stack.Push(aa);
                }
            }

            UpdateIsland(CollectionsMarshal.AsSpan(_island), movables, units);
            islandCount++;
            _island.Clear();
        }
    }

    private void UpdateIsland(ReadOnlySpan<int> island, ReadOnlySpan<Movable> movables, Span<Unit> units)
    {
        
    }

    private void Align(float weight, ReadOnlySpan<int> island, ReadOnlySpan<Movable> movables, Span<Unit> units)
    {
        var averageVelocity = default(Vector2);
        foreach (var i in island)
            averageVelocity += movables[i].Velocity;
        averageVelocity /= island.Length;

        if (averageVelocity.TryNormalize() != 0)
            foreach (var i in island)
                units[i]._contactDirection += weight * averageVelocity;
    }

    private void Separate(float weight, ReadOnlySpan<int> island, ReadOnlySpan<Movable> movables, Span<Unit> units)
    {
        var center = default(Vector2);
        foreach (var i in island)
            center += movables[i].Position;
        center /= island.Length;

        foreach (var i in island)
        {
            var offset = movables[i].Position - center;
            if (offset.TryNormalize() != 0)
                units[i]._contactDirection = offset * weight;
        }
    }

    private void UpdateContact(Span<Movable> movables, Span<Unit> units, in (int a, int b) c)
    {
        ref var ma = ref movables[c.a];
        ref var mb = ref movables[c.b];
        ref var ua = ref units[c.a];
        ref var ub = ref units[c.b];

        if (ua.Target != null && ub.Target != null)
            UpdateContactBothBuzy(ma, ref ua, mb, ref ub);
        else if (ua.Target != null)
            UpdateContactOneBuzyOneIdle(ma, ref ua, mb, ref ub);
        else if (ub.Target != null)
            UpdateContactOneBuzyOneIdle(mb, ref ub, ma, ref ua);
    }

    private void UpdateContactBothBuzy(in Movable ma, ref Unit ua, in Movable mb, ref Unit ub)
    {
        var velocity = mb.Velocity - ma.Velocity;
        var normal = mb.Position - ma.Position;
        if (normal.TryNormalize() == 0)
            return;

        var perpendicular = MathFHelper.Cross(velocity, normal) > 0
            ? new Vector2(normal.Y, -normal.X)
            : new Vector2(-normal.Y, normal.X);

        if (Vector2.Dot(ma.Velocity, mb.Velocity) < 0)
        {
            // Try circle around each other on meeting
            ua._contactDirection -= perpendicular;
            ub._contactDirection += perpendicular;
        }
        else if (ua.Speed > ub.Speed && Vector2.Dot(ma.Velocity, normal) > 0)
        {
            // Try surpass when A chase B
            ua._contactDirection += perpendicular;
        }
        else if (ub.Speed > ua.Speed && Vector2.Dot(mb.Velocity, normal) < 0)
        {
            // Try surpass when B chase A
            ub._contactDirection += perpendicular;
        }
    }

    private void UpdateContactOneBuzyOneIdle(in Movable ma, ref Unit ua, in Movable mb, ref Unit ub)
    {
        var velocity = ma.Velocity;
        var normal = mb.Position - ma.Position;

        // Are we occupying the target?
        var direction = mb.Position - ua.Target!.Value;
        if (direction.LengthSquared() > (ma.Radius + mb.Radius) * (ma.Radius + mb.Radius))
        {
            // Choose a perpendicular direction to give way to the moving unit.
            direction = MathFHelper.Cross(velocity, normal) > 0
                ? new Vector2(-ma.Velocity.Y, ma.Velocity.X)
                : new Vector2(ma.Velocity.Y, -ma.Velocity.X);
        }

        if (direction.TryNormalize() == 0)
            return;

        ub._contactDirection += direction;
    }
}