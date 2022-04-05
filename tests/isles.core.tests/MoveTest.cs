using System.Text.Json;
using Xunit;

namespace Isles;

public class MoveTests
{
    private readonly Move _move = new();
    private readonly SvgBuilder _svg = new();

    public static TheoryData<string, string> TestCases { get; } = LoadTestCases();

    [Theory]
    [MemberData(nameof(TestCases))]
    public void MoveTest(string name, string json)
    {
        var duration = 0.0f;
        var timeStep = 1.0f / 60;
        var testSchema = new { units = Array.Empty<Movable>(), grid = Array.Empty<int>() };
        var test = JsonHelper.DeserializeAnonymousType(json, testSchema)!;
        var grid = test.grid != null ? CreateGrid(test.grid) : null;
        var positions = Array.ConvertAll(test.units, m => new List<Vector2> { m.Position });

        foreach (var m in test.units)
        {
            _svg.AddCircle(m.Position.X, m.Position.Y, m.Radius);
        }

        while (duration < 4)
        {
            _move.Update(timeStep, test.units, grid);
            duration += timeStep;

            var running = false;
            for (var i = 0; i < test.units.Length; i++)
            {
                ref readonly var m = ref test.units[i];
                _svg.AnimateCircle(i, m.Position.X, m.Position.Y);

                if (m.Velocity != default)
                {
                    running = true;
                    positions[i].Add(m.Position);
                }
            }

            if (!running && duration > 1)
            {
                break;
            }
        }

        for (var i = 0; i < positions.Length; i++)
        {
            var speeds = positions[i].Skip(1).Select((p, j) => (p - positions[i][j]).Length() / timeStep).ToArray();
            if (speeds.Length > 0)
            {
                _svg.SetCircleData(i, "avg-speed", speeds.Average().ToString());
                _svg.SetCircleData(i, "speed", string.Join(", ", speeds));
            }
        }

        if (grid != null)
        {
            _svg.AddGrid(grid);
        }

        _svg.Snapshot($"move/{name}.svg", duration);
    }

    private static PathGrid CreateGrid(int[] grid)
    {
        var size = (int)Math.Sqrt(grid.Length);
        var bits = new System.Collections.BitArray(grid.Select(g => g != 0).ToArray());
        return new(size, size, 10, bits);
    }

    private static TheoryData<string, string> LoadTestCases()
    {
        var result = new TheoryData<string, string>();
        var testCases = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            File.ReadAllBytes("data/move.json"), JsonHelper.Options);

        foreach (var (name, value) in testCases!)
        {
            result.Add(name, value.ToString());
        }

        return result;
    }
}
