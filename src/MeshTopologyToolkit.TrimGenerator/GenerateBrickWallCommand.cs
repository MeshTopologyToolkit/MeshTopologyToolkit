using Cocona;
using MeshTopologyToolkit.Operators;
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
            [Option(Description = "Maximum random brick rotation in degrees")] float maxRot = 0.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option('m', Description = "Max deviation from the scale in percents")] float maxDeviation = 10.0f,
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


            var meshes = new[] {
                new EliminateTVerticesOperator().Transform(GenerateBoxCommand.BuildBoxMesh(new Vector3(brickWidth, brickHeight, wallThickness), args, new BoxBuilder(maxDeviation, false, false))),
                new EliminateTVerticesOperator().Transform(GenerateBoxCommand.BuildBoxMesh(new Vector3(brickWidth, brickHeight, wallThickness), args, new BoxBuilder(maxDeviation, true, false))),
                new EliminateTVerticesOperator().Transform(GenerateBoxCommand.BuildBoxMesh(new Vector3(brickWidth, brickHeight, wallThickness), args, new BoxBuilder(maxDeviation, false, true))),
                new EliminateTVerticesOperator().Transform(GenerateBoxCommand.BuildBoxMesh(new Vector3(brickWidth, brickHeight, wallThickness), args, new BoxBuilder(maxDeviation, true, true))),
            };

            foreach (var mesh in meshes)
                container.Meshes.Add(mesh);

            var scene = new Scene();
            container.Scenes.Add(scene);
            var corner = new Vector3(-wallWidth*0.5f, wallHeight, 0.0f);
            var rnd = new Random(0);
            for (int y=0; y<rows; ++y)
            {
                var shiftBricks = (y % 2) == 0;
                var rowCorner = corner + new Vector3(shiftBricks ?-brickWidth*0.5f : 0.0f, -y * brickHeight, 0.0f);
                for (int x = 0; x < columns; ++x)
                {
                    var rotAngle = MathF.PI / 180.0f * maxRot * 2.0f*((float)rnd.NextDouble()-0.5f);
                    var rot = Quaternion.CreateFromYawPitchRoll(rotAngle, 0.0f, 0.0f);
                    var brick = new Node($"brick_{x}_{y}");
                    brick.Mesh = new MeshReference(meshes[rnd.Next(meshes.Length)], material);
                    var pos = rowCorner + new Vector3(x * brickWidth, 0, 0.0f);
                    brick.Transform = new TRSTransform(pos, rot);
                    scene.AddChild(brick);

                    if (x == 0 && shiftBricks)
                    {
                        brick = new Node($"brick_{columns}_{y}");
                        brick.Mesh = new MeshReference(meshes[rnd.Next(meshes.Length)], material);
                        pos = rowCorner + new Vector3(columns * brickWidth, 0, 0.0f);
                        brick.Transform = new TRSTransform(pos, rot);
                        scene.AddChild(brick);
                    }
                }
            }
            
            return SaveOutputModel(container, output) ? 1 : 0;
        }
    }
}