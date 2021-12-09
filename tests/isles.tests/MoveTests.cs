using System.Runtime.CompilerServices;
using Xunit;

namespace isles.tests;

public sealed class MoveTests : IDisposable
{
    private readonly float _speed = 10;
    private readonly Random _random = new(0);
    private readonly Move _move = new();
    private readonly SvgBuilder _svg = new();

    [Fact]
    public void AToB()
    {
        AddUnit(0, 0, 2);
        Run(() => _move.SetUnitTarget(0, 10, 0));
    }

    [Fact]
    public void Spawn()
    {
        AddUnits(10, 5);
        Run(checkSpeed: false);
    }

    [Fact]
    public void PassThrough()
    {
        AddUnit(-10, 0, 2);
        AddUnits(9, 2);

        Run(() => _move.SetUnitTarget(0, 20, 0));
    }

    [Fact]
    public void Corner()
    {
        AddUnit(-5, 5, 1);
        AddObstacle(0, 0, 10, 10);

        Run(() => _move.SetUnitTarget(0, 5, -2));
    }

    private void AddUnits(int count, float radius)
    {
        for (var i = 0; i < count; i++)
        {
            // Give it some random offset to kick start the simulation
            var vx = (_random.NextSingle() - 0.5f) * 0.01f;
            var vy = (_random.NextSingle() - 0.5f) * 0.01f;

            AddUnit(vx, vy, radius);
        }
    }

    private void AddUnit(float x, float y, float radius)
    {
        _move.AddUnit(x, y, radius, _speed);
        _svg.AddCircle(x, y, radius);
    }

    private void AddObstacle(float x, float y, float w, float h)
    {
        _move.AddObstacle(x, y, w, h);
        _svg.AddRectangle(x, y, w, h);
    }

    private void Run(Action? update = null, bool checkSpeed = true, [CallerMemberName] string? testName = null)
    {
        var duration = 0.0f;
        var timeStep = 1.0f / 60;
        var speed = new List<float>();
        var ox = 0.0f;
        var oy = 0.0f;

        while (duration < 5 && _move.IsRunning())
        {
            update?.Invoke();

            _move.Step(timeStep);
            duration += timeStep;

            for (var i = 0; i < _move.UnitCount; i++)
            {
                var (x, y, dx, dy) = _move.GetUnit(i);
                _svg.AnimateCircle(i, x, y);

                if (i == 0)
                {
                    var vx = (x - ox) / timeStep;
                    var vy = (y - oy) / timeStep;
                    speed.Add(MathF.Sqrt(vx * vx + vy * vy));
                    ox = x;
                    oy = y;
                }
            }
        }

        try
        {
            if (checkSpeed)
            {
                CheckSpeed(speed.SkipLast(1), _speed, _speed * 0.01f);
            }
        }
        finally
        {
            _svg.Snapshot($"move/{testName?.ToLowerInvariant()}.svg", duration);
        }
    }

    private void CheckSpeed(IEnumerable<float> speed, float avg, float stddev)
    {
        var actualAvg = speed.Average();
        var actualStddev = Math.Sqrt(speed.Average(v => Math.Pow(v - avg, 2)));
        var message = $"avg: {avg} -> {actualAvg} stddev: {stddev} -> {actualStddev}";

        Assert.True(Math.Abs(avg - actualAvg) < stddev, message);
        Assert.True(actualStddev < stddev, message);
    }

    public void Dispose()
    {
        _move.Dispose();
    }
}
