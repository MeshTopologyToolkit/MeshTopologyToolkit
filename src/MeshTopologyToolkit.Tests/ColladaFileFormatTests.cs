using MeshTopologyToolkit.Collada;
using MeshTopologyToolkit.Gltf;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class ColladaFileFormatTests
{
    private readonly ITestOutputHelper _testOutput;

    public ColladaFileFormatTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
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
