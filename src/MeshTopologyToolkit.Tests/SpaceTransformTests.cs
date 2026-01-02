using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class SpaceTransformTests
{
    private readonly ITestOutputHelper _testOutput;

    public SpaceTransformTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
    }

    [Fact]
    public void TansformPosition()
    {
        var name = new[] { "X", "Y", "Z" };

        for (int first = 0; first <= 2; first++)
            for (int second = 0; second <= 2; second++)
                for (int third = 0; third<= 2; third++)
                {
                    if (third == first || third == second || second == first)
                        continue;

                    for (int signs = 0; signs < 2 * 2 * 2; ++signs)
                    {
                        var names = new[] {
                            name[first],
                            name[second],
                            name[third],
                        };
                        var sign = new[] { 
                            (signs & 1) == 0 ? "" : "-",
                            (signs & 2) == 0 ? "" : "-",
                            (signs & 4) == 0 ? "" : "-",
                        };
                        var copmonents = new[] {
                            $"{sign[0]}{names[0]}",
                            $"{sign[1]}{names[1]}",
                            $"{sign[2]}{names[2]}",
                        };
                        var prefix = sign.Select(_ => _.Replace("-", "_")).ToList();
                        _testOutput.WriteLine($"// Transform matrix that maps vector (X, Y, Z) to ({copmonents[0]}, {copmonents[1]}, {copmonents[2]})");
                        _testOutput.WriteLine($"public static readonly Matrix4x4 {prefix[0]}{names[0]}{prefix[1]}{names[1]}{prefix[2]}{names[2]} = CreateMatrix({sign[0]}Vector3.Unit{names[0]}, {sign[1]}Vector3.Unit{names[1]}, {sign[2]}Vector3.Unit{names[2]});");
}
                }
    }
}
