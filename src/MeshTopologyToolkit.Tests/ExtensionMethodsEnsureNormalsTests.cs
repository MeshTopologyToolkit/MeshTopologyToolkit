using MeshTopologyToolkit.Operators;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class ExtensionMethodsEnsureNormalsTests
{
    //[Fact]
    public void RandomizedProjection()
    {
        var rnd = new Random(0);

        for (int iteration = 0; iteration < 10; iteration++)
        {
            var rotation = GetRandomRotation(rnd);
            var projectionRotation = GetRandomRotation(rnd);

            var faceNormal = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            var mesh = new SeparatedIndexedMesh();
            var n = Vector3.Transform(Vector3.UnitY, rotation);
            {
                var positions = new ListMeshVertexAttribute<Vector3>
                {
                    Vector3.Transform(new Vector3(-1, -1, 0), rotation),
                    Vector3.Transform(new Vector3(1, -1, 0), rotation),
                    Vector3.Transform(new Vector3(1, 1, 0), rotation)
                };
                mesh.AddAttribute(MeshAttributeKey.Position, positions, new[] { 0, 1, 2 });
            }
            {
                var normals = new ListMeshVertexAttribute<Vector3>
                {
                    faceNormal
                };
                mesh.AddAttribute(MeshAttributeKey.Normal, normals, new[] { 0, 0, 0 });
            }
            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, 3));

            Matrix4x4.Invert(Matrix4x4.CreateFromQuaternion(projectionRotation), out var inverseProjection);
            mesh.ApplyUVProjection(inverseProjection);
            var expectedTangent = Vector3.Transform(new Vector3(1, 0, 0), projectionRotation);
            mesh = (SeparatedIndexedMesh)new EnsureTangentsOperator().Transform(mesh);

            expectedTangent = expectedTangent - Vector3.Dot(expectedTangent, faceNormal) * faceNormal;
            expectedTangent = Vector3.Normalize(expectedTangent);

            var tangents = mesh.GetAttribute<Vector4>(MeshAttributeKey.Tangent);
            Assert.Single(tangents);
            var actualTangent = new Vector3(tangents[0].X, tangents[0].Y, tangents[0].Z);
            Assert.True((expectedTangent - actualTangent).LengthSquared() < 1e-6f, $"Iteration[{iteration}]: Expected tangent {expectedTangent} but evaluated {actualTangent}");
        }
    }

    private static Quaternion GetRandomRotation(Random rnd)
    {
        return Quaternion.CreateFromYawPitchRoll(MathF.PI * 2.0f * (float)rnd.NextDouble(), MathF.PI * (float)rnd.NextDouble(), MathF.PI * (float)rnd.NextDouble());
    }
    private static Vector3 GetRandomVector(Random rnd)
    {
        return new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() - 0.5f);
    }
}
