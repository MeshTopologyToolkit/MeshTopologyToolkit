using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class TRSTransformTests
{
    [Fact]
    public void Combine()
    {
        Vector3 originalVector = new Vector3(1.1f, 2.2f, 3.3f);
        var rotate = new TRSTransform(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f));
        var translation = new TRSTransform(new Vector3(1, 2, 3));

        var combined = (TRSTransform)rotate.Combine(translation);

        Vector3 individuallyTranformedPosition = rotate.TransformPosition(translation.TransformPosition(originalVector));
        Vector3 individuallyTranformedNormal = rotate.TransformNormal(translation.TransformNormal(originalVector));
        Vector3 combinedTranformedPosition = combined.TransformPosition(originalVector);
        Vector3 combinedTranformedNormal = combined.TransformNormal(originalVector);

        Assert.Equal(individuallyTranformedPosition, combinedTranformedPosition, Vector3EqualityComparer.Default);
        Assert.Equal(individuallyTranformedNormal, combinedTranformedNormal, Vector3EqualityComparer.Default);
    }
}
