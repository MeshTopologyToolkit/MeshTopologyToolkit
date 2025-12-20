using MeshTopologyToolkit.Stl;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;
public class StlFileFormatTests
{
    [Fact]
    public void TwoCorners()
    {
        var fileFormat = new StlFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.samples.corner.TwoCorners.stl"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void TwoCorners_Text()
    {
        var fileFormat = new StlFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.samples.corner.TwoCorners_Text.stl"), out var content));

        Assert.NotNull(content);
        Assert.Single(content.Meshes);
    }

    [Fact]
    public void ReadWriteTwoCorners()
    {
        var fileFormat = new StlFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.samples.corner.TwoCorners.stl"), out var content));
        Assert.NotNull(content);

        var memoryStream = new MemoryStream();
        Assert.True(fileFormat.TryWrite(new StreamFileSystemEntry(() => memoryStream, "TwoCorners.stl"), content));

        Assert.True(fileFormat.TryRead(new StreamFileSystemEntry(() => new MemoryStream(memoryStream.ToArray()), "TwoCorners.stl"), out var newContent));
    }

    [Fact]
    public void BuildStlFileFromScratch()
    {
        // Create a new unified mesh object that will store vertex data and indices.
        var mesh = new UnifiedIndexedMesh();

        // Create a vertex attribute to hold 3D positions for the mesh’s vertices.
        // ListMeshVertexAttribute<Vector3> is a simple container that stores values as-is.
        IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();

        // Register the position attribute in the mesh using the predefined key for "Position".
        mesh.AddAttribute(MeshAttributeKey.Position, positions);

        // Access the mesh’s index list. This defines how vertices are connected into triangles.
        var indices = mesh.Indices;

        // Add three vertices to the position attribute, each representing a point in 3D space.
        // The Add() method returns the index of the newly added vertex,
        // which we then add to the mesh’s index buffer.
        indices.Add(positions.Add(new Vector3(0, 0, 0)));
        indices.Add(positions.Add(new Vector3(1, 0, 0)));
        indices.Add(positions.Add(new Vector3(0, 1, 0)));

        // Add a draw call defining how to interpret the indices above.
        // Here, MeshTopology.TriangleList means every three indices form a triangle.
        // The parameters (0, 3) mean: start at index 0, use 3 indices → one triangle.
        mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, indices.Count));

        // Create a new file container to hold scene assets (e.g. meshes, textures, materials).
        var content = new FileContainer();

        // Store the mesh we just built in the container.
        content.Add(mesh);

        // Create a simple scene object named "My Scene".
        var scene = new Scene() { Name = "My Scene" };

        // Create a node in the scene. The node holds a transform and references the mesh.
        // Here, TRSTransform applies a translation of +10 units along the X axis.
        var node = new Node()
        {
            Transform = new TRSTransform(new Vector3(10, 0, 0)),
            Mesh = new MeshReference(mesh)
        };
        // Add node the scene.
        scene.AddChild(node);
        // Add scene to the file content.
        content.Add(scene);

        // Create an STL file format writer.
        var fileFormat = new StlFileFormat();

        // Attempt to export the content (which includes the mesh and scene) as an STL file.
        // The STL format stores only the mesh geometry, not scene hierarchy or transforms.
        // Because of that all meshes in scene going to be merged into a single triangle soup.
        fileFormat.TryWrite(new FileSystemEntry("triangle.stl"), content);
    }
}
