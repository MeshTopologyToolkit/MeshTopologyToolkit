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
    public void Face102030()
    {
        var resourceName = this.GetType().Namespace + ".samples.primitives.Face102030";
        var fileFormat = new FileFormatCollection(
            new GltfFileFormat(),
            new ColladaFileFormat());

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName + ".glb"), out var glbContent));
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName + ".dae"), out var daeContent));
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName + "ZUp.dae"), out var daeZUpContent));

        var variants = new List<FileContainer>() { glbContent, daeContent, daeZUpContent };

        PrintNodeTransforms(variants);

        FileContainer Process(FileContainer content)
        {
            content = content.ToGltfSpace();
            content = new MergeOperator().Transform(content);
            return content;
        }
        ;

        variants = variants.Select(Process).ToList();

        _testOutput.WriteLine($"----------------");
        _testOutput.WriteLine($"After tranfsorm:");
        PrintNodeTransforms(variants);

        var meshes = variants.Select(_ => _.Meshes.First()).ToList();

        var positions = meshes.Select(_ => _.GetAttribute<Vector3>(MeshAttributeKey.Position)).ToList();

        for (int i = 0; i < positions.Count; i++)
        {
            _testOutput.WriteLine($"Set {i}:");
            foreach (var pos in positions[i])
            {
                _testOutput.WriteLine($"    Position: {pos}");
            }
        }
    }

    private void PrintNodeTransforms(List<FileContainer> variants)
    {
        for (int i = 0; i < variants.Count; i++)
        {
            _testOutput.WriteLine($"Variant {i}:");

            foreach (var node in variants[i].Scenes[0].VisitAllChildren())
            {
                var t = node.Transform.ToTRS();
                _testOutput.WriteLine($"    Node transform:");
                _testOutput.WriteLine($"      Position: {t.Translation}");
                _testOutput.WriteLine($"      Rotation: {t.Rotation}");
                _testOutput.WriteLine($"      Scale: {t.Scale}");
            }
        }
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
