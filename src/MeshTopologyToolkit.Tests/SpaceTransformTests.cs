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
        for (int i = 1; i <= 4; ++i)
            for (int j = 1; j <= 4; ++j)
            {
                _testOutput.WriteLine($"if (Math.Abs(reconstructed.M{i}{j} - nodeMatrix.Transform.M{i}{j}) > 1e-6f)");
                _testOutput.WriteLine($"throw new Exception($\"Failed to decompose node matrix correctly {{reconstructed.M{i}{j}}} != {{nodeMatrix.Transform.M{i}{j}}}.\");");
            }
    }
}
