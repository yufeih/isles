using System.Collections;
using Xunit;

namespace Isles;

public static class TestHelper
{
    public static PathGrid CreateRandomGrid(Random random, int w = 64, int h = 64, float density = 0.1f)
    {
        var bits = new BitArray(w * h);
        for (var i = 0; i < (int)(w * h * density); i++)
        {
            bits[random.Next(bits.Length)] = true;
        }
        return new PathGrid(w, h, 1, bits);
    }
}
