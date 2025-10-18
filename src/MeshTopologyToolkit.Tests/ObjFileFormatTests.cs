using MeshTopologyToolkit.Obj;

namespace MeshTopologyToolkit.Tests;

public class ObjFileFormatTests
{
    [Fact]
    public void TwoCorners_AllAttributes_ZUp()
    {
        var fileFormat = new ObjFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners_AllAttributes_ZUp.obj"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void TwoCorners_NoAttributes_YUp()
    {
        var fileFormat = new ObjFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners_NoAttributes_YUp.obj"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void TwoCorners_Normals()
    {
        var fileFormat = new ObjFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners_Normals.obj"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void TwoCorners_SmoothingGroops()
    {
        var fileFormat = new ObjFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners_SmoothingGroops.obj"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void TwoCorners_UV()
    {
        var fileFormat = new ObjFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners_UV.obj"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }
}
