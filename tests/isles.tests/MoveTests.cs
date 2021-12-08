using Xunit;

namespace isles.tests;

public class MoveTests
{
    private const float TimeStep = 1.0f / 60;

    [Fact]
    public void Move_Run()
    {
        using var move = new Move();

        var svg = new SvgBuilder();
        var unitCount = 1;

        for (var i = 0; i < unitCount; i++)
        {
            move.AddUnit(0, 0, 5);
            svg.AddCircle(0, 0, 5);
        }

        for (var step = 0; step < 100; step++)
        {
            move.Step(TimeStep);

            for (var i = 0; i < unitCount; i++)
            {
                var (x, y) = move.GetUnitPosition(i);
                svg.AnimateCircle(i, x, y);
            }
        }

        svg.Save("move/move.svg", 100 * TimeStep);
    }
}