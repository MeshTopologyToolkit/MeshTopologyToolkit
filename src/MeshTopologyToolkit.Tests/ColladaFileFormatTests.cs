using MeshTopologyToolkit.Collada;
using MeshTopologyToolkit.Gltf;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class ColladaFileFormatTests
{
    private readonly ITestOutputHelper _testOutput;

    public ColladaFileFormatTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
    }

    [Fact]
    public void Face102030YUp()
    {
        var resourceName = this.GetType().Namespace + ".samples.primitives.Face102030";
        var fileFormat = new FileFormatCollection(
            new FormatAndSpace(new GltfFileFormat(), new SpaceTransform(Matrix4x4.CreateScale(10.0f/0.254f))),
            new FormatAndSpace(new ColladaFileFormat(), new SpaceTransform(Matrix4x4.Identity, flipV: true)));

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName+".glb"), out var glbContent));
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName+".dae"), out var daeContent));

        var glbScene = glbContent.Scenes.First();
        var daeScene = daeContent.Scenes.First();

        var glbNode = glbScene.Children.First();
        var daeNode = daeScene.Children.First();

        var glbTransform = glbNode.Transform;
        var daeTransform = daeNode.Transform;

        var glbMesh = glbContent.Meshes.First();
        var daeMesh = daeContent.Meshes.First();

        var glbPositions = glbMesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
        var daePositions = daeMesh.GetAttribute<Vector3>(MeshAttributeKey.Position);

        var glbPositionsIndices = glbMesh.GetAttributeIndices(MeshAttributeKey.Position);
        var daePositionsIndices = daeMesh.GetAttributeIndices(MeshAttributeKey.Position);
    }

    [Theory]
    //[InlineData("samples.corner.TwoCorners.glb")]
    //[InlineData("samples.kronos.AnimatedMorphCube.glb")]
    [InlineData("samples.kronos.MultiUVTest.glb")]
    [InlineData("samples.kronos.NormalTangentMirrorTest.glb")]
    [InlineData("samples.kronos.SimpleInstancing.glb")]
    [InlineData("samples.kronos.SimpleMeshes.gltf")]
    [InlineData("samples.kronos.SimpleMorph.gltf")]
    [InlineData("samples.kronos.SimpleSkin.gltf")]
    [InlineData("samples.kronos.VC.glb")]
    [InlineData("samples.primitives.Primitives.glb")]
    [InlineData("samples.primitives.TangentFaces.glb")]
    public void ReadAndWriteSamples(string fileName)
    {
        var resourceName = this.GetType().Namespace + "." + fileName;

        var fileFormat = new FileFormatCollection(new GltfFileFormat(), new ColladaFileFormat());

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName), out var content));
        Assert.NotNull(content);

        fileFormat.TryWrite(new FileSystemEntry(Path.GetFileNameWithoutExtension(fileName) + ".dae"), content);
    }
}
