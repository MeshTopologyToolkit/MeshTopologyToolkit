using MeshTopologyToolkit.Collada;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Operators;
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
            new GltfFileFormat(),
            new ColladaFileFormat());

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName+".glb"), out var glbContent));
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName+".dae"), out var daeContent));
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName + "ZUp.dae"), out var daeZUpContent));

        FileContainer Process(FileContainer content)
        {
            content = content.ToGltfSpace();
            return new MergeOperator().Transform(content);
        }
        ;
        var variants = new [] { Process(glbContent), Process(daeContent), Process(daeZUpContent) };

        var meshes = variants.Select(_ => _.Meshes.First()).ToList();

        var positions = meshes.Select(_ => _.GetAttribute<Vector3>(MeshAttributeKey.Position)).ToList();
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
