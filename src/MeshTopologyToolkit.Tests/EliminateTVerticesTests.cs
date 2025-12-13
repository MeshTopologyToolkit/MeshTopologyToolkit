using MeshTopologyToolkit.Operators;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class EliminateTVerticesTests
{
    [Theory]
    [InlineData(0, 0.25f)]
    [InlineData(1, 0.25f)]
    [InlineData(2, 0.25f)]
    [InlineData(0, 0.5f)]
    [InlineData(1, 0.5f)]
    [InlineData(2, 0.5f)]
    [InlineData(0, 0.99f)]
    [InlineData(1, 0.99f)]
    [InlineData(2, 0.99f)]
    public void SplitSide(int side, float factor)
    {
        var sourceMesh = new UnifiedIndexedMesh();
        var sourcePositions = new ListMeshVertexAttribute<Vector3>() { 
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(0,1,0),
        };
        sourceMesh.AddAttribute(MeshAttributeKey.Position, sourcePositions);
        sourceMesh.Indices.Add(0);
        sourceMesh.Indices.Add(1);
        sourceMesh.Indices.Add(2);
        sourceMesh.WithTriangleList();

        var a = sourcePositions[side];
        var b = sourcePositions[(side+1)%3];
        sourcePositions.Add(Vector3.Lerp(a, b, factor));

        var resultMesh = new EliminateTVerticesOperator().Transform(sourceMesh);
        Assert.Equal(4, resultMesh.GetAttribute(MeshAttributeKey.Position).Count);
        var faces = resultMesh.GetFaces().ToList();
        Assert.Equal(2, faces.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SplitSideWithOffset(int side)
    {
        var sourceMesh = new UnifiedIndexedMesh();
        var sourcePositions = new ListMeshVertexAttribute<Vector3>() {
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(0,1,0),
        };
        sourceMesh.AddAttribute(MeshAttributeKey.Position, sourcePositions);
        sourceMesh.Indices.Add(0);
        sourceMesh.Indices.Add(1);
        sourceMesh.Indices.Add(2);
        sourceMesh.WithTriangleList();

        var a = sourcePositions[side];
        var b = sourcePositions[(side + 1) % 3];
        sourcePositions.Add(Vector3.Lerp(a, b, 0.25f)+new Vector3(0.0f, 0.0f, 2e-7f));

        var resultMesh = new EliminateTVerticesOperator().Transform(sourceMesh);
        Assert.Equal(4, resultMesh.GetAttribute(MeshAttributeKey.Position).Count);
        var faces = resultMesh.GetFaces().ToList();
        Assert.Equal(2, faces.Count);
    }
}
