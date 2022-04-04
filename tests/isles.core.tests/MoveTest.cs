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
    public void MoveTest(string name, string test)
    {
        var duration = 0.0f;
        var timeStep = 1.0f / 60;
        var movables = JsonSerializer.Deserialize<Movable[]>(test, JsonHelper.Options)!;
        var positions = Array.ConvertAll(movables, m => new List<Vector2> { m.Position });

        foreach (var m in movables)
        {
            _svg.AddCircle(m.Position.X, m.Position.Y, m.Radius);
        }

        while (duration < 4)
        {
            _move.Update(timeStep, movables);
            duration += timeStep;

            var running = false;
            for (var i = 0; i < movables.Length; i++)
            {
                ref readonly Movable m = ref movables[i];
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
                _svg.SetCircleData(i, "speed", speeds.Average().ToString());
            }
        }

        _svg.Snapshot($"move/{name}.svg", duration);
    }

    private static TheoryData<string, string> LoadTestCases()
    {
        var result = new TheoryData<string, string>();
        var testCases = JsonSerializer.Deserialize<Dictionary<string, Movable[]>>(
            File.ReadAllBytes("data/move.json"), JsonHelper.Options);

        foreach (var (name, movables) in testCases!)
        {
            result.Add(name, JsonSerializer.Serialize(movables, JsonHelper.Options));
        }

        return result;
    }
}
