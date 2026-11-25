// A sandbox app where you can experiment.

using MeshTopologyToolkit;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Operators;

var fileFormat = new FileFormatCollection(new GltfFileFormat());

// ... write your code here:

if (!fileFormat.TryRead(@"C:\github\UltimateTrimImage\Data\Models\BrickFloor01.glb", out var content))
    throw new Exception("Failed to read file");

var mesh = content.Meshes[0];
mesh.RemoveAttribute(MeshAttributeKey.Normal);
mesh.RemoveAttribute(MeshAttributeKey.Tangent);

new EnsureTangentsOperator().Transform(mesh);

if (!fileFormat.TryWrite(@"C:\github\UltimateTrimImage\Data\Models\BrickFloor02.glb", content))
    throw new Exception("Failed to read file");
