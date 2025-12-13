using Cocona;
using MeshTopologyToolkit.Operators;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateBoxCommand : CommandBase
    {
        [Command("box", Description = "Generate trim normal map from trim height data.")]
        public int Build(
            [Option('t', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option(Description = "Full trim width in world units")] float widthInUnits = 5.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option(Description = "Box size along X dimention")] float sizeX = 1.0f,
            [Option(Description = "Box size along Y dimention")] float sizeY = 1.0f,
            [Option(Description = "Box size along Z dimention")] float sizeZ = 1.0f,
            [Option(Description = "Snap to right side of the trim")] bool snapToRight = false,
            [Option(Description = "Snap to bottom side of the trim")] bool snapToBottom = false,
            [Option(Description = "Split faces to eliminame T-vertices")] bool noT = false,
            [Option('m', Description = "Max deviation from the scale in percents")] float maxDeviation = 10.0f,
            [Option('n', Description = "Add normal map")] bool normalMap = false,
            [Option('c', Description = "Add checker map as base color (albedo)")] bool checkerMap = false,
            [Option('a', Description = "Base color (albedo) texture file name")] string? albedo = null,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth, widthInUnits: widthInUnits);
            Material material = BuildMaterial(normalMap, checkerMap, albedo, args);

            IMesh mesh = BuildBoxMesh(new Vector3(sizeX, sizeY, sizeZ), args, new BoxBuilder(maxDeviation, snapToRight, snapToBottom));
            if (noT)
            {
                mesh = new EliminateTVerticesOperator().Transform(mesh);
            }

            var container = new FileContainer();
            container.AddSingleMeshScene(new MeshReference(mesh, material));

            string fileName = output ?? "box.glb";
            return SaveOutputModel(container, fileName) ? 1 : 0;
        }


        public static Material BuildMaterial(bool normalMap, bool checkerMap, string? albedo, TrimGenerationArguments args)
        {
            var material = new Material("Default", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

            if (!string.IsNullOrEmpty(albedo))
            {
                material.SetTexture(MaterialParam.BaseColor, new Texture(new FileSystemEntry(albedo)));
            }

            if (normalMap)
            {
                var normalTexture = GenerateNormalMapCommand.BuildPng(args);
                material.SetTexture(MaterialParam.Normal, new Texture(normalTexture));
            }

            if (checkerMap)
            {
                var albedoTexture = GenerateCheckerMapCommand.BuildPng(args.WidthInPixels, args.HeightInPixels, 4, args.TrimRecepies[0].HeightInPixels, 0);
                material.SetTexture(MaterialParam.BaseColor, new Texture(albedoTexture));
            }

            return material;
        }

        public static UnifiedIndexedMesh BuildBoxMesh(Vector3 size, TrimGenerationArguments args, BoxBuilder boxAttributes)
        {
            var halfSize = size * 0.5f;

            boxAttributes.AddSideToBox(args, new Vector2(size.X, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Z)));
            boxAttributes.AddSideToBox(args, new Vector2(size.X, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Z)) * Matrix4x4.CreateRotationY(MathF.PI));
            boxAttributes.AddSideToBox(args, new Vector2(size.Z, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.X)) * Matrix4x4.CreateRotationY(MathF.PI * 0.5f));
            boxAttributes.AddSideToBox(args, new Vector2(size.Z, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.X)) * Matrix4x4.CreateRotationY(-MathF.PI * 0.5f));
            boxAttributes.AddSideToBox(args, new Vector2(size.X, size.Z), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Y)) * Matrix4x4.CreateRotationX(MathF.PI * 0.5f));
            boxAttributes.AddSideToBox(args, new Vector2(size.X, size.Z), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Y)) * Matrix4x4.CreateRotationX(-MathF.PI * 0.5f));

            return boxAttributes.Build();
        }


    }
}