using Xunit;

namespace isles.tests;

public class MoveTests
{
    [Fact]
    public void Move_Run()
    {
        using var move = new Move();
        move.Step();
    }
}