using Xunit;

namespace isles.tests;

public class MoveTests
{
    private const float TimeStep = 1.0f / 60;

    [Fact]
    public void Spawn()
    {
        using var move = new Move();

        var svg = new SvgBuilder();
        var unitCount = 10;
        for (var i = 0; i < unitCount; i++)
        {
            move.AddUnit(0, 0, 5);
            svg.AddCircle(0, 0, 5);
        }

        var duration = 0.0f;
        while (move.IsRunning() && duration < 5)
        {
            move.Step(TimeStep);
            duration += TimeStep;

            for (var i = 0; i < unitCount; i++)
            {
                var (x, y) = move.GetUnitPosition(i);
                svg.AnimateCircle(i, x, y);
            }
        }

        svg.Snapshot("move/spawn.svg", duration);
    }

    [Fact]
    public void PassThrough()
    {
        using var move = new Move();

        var svg = new SvgBuilder();
        var unitCount = 10;
        for (var i = 0; i < unitCount; i++)
        {
            if (i == 0)
            {
                move.AddUnit(-20, 0, 2);
                svg.AddCircle(-20, 0, 2);
            }
            else
            {
                move.AddUnit(0, 0, 5);
                svg.AddCircle(0, 0, 5);
            }
        }

        var duration = 0.0f;
        while (move.IsRunning() && duration < 5)
        {
            if (move.GetUnitPosition(0).x < 10)
            {
                move.SetUnitVelocity(0, 20, 0);
            }

            move.Step(TimeStep);
            duration += TimeStep;

            for (var i = 0; i < unitCount; i++)
            {
                var (x, y) = move.GetUnitPosition(i);
                svg.AnimateCircle(i, x, y);
            }
        }

        svg.Snapshot("move/passthrough.svg", duration);
    }
}
