using System.Numerics;
using System.Reflection;

namespace MeshTopologyToolkit.Tests;

public class SpaceTransformerRotationTests
{
    public static IEnumerable<object[]> AxisMatrices()
    {
        foreach (var field in typeof(SpaceTransform).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(Matrix4x4))
                yield return new object[] { field.Name };
        }
    }

    /// <summary>
    /// Rotating a vector and then changing the space has to give the same result as changing the space first and
    /// rotating by the converted rotation. It holds for any axis permutation, also for the ones that are not symmetric
    /// (for example Z up to Y up) and for the ones that flip the handedness.
    /// </summary>
    [Theory]
    [MemberData(nameof(AxisMatrices))]
    public void TransformedRotationRotatesTransformedVector(string matrixName)
    {
        var matrix = (Matrix4x4)typeof(SpaceTransform).GetField(matrixName, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var transformer = new SpaceTransform(matrix).Transformer;

        var rotations = new[]
        {
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.7f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.1f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -0.4f),
            Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(0.3f, -0.8f, 1.2f)),
        };
        var vectors = new[] { new Vector3(1, 2, 3), new Vector3(-0.5f, 0.25f, 4) };

        foreach (var rotation in rotations)
        {
            foreach (var vector in vectors)
            {
                var expected = transformer.TransformPosition(Vector3.Transform(vector, rotation));
                var actual = Vector3.Transform(transformer.TransformPosition(vector), transformer.TransformRotation(rotation));
                Assert.True(Vector3.Distance(expected, actual) < 1e-4f, $"{matrixName}: expected {expected}, got {actual}");
            }
        }
    }
}
