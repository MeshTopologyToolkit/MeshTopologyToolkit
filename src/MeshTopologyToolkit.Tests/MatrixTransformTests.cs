using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class MatrixTransformTests
{
    [Fact]
    public void Combine()
    {
        var rotate = new MatrixTransform(Matrix4x4.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f)));
        var translation = new MatrixTransform(Matrix4x4.CreateTranslation(new Vector3(1, 2, 3)));

        var combined = (MatrixTransform)rotate.Combine(translation);

        var combinedTranslation = combined.Transform.Translation;
        Assert.True((new Vector3(-2, 1, 3) - combinedTranslation).LengthSquared() < 1e-6f);
    }
}
