using Cocona;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Stl;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateBoxCommand
    {
        [Command("box", Description = "Generate trim normal map from trim height data.")]
        public int Build(
            [Option('t', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            float widthInUnits = 5.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option(Description = "Box size along X dimention")] float sizeX = 1.0f,
            [Option(Description = "Box size along Y dimention")] float sizeY = 1.0f,
            [Option(Description = "Box size along Z dimention")] float sizeZ = 1.0f,
            [Option('m', Description = "Max deviation from the scale in percents")] float maxDeviation = 10.0f,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth);

            var mesh = Shapes.BuildBox(new ShapeGenerationOptions(MeshAttributeMask.All).WithScale(new Vector3(sizeX, sizeY, sizeZ)));
            var container = new FileContainer();
            container.AddSingleMeshScene(mesh);

            return new FileFormatCollection(new GltfFileFormat(), new StlFileFormat()).TryWrite(output ?? "box.glb", container) ?0:1;
        }
    }
}