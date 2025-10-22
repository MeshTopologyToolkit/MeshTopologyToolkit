using MeshTopologyToolkit.Gltf;

namespace MeshTopologyToolkit.Tests;

public class GltfFileFormatTests
{
    [Fact]
    public void TwoCorners()
    {
        var fileFormat = new GltfFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.Samples.Corner.TwoCorners.glb"), out var content));

        Assert.NotNull(content);
        Assert.Equal(2, content.Meshes.Count);
    }
}
