using MeshTopologyToolkit.Operators;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class TransformSpaceOperatorTests
{
    private readonly ITestOutputHelper _testOutput;

    public TransformSpaceOperatorTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
    }

    [Fact]
    public void TansformPosition()
    {
        var mesh = new UnifiedIndexedMesh();
        IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
        mesh.AddAttribute(MeshAttributeKey.Position, positions);
        var indices = mesh.Indices;
        indices.Add(positions.Add(new Vector3(1, 2, 3)));
        mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.Points, 0, 1));
        var content = new FileContainer();
        content.AddSingleMeshScene(mesh);
        content.Scenes[0].Children[0].Transform = new TRSTransform(new Vector3(1,2,3));

        var spaceTransform = new SpaceTransform(Matrix4x4.CreateRotationY(MathF.PI / 2.0f) * Matrix4x4.CreateScale(-1, 1, 1), scale: 1.0f);
        var changeSpace = new SpaceTransformOperator(spaceTransform);
        var transformedContent = changeSpace.Transform(content);

        Assert.Equal(new Vector3(-3, 2, -1), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(-3, 2, -1), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);

        spaceTransform = spaceTransform.Invert();
        changeSpace = new SpaceTransformOperator(spaceTransform);
        transformedContent = changeSpace.Transform(transformedContent);

        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);
    }

    [Fact]
    public void TansformWithScale()
    {
        var rotation = Matrix4x4.CreateFromYawPitchRoll(0.5f,0.0f,0.0f);

        var mesh = new UnifiedIndexedMesh();
        IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
        mesh.AddAttribute(MeshAttributeKey.Position, positions);
        var indices = mesh.Indices;
        indices.Add(positions.Add(new Vector3(1, 2, 3)));
        mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.Points, 0, 1));
        var content = new FileContainer();
        content.AddSingleMeshScene(mesh);
        content.Scenes[0].Children[0].Transform = new TRSTransform(new Vector3(1, 2, 3));

        var spaceTransform = new SpaceTransform(Matrix4x4.CreateRotationY(MathF.PI / 2.0f) * Matrix4x4.CreateScale(-1, 1, 1), 1.1f);
        var changeSpace = new SpaceTransformOperator(spaceTransform);
        var transformedContent = changeSpace.Transform(content);

        Assert.Equal(new Vector3(-3.3f, 2.2f, -1.1f), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(-3.3f, 2.2f, -1.1f), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);

        spaceTransform = spaceTransform.Invert();
        changeSpace = new SpaceTransformOperator(spaceTransform);
        transformedContent = changeSpace.Transform(transformedContent);

        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);
    }
}
