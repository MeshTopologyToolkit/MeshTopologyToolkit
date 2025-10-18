using MeshTopologyToolkit.Stl;

namespace MeshTopologyToolkit.Tests;

public class StlFileFormatTests
{
    [Fact]
    public void TwoCorners()
    {
        var fileFormat = new StlFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners.stl"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void TwoCorners_Text()
    {
        var fileFormat = new StlFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners_Text.stl"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void ReadWriteTwoCorners()
    {
        var fileFormat = new StlFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners.stl"), out var content));
        Assert.NotNull(content);

        var memoryStream = new MemoryStream();
        Assert.True(fileFormat.TryWrite(new StreamFileSystemEntry(() => memoryStream), content));

        Assert.True(fileFormat.TryRead(new StreamFileSystemEntry(() => new MemoryStream(memoryStream.ToArray())), out var newContent));
    }
}
