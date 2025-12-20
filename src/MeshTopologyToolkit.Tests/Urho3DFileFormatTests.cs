using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Urho3D;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class Urho3DFileFormatTests
{
    private readonly ITestOutputHelper _testOutput;

    public Urho3DFileFormatTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
    }

    [Theory]
    [InlineData("samples.urho3d.BrickFloor01.mdl")]
    [InlineData("samples.urho3d.Jack.mdl")]
    [InlineData("samples.urho3d.Mutant.mdl")]
    public void ReadAndWriteSamples(string fileName)
    {
        var resourceName = this.GetType().Namespace + "." + fileName;

        var fileFormat = new Urho3DFileFormat();

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName), out var content));
        Assert.NotNull(content);

        (new GltfFileFormat()).TryWrite(new FileSystemEntry(Path.GetFileNameWithoutExtension(fileName) + ".glb"), content);
    }

    [Theory]
    [InlineData("samples.urho3d.BrickFloor01.mdl", Urho3DModelVersion.MorphWeightVersion)]
    [InlineData("samples.urho3d.Jack.mdl", Urho3DModelVersion.Original)]
    [InlineData("samples.urho3d.Mutant.mdl", Urho3DModelVersion.VertexDeclarations)]
    public void ReadWriteAndReadSamples(string fileName, Urho3DModelVersion version)
    {
        var resourceName = this.GetType().Namespace + "." + fileName;

        var fileFormat = new Urho3DFileFormat(version);

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName), out var content));
        Assert.NotNull(content);

        MemoryStream? memoryStream = null;
        fileFormat.TryWrite(new StreamFileSystemEntry(()=> memoryStream = new MemoryStream(), resourceName), content);
        Assert.NotNull(memoryStream);

        Assert.True(fileFormat.TryRead(new InMemoryFileSystemEntry(resourceName, memoryStream.ToArray()), out var restoredContent));

        Assert.Equal(content.Meshes.Count, restoredContent.Meshes.Count);
        Assert.Single(restoredContent.Meshes);

        var meshA = content.Meshes[0];
        var meshB = restoredContent.Meshes[0];

        Assert.Equal(meshA.Name, meshB.Name);
        Assert.Equal(meshA.GetAttribute<Vector3>(MeshAttributeKey.Position), meshB.GetAttribute<Vector3>(MeshAttributeKey.Position), new Vector3EqualityComparer(1e-6f));
        Assert.Equal(meshA.GetAttribute<Vector3>(MeshAttributeKey.Normal), meshB.GetAttribute<Vector3>(MeshAttributeKey.Normal), new Vector3EqualityComparer(1e-6f));
    }


    //[Fact]
    public void MatchGlbTransform()
    {
        var urho3dResourceName = this.GetType().Namespace + ".samples.urho3d.BrickFloor01.mdl";
        var gltfResourceName = this.GetType().Namespace + ".samples.urho3d.BrickFloor01.glb";

        var fileFormat = new FileFormatCollection(
            new FormatAndSpace(new GltfFileFormat(), SpaceTransform.Identity),
            new FormatAndSpace(new Urho3DFileFormat(), new SpaceTransform(Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f))));

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(gltfResourceName), out var gltfContent));
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(urho3dResourceName), out var urho3DContent));

        Assert.Equal(urho3DContent.Meshes.Count, gltfContent.Meshes.Count);
        Assert.Single(urho3DContent.Meshes);

        //Assert.Equal(gltfContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, urho3DContent.Scenes[0].Children[0].Transform.ToMatrix().Translation);

        var meshA = urho3DContent.Meshes[0];
        var meshB = gltfContent.Meshes[0];

        //Assert.Equal(meshA.GetAttribute<Vector3>(MeshAttributeKey.Position), meshB.GetAttribute<Vector3>(MeshAttributeKey.Position), new Vector3EqualityComparer(1e-6f));
        //Assert.Equal(meshA.GetAttribute<Vector3>(MeshAttributeKey.Normal), meshB.GetAttribute<Vector3>(MeshAttributeKey.Normal), new Vector3EqualityComparer(1e-6f));
        //Assert.Equal(meshA.GetAttribute<Vector4>(MeshAttributeKey.Tangent), meshB.GetAttribute<Vector4>(MeshAttributeKey.Tangent), new Vector4EqualityComparer(1e-6f));
    }
}
