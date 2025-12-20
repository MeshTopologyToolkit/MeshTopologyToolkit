// A sandbox app where you can experiment.

using MeshTopologyToolkit;
using MeshTopologyToolkit.Gltf;

var fileFormat = new FileFormatCollection(new GltfFileFormat());

// ... write your code here:

if (!fileFormat.TryRead(@"path_to_file.glb", out var content))
    throw new Exception("Failed to read file");

if (!fileFormat.TryWrite(@"path_to_file.glb", content))
    throw new Exception("Failed to read file");
