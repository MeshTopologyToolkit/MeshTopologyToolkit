using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Urho3D;
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
}
