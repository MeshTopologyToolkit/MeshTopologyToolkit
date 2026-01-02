using MeshTopologyToolkit.Operators;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class MergeOperatorTests
{
    private readonly ITestOutputHelper _testOutput;

    public MergeOperatorTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
    }

    [Fact]
    public void SingleNode()
    {
        var mesh = TestUtils.BuildPointMesh(new Vector3(2.0f, 3.0f, 4.0f), Vector3.UnitY);

        var spaceTransform = SpaceTransform.Identity;

        var container = TestUtils.BuildSingleNodeScene(SpaceTransform.Identity,
            new TRSTransform(new Vector3(1, 2, 3)),
            mesh);

        var transformedMerged = new MergeOperator().Transform(container);

        var scene = transformedMerged.Scenes[0];
        Assert.True(scene.Transform.IsIdentity);

        var node = scene.Children[0];
        Assert.True(node.Transform.IsIdentity);

        var actualMesh = node.Mesh!.Mesh;

        var positions = actualMesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
        Assert.Equal(new Vector3(3, 5, 7), positions[0], Vector3EqualityComparer.Default);
    }

    [Fact]
    public void NodeHierarchy()
    {
        var mesh = TestUtils.BuildPointMesh(new Vector3(2.0f, 3.0f, 4.0f), Vector3.UnitY);

        var spaceTransform = SpaceTransform.Identity;

        var container = TestUtils.BuildChildParentScene(SpaceTransform.Identity,
            new TRSTransform(new Vector3(1, 2, 3)),
            new TRSTransform(new Vector3(0, -2, 0)),
            mesh);

        var transformedMerged = new MergeOperator().Transform(container);

        var scene = transformedMerged.Scenes[0];
        Assert.True(scene.Transform.IsIdentity);

        var node = scene.Children[0];
        Assert.True(node.Transform.IsIdentity);

        var actualMesh = node.Mesh!.Mesh;

        var positions = actualMesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
        Assert.Equal(new Vector3(3, 3, 7), positions[0], Vector3EqualityComparer.Default);
    }
}
