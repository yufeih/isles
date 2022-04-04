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
        var testSchema = new { units = Array.Empty<Movable>(), grid = new { } };
        var test = JsonHelper.DeserializeAnonymousType(json, testSchema)!;
        var positions = Array.ConvertAll(test.units, m => new List<Vector2> { m.Position });

        foreach (var m in test.units)
        {
            _svg.AddCircle(m.Position.X, m.Position.Y, m.Radius);
        }

        while (duration < 4)
        {
            _move.Update(timeStep, test.units);
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
        
        var bits = new System.Collections.BitArray(10 * 10);
        bits[0] = true;
        bits[2] = true;
        _svg.AddGrid(10, 10, 1, bits);
        _svg.Snapshot($"move/{name}.svg", duration);
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
