using Cocona;
using MeshTopologyToolkit.Operators;
using SharpGLTF.Schema2;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateBrickWallCommand : CommandBase
    {
        [Command("brick-wall", Description = "Generate brick wall with randomized bricks.")]
        public int Build(
            [Option('t', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option(Description = "Full trim width in world units")] float widthInUnits = 5.0f,
            [Option(Description = "Wall width in world units")] float wallWidth = 5.0f,
            [Option(Description = "Wall height in world units")] float wallHeight = 4.0f,
            [Option(Description = "Wall thickness in world units. Zero means it matches brick height.")] float wallThickness = 0.0f,
            [Option(Description = "Number of brick columns in the wall")] int columns = 8,
            [Option(Description = "Number of brick rows in the wall")] int rows = 16,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option('n', Description = "Add normal map")] bool normalMap = false,
            [Option('c', Description = "Add checker map as base color (albedo)")] bool checkerMap = false,
            [Option('a', Description = "Base color (albedo) texture file name")] string? albedo = null,
            [Option('o', Description = "Output file name")] string output = "brick-wall.glb")
        {
            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth, widthInUnits: widthInUnits);

            Material material = GenerateBoxCommand.BuildMaterial(normalMap, checkerMap, albedo, args);

            var container = new FileContainer();
            container.Materials.Add(material);
            foreach (var texture in material.TextureParams.Values)
                container.Textures.Add(texture);

            var brickWidth = wallWidth / columns;
            var brickHeight = wallHeight / rows;
            if (wallThickness == 0.0f)
                wallThickness = brickHeight;


            var scene = new Scene();
            container.Scenes.Add(scene);

            return SaveOutputModel(container, output) ? 1 : 0;
        }
    }
    public class GenerateBoxPaletteCommand : CommandBase
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
            for (var sizeX = 0; sizeX < allSizes.Count - 1; ++sizeX)
            {
                for (var sizeY = sizeX; sizeY < allSizes.Count - 1; ++sizeY)
                {
                    for (var sizeZ = sizeY; sizeZ < allSizes.Count; ++sizeZ)
                    {
                        var name = $"box_{sizeX}_{sizeY}_{sizeZ}";
                        UnifiedIndexedMesh mesh = GenerateBoxCommand.BuildBoxMesh(new Vector3(allSizes[sizeX], allSizes[sizeY], allSizes[sizeZ]), args, new BoxBuilder(1, false, false));
                        mesh.Name = name;
                        var node = new Node(name);
                        node.Transform = new TRSTransform(new Vector3(x, allSizes[sizeY] * 0.5f, 0.0f));
                        node.Mesh = new MeshReference(mesh, material);
                        scene.AddChild(node);
                        x += allSizes[sizeX] + 0.1f;
                    }
                }
            }

            {
                var tallestTrim = args.TrimRecepies.OrderByDescending(_ => _.SizeInUnits.Y).First();

                var radius = args.WidthInUnits * 0.5f / MathF.PI;
                for (var tubeSize = 0; tubeSize < allSizes.Count; ++tubeSize)
                {
                    if (allSizes[tubeSize] > radius)
                        continue;
                    x += radius + 0.05f;
                    var name = $"tube_{tubeSize}";
                    UnifiedIndexedMesh mesh = BuildTubeMesh(args, tallestTrim, radius - allSizes[tubeSize], radius);
                    mesh.Name = name;
                    var node = new Node(name);
                    node.Transform = new TRSTransform(new Vector3(x, tallestTrim.SizeInUnits.Y * 0.5f, 0.0f));
                    node.Mesh = new MeshReference(mesh, material);
                    scene.AddChild(node);
                    x += radius + 0.05f;
                    break;
                }
            }

            string fileName = output ?? "box-palette.glb";
            return SaveOutputModel(container, fileName) ? 1 : 0;
        }

        private UnifiedIndexedMesh BuildTubeMesh(TrimGenerationArguments args, TrimRecepie tallestTrim, float innerRadius, float outerRadius)
        {
            var mesh = new UnifiedIndexedMesh();

            var positions = new ListMeshVertexAttribute<Vector3>();
            var normals = new ListMeshVertexAttribute<Vector3>();
            var texCoords = new ListMeshVertexAttribute<Vector2>();

            int numSideSegments = 36;
            var halfHeight = new Vector3(0, tallestTrim.SizeInUnits.Y * 0.5f, 0);
            for (int sideSegmentIndex = 0; sideSegmentIndex <= numSideSegments; ++sideSegmentIndex)
            {
                var factor = sideSegmentIndex / (float)numSideSegments;
                var a = -factor * MathF.PI * 2.0f;
                var normal = new Vector3(MathF.Cos(a), 0.0f, MathF.Sin(a));
                positions.Add(normal * innerRadius - halfHeight);
                positions.Add(normal * innerRadius + halfHeight);
                positions.Add(normal * outerRadius - halfHeight);
                positions.Add(normal * outerRadius + halfHeight);
                normals.Add(-normal);
                normals.Add(-normal);
                normals.Add(normal);
                normals.Add(normal);
                texCoords.Add(new Vector2(tallestTrim.TexCoord.X + tallestTrim.TexCoordSize.X * (1.0f - factor), tallestTrim.TexCoord.Y + tallestTrim.TexCoordSize.Y));
                texCoords.Add(new Vector2(tallestTrim.TexCoord.X + tallestTrim.TexCoordSize.X * (1.0f - factor), tallestTrim.TexCoord.Y));
                texCoords.Add(new Vector2(tallestTrim.TexCoord.X + tallestTrim.TexCoordSize.X * factor, tallestTrim.TexCoord.Y + tallestTrim.TexCoordSize.Y));
                texCoords.Add(new Vector2(tallestTrim.TexCoord.X + tallestTrim.TexCoordSize.X * factor, tallestTrim.TexCoord.Y));
            }
            for (int sideSegmentIndex = 0; sideSegmentIndex < numSideSegments; ++sideSegmentIndex)
            {
                var i0 = sideSegmentIndex * 4;
                var i1 = i0 + 1;
                var i2 = i0 + 4;
                var i3 = i0 + 5;
                mesh.Indices.Add(i0);
                mesh.Indices.Add(i1);
                mesh.Indices.Add(i2);
                mesh.Indices.Add(i1);
                mesh.Indices.Add(i3);
                mesh.Indices.Add(i2);
                mesh.Indices.Add(i0 + 2);
                mesh.Indices.Add(i2 + 2);
                mesh.Indices.Add(i1 + 2);
                mesh.Indices.Add(i1 + 2);
                mesh.Indices.Add(i2 + 2);
                mesh.Indices.Add(i3 + 2);
            }

            mesh.AddAttribute(MeshAttributeKey.Position, positions);
            mesh.AddAttribute(MeshAttributeKey.Normal, normals);
            mesh.AddAttribute(MeshAttributeKey.TexCoord, texCoords);
            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, mesh.Indices.Count));
            mesh = (UnifiedIndexedMesh)new EnsureTangentsOperator().Transform(mesh);
            return mesh;
        }
    }
}