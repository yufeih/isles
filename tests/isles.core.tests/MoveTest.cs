using System.Text.Json;
using Xunit;

namespace Isles;

public class MoveTests
{
    private readonly SvgBuilder _svg = new();

    public static TheoryData<string, string> TestCases { get; } = LoadTestCases();

    [Theory]
    [MemberData(nameof(TestCases))]
    public void MoveTest(string name, string json)
    {
        using var move = new Move();
        var duration = 0.0f;
        var timeStep = 1.0f / 60;
        var testSchema = new { units = Array.Empty<MoveUnit>(), grid = Array.Empty<int>() };
        var test = JsonHelper.DeserializeAnonymousType(json, testSchema)!;
        var grid = test.grid != null ? CreateGrid(test.grid) : null;
        var positions = Array.ConvertAll(test.units, m => new List<Vector2> { m.Position });

        while (duration < 4)
        {
            move.Update(timeStep, test.units, grid);
            duration += timeStep;

            var running = false;
            for (var i = 0; i < test.units.Length; i++)
            {
                ref readonly var m = ref test.units[i];
                positions[i].Add(m.Position);
                if (m.Velocity != default)
                {
                    running = true;
                }
            }

            if (!running && duration > 1)
            {
                break;
            }
        }

        for (var i = 0; i < positions.Length; i++)
        {
            var data = default(SvgData);
            var poses = positions[i];
            var radius = test.units[i].Radius;
            var speeds = poses.Skip(1).Select((p, j) => (p - poses[j]).Length() / timeStep).Where(s => s != 0).ToArray();
            if (speeds.Length > 0)
            {
                data = new()
                {
                    Attributes = new()
                    {
                        ["avg-speed"] = speeds.Average().ToString(),
                        ["speed"] = string.Join(", ", speeds),
                    },
                    Duration = duration,
                    Animations = poses.ToArray()
                };
            }
            _svg.AddCircle(poses[0].X, poses[0].Y, radius, data: data);
        }

        if (grid != null)
        {
            _svg.AddGrid(grid);
        }

        Snapshot.Save($"move/{name}.svg", _svg.ToString());
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
