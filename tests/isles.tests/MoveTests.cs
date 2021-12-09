using System.Runtime.CompilerServices;
using Xunit;

namespace isles.tests;

public sealed class MoveTests : IDisposable
{
    private readonly Move _move = new();
    private readonly SvgBuilder _svg = new();

    [Fact]
    public void Spawn()
    {
        for (var i = 0; i < 10; i++)
        {
            AddUnit(0, 0, 5);
        }

        Run();
    }

    [Fact]
    public void PassThrough()
    {
        for (var i = 0; i < 10; i++)
        {
            if (i == 0)
            {
                AddUnit(-20, 0, 2);
            }
            else
            {
                AddUnit(0, 0, 5);
            }
        }

        Run(() =>
        {
            if (_move.GetUnitPosition(0).x < 10)
            {
                _move.SetUnitVelocity(0, 20, 0);
            }
        });
    }

    private void AddUnit(float x, float y, float radius)
    {
        _move.AddUnit(x, y, radius);
        _svg.AddCircle(x, y, radius);
    }

    private void Run(Action? update = null, [CallerMemberName]string? testName = null)
    {
        var duration = 0.0f;
        var timeStep = 1.0f / 60;

        while (duration < 5 && _move.IsRunning())
        {
            update?.Invoke();

            _move.Step(timeStep);
            duration += timeStep;

            for (var i = 0; i < _move.UnitCount; i++)
            {
                var (x, y) = _move.GetUnitPosition(i);
                _svg.AnimateCircle(i, x, y);
            }
        }

        _svg.Snapshot($"move/{testName?.ToLowerInvariant()}.svg", duration);
    }

    public void Dispose()
    {
        _move.Dispose();
    }
}
