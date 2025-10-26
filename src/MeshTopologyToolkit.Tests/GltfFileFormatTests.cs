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

    public static IEnumerable<object[]> EnumerateGltfFiles
    {
        get
        {
            var res = new List<object[]>();
            try
            {
                var rootFolder = @"C:\github\glTF-Sample-Models\2.0";
                if (Directory.Exists(rootFolder))
                {
                    foreach (var fileName in Directory.GetFiles(rootFolder, "*.gltf", SearchOption.AllDirectories))
                    {
                        if (!fileName.Contains("-Draco"))
                            res.Add(new[] { fileName });
                    }

                    foreach (var fileName in Directory.GetFiles(rootFolder, "*.glb", SearchOption.AllDirectories))
                    {
                        if (!fileName.Contains("-Draco"))
                            res.Add(new[] { fileName });
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return res;
        }

    }

    [Theory]
    [MemberData(nameof(EnumerateGltfFiles))]
    public void TestOnSampleModels(string fileName)
    {
        var fileFormat = new GltfFileFormat();

        Assert.True(fileFormat.TryRead(new FileSystemEntry(fileName), out var content));
    }
}
