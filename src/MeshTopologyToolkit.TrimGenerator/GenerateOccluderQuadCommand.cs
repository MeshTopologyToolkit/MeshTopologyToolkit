using Cocona;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateOccluderQuadCommand : CommandBase
    {
        [Command("occluder-quad", Description = "Generate palette of boxes that combine all trim sizes.")]
        public int Build(
            [Option('w', Description = "Occluder width in world units")] float width = 1.0f,
            [Option('h', Description = "Occluder height in world units")] float height = 1.0f,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var container = new FileContainer();

            var mesh = new UnifiedIndexedMesh("Occluder");

            var scale = new Vector3(width, height, 1.0f);
            mesh.AddAttribute(MeshAttributeKey.Position, new ListMeshVertexAttribute<Vector3>
            {
                new Vector3(-0.5f, -0.5f, 0.0f) * scale,
                new Vector3(0.5f, -0.5f, 0.0f) * scale,
                new Vector3(0.5f, 0.5f, 0.0f) * scale,
                new Vector3(-0.5f, 0.5f, 0.0f) * scale,
            });

            mesh.AddIndices(new[] {0,1,2, 0,2,3});

            mesh.WithTriangleList();

            container.AddSingleMeshScene(mesh);

            string fileName = output ?? "occluder-quad.glb";
            return SaveOutputModel(container, fileName) ? 1 : 0;
        }
    }
}