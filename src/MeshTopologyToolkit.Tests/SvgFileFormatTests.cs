using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.SVG;

namespace MeshTopologyToolkit.Tests;

public class SvgFileFormatTests
{
    [Theory]
    [InlineData("samples.primitives.sample.svg")]
    [InlineData("samples.primitives.RectangleWithRoundedCorners.svg")]
    public void ReadAndWriteSamples(string fileName)
    {
        var resourceName = this.GetType().Namespace + "." + fileName;

        var fileFormat = new SvgFileFormat();

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName), out var content));
        Assert.NotNull(content);

        (new GltfFileFormat()).TryWrite(new FileSystemEntry(Path.GetFileNameWithoutExtension(fileName) + ".glb"), content);
    }
}
