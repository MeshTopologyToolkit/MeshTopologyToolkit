using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class MatrixTransformTests
{
    [Fact]
    public void Combine()
    {
        Vector3 originalVector = new Vector3(1.1f, 2.2f, 3.3f);
        var rotate = new MatrixTransform(Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f)));
        var translation = new MatrixTransform(Matrix4x4.CreateTranslation(new Vector3(1, 2, 3)));

        var combined = (MatrixTransform)rotate.Combine(translation);

        Vector3 individuallyTranformedPosition = rotate.TransformPosition(translation.TransformPosition(originalVector));
        Vector3 individuallyTranformedNormal = rotate.TransformNormal(translation.TransformNormal(originalVector));
        Vector3 combinedTranformedPosition = combined.TransformPosition(originalVector);
        Vector3 combinedTranformedNormal = combined.TransformNormal(originalVector);

        Assert.Equal(individuallyTranformedPosition, combinedTranformedPosition, Vector3EqualityComparer.Default);
        Assert.Equal(individuallyTranformedNormal, combinedTranformedNormal, Vector3EqualityComparer.Default);
    }
}
