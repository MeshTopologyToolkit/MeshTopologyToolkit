using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class TRSTransformTests
{
    [Fact]
    public void Combine()
    {
        var rotate = new TRSTransform(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f));
        var translation = new TRSTransform(new Vector3(1, 2, 3));

        var combined = (TRSTransform)rotate.Combine(translation);

        var combinedTranslation = combined.Translation;
        Assert.True((new Vector3(-2, 1, 3) - combinedTranslation).LengthSquared() < 1e-6f);
    }
}
