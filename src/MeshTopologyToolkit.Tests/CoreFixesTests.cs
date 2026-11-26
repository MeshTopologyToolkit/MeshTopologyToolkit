using MeshTopologyToolkit.Operators;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class CoreFixesTests
{
    [Fact]
    public void ADrawCallMadeWithAMaterialIndex_KeepsItAsTheMaterial_AndStaysAtTheFinestLevelOfDetail()
    {
        var drawCall = new MeshDrawCall(3, MeshTopology.TriangleList, 0, 6);

        Assert.Equal(0, drawCall.LodLevel);
        Assert.Equal(3, drawCall.MaterialIndex);
    }

    [Fact]
    public void InsertingIntoAContainerCollection_AddsTheItemOnce()
    {
        var content = new FileContainer();
        content.Materials.Insert(0, new Material("A"));

        Assert.Single(content.Materials);
    }

    [Fact]
    public void TheWorldTransformOfANode_IncludesEveryParent()
    {
        var root = new Node("root") { Transform = new TRSTransform(new Vector3(1, 0, 0), Quaternion.Identity, Vector3.One) };
        var middle = new Node("middle") { Transform = new TRSTransform(new Vector3(0, 2, 0), Quaternion.Identity, Vector3.One) };
        var leaf = new Node("leaf") { Transform = new TRSTransform(new Vector3(0, 0, 3), Quaternion.Identity, Vector3.One) };
        root.AddChild(middle);
        middle.AddChild(leaf);

        var world = leaf.GetWorldSpaceTransform().ToTRS();

        Assert.Equal(new Vector3(1, 2, 3), world.Translation);
    }

    [Fact]
    public void AnOperator_KeepsTheTransformAndMeshOfTheSceneRoot()
    {
        var content = new FileContainer();
        var scene = new Scene("scene") { Transform = new TRSTransform(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One) };
        scene.Mesh = new MeshReference(Shapes.BuildBox(), new Material("m"));
        content.Add(scene);

        var result = new ContentOperatorBase().Transform(content);

        var root = Assert.Single(result.Scenes);
        Assert.Equal(new Vector3(5, 0, 0), root.Transform.ToTRS().Translation);
        Assert.NotNull(root.Mesh);
    }
}
