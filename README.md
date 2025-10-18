# MeshTopologyToolkit

MeshTopologyToolkit is a lightweight, extensible C# library designed for developers working with 3D mesh data.
It provides utilities for loading, saving, and processing meshes that use either single (unified) or multi-index topologies — enabling seamless conversion, optimization, and manipulation of complex 3D geometry.

✨ Key Features

### Dual topology support
Handle both single-index meshes (shared indices for positions, normals, UVs, etc.) and multi-index meshes (dedicated index sets per attribute).

### Flexible import/export
Read and write common mesh formats (planned extensibility for others).

### Mesh editing utilities
Recalculate normals, merge vertices, remap attributes, and convert between topology types.

### Lightweight and modular
Built with clean, dependency-minimal C# design — ideal for integration into rendering engines, game tools, or CAD pipelines.

### Extensible architecture
Plug in custom attribute types, codecs, or topology converters without modifying the core.

## How to create a mesh from code

```csharp
// Create a new unified mesh object that will store vertex data and indices.
var mesh = new UnifiedIndexedMesh();

// Create a vertex attribute to hold 3D positions for the mesh’s vertices.
// DictionaryMeshVertexAttribute<Vector3> is a simple container that only stores unique vertex positions.
IMeshVertexAttribute<Vector3> positions = new DictionaryMeshVertexAttribute<Vector3>();

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
mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.TriangleList, 0, 3));

// Create a new file container to hold scene assets (e.g. meshes, textures, materials).
var content = new FileContainer();

// Store the mesh we just built in the container.
content.Meshes.Add(mesh);

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
content.Scenes.Add(scene);

// Create an STL file format writer.
var fileFormat = new StlFileFormat();

// Attempt to export the content (which includes the mesh and scene) as an STL file.
// The STL format stores only the mesh geometry, not scene hierarchy or transforms.
// Because of that all meshes in scene going to be merged into a single triangle soup.
fileFormat.TryWrite(new FileSystemEntry("triangle.stl"), content);
```
