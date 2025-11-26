using Cocona;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Stl;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateBoxPaletteCommand
    {
        [Command("box-palette", Description = "Generate palette of boxes that combine all trim sizes.")]
        public int Build(
            [Option('t', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option(Description = "Full trim width in world units")] float widthInUnits = 5.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option('n', Description = "Add normal map")] bool normalMap = false,
            [Option('c', Description = "Add checker map as base color (albedo)")] bool checkerMap = false,
            [Option('a', Description = "Base color (albedo) texture file name")] string? albedo = null,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth, widthInUnits: widthInUnits);

            Material material = GenerateBoxCommand.BuildMaterial(normalMap, checkerMap, albedo, args);

            var container = new FileContainer();
            container.Materials.Add(material);
            foreach (var texture in material.TextureParams.Values)
                container.Textures.Add(texture);

            var scene = new Scene();
            container.Scenes.Add(scene);

            var allSizes = args.TrimRecepies.Select(_ => _.SizeInUnits.Y).Concat(new[] { args.TrimRecepies[0].SizeInUnits.X }).Distinct().Order().ToList();
            var x = 0.0f;
            for (var sizeX = 0; sizeX < allSizes.Count-1; ++sizeX)
            {
                for (var sizeY = sizeX; sizeY < allSizes.Count-1; ++sizeY)
                {
                    for (var sizeZ = sizeY; sizeZ < allSizes.Count; ++sizeZ)
                    {
                        var name = $"box_{sizeX}_{sizeY}_{sizeZ}";
                        UnifiedIndexedMesh mesh = GenerateBoxCommand.BuildBoxMesh(bevelWidth, new Vector3(allSizes[sizeX], allSizes[sizeY], allSizes[sizeZ]), 1, args);
                        mesh.Name = name;
                        var node = new Node(name);
                        node.Transform = new TRSTransform(new Vector3(x, allSizes[sizeY] * 0.5f, 0.0f));
                        node.Mesh = new MeshReference(mesh, material);
                        scene.AddChild(node);
                        x += allSizes[sizeX] + 0.1f;
                    }
                }
            }

            string fileName = output ?? "box-palette.glb";
            if (!new FileFormatCollection(new GltfFileFormat(), new StlFileFormat()).TryWrite(fileName, container))
            {
                Console.Error.WriteLine($"Failed to save file {output}");
                return 1;
            }
            return 0;
        }
    }
}