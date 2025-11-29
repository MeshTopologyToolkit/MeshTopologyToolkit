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
            [Option(Description = "Full trim width in world units")] float widthInUnits = 5.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option(Description = "Box size along X dimention")] float sizeX = 1.0f,
            [Option(Description = "Box size along Y dimention")] float sizeY = 1.0f,
            [Option(Description = "Box size along Z dimention")] float sizeZ = 1.0f,
            [Option('m', Description = "Max deviation from the scale in percents")] float maxDeviation = 10.0f,
            [Option('n', Description = "Add normal map")] bool normalMap = false,
            [Option('c', Description = "Add checker map as base color (albedo)")] bool checkerMap = false,
            [Option('a', Description = "Base color (albedo) texture file name")] string? albedo = null,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth, widthInUnits: widthInUnits);
            Material material = BuildMaterial(normalMap, checkerMap, albedo, args);

            UnifiedIndexedMesh mesh = BuildBoxMesh(new Vector3(sizeX, sizeY, sizeZ), maxDeviation, args);

            var container = new FileContainer();
            container.AddSingleMeshScene(new MeshReference(mesh, material));

            string fileName = output ?? "box.glb";
            if (!new FileFormatCollection(new GltfFileFormat(), new StlFileFormat()).TryWrite(fileName, container))
            {
                Console.Error.WriteLine($"Failed to save file {output}");
                return 1;
            }
            return 0;
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

        public static UnifiedIndexedMesh BuildBoxMesh(Vector3 size, float maxDeviation, TrimGenerationArguments args)
        {
            var halfSize = size * 0.5f;
            var boxAttributes = new BoxBuilder();

            AddSideToBox(maxDeviation, args, new Vector2(size.X, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Z)), boxAttributes);
            AddSideToBox(maxDeviation, args, new Vector2(size.X, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Z)) * Matrix4x4.CreateRotationY(MathF.PI), boxAttributes);
            AddSideToBox(maxDeviation, args, new Vector2(size.Z, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.X)) * Matrix4x4.CreateRotationY(MathF.PI * 0.5f), boxAttributes);
            AddSideToBox(maxDeviation, args, new Vector2(size.Z, size.Y), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.X)) * Matrix4x4.CreateRotationY(-MathF.PI * 0.5f), boxAttributes);
            AddSideToBox(maxDeviation, args, new Vector2(size.X, size.Z), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Y)) * Matrix4x4.CreateRotationX(MathF.PI * 0.5f), boxAttributes);
            AddSideToBox(maxDeviation, args, new Vector2(size.X, size.Z), Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Y)) * Matrix4x4.CreateRotationX(-MathF.PI * 0.5f), boxAttributes);

            return boxAttributes.Build();
        }

        public static void AddSideToBox(float maxDeviation, TrimGenerationArguments args, Vector2 size, Matrix4x4 tranform, BoxBuilder boxAttributes)
        {
            float width = size.X;
            float height = size.Y;

            if (height > width)
            {
                (height, width) = (width, height);
                tranform = Matrix4x4.CreateRotationZ(MathF.PI * 0.5f) * tranform;
            }

            var recepie = args.FindMatchingRecepie(height);

            var halfSize = new Vector3(width, height, 0) * 0.5f;
            var requestedSizeInUV = new Vector2(width, height) * args.UnitsToUV;

            // If requested normal map slice is bigger than the recepie slice we have to scale the requested size down.
            if (requestedSizeInUV.X > recepie.TexCoordSize.X || requestedSizeInUV.Y > recepie.TexCoordSize.Y)
            {
                var scaleFactor = MathF.Min(recepie.TexCoordSize.X / requestedSizeInUV.X, recepie.TexCoordSize.Y / requestedSizeInUV.Y);
                requestedSizeInUV *= scaleFactor;
            }

            (var leftFactor, var rightFactor) = GetSliceFactors(maxDeviation, requestedSizeInUV.X, recepie.TexCoordSize.X, recepie.BevelSize.X);
            (var bottomFactor, var topFactor) = GetSliceFactors(maxDeviation, requestedSizeInUV.Y, recepie.TexCoordSize.Y, recepie.BevelSize.Y);
            if (rightFactor == 0 || leftFactor == 0)
                requestedSizeInUV.X = recepie.TexCoordSize.X;
            if (bottomFactor == 0 || topFactor == 0)
                requestedSizeInUV.Y = recepie.TexCoordSize.Y;

            var wholeBox = new BoundingBox3(halfSize * new Vector3(-1, -1, 1), halfSize * new Vector3(1, 1, 1));
            var wholeUvBox = FlipY(new BoundingBox2(recepie.TexCoord, recepie.TexCoord + recepie.TexCoordSize));
            var requestedUvBox = FlipY(new BoundingBox2(recepie.TexCoord, recepie.TexCoord + requestedSizeInUV));
            var breakPoint = wholeBox.Lerp(new Vector3(leftFactor, topFactor, 0.0f));
            var breakUvPoint = requestedUvBox.Lerp(new Vector2(leftFactor, topFactor));

            AddQuadToBox(
                new BoundingBox3(wholeBox.Min, breakPoint),
                new BoundingBox2(requestedUvBox.Min, breakUvPoint).AlignWithin(wholeUvBox, new Vector2(0, 0)),
                tranform,
                boxAttributes);
            AddQuadToBox(
                new BoundingBox3(breakPoint, wholeBox.Max),
                new BoundingBox2(breakUvPoint, requestedUvBox.Max).AlignWithin(wholeUvBox, new Vector2(1, 1)),
                tranform,
                boxAttributes);
            AddQuadToBox(
                new BoundingBox3(
                    wholeBox.Min * new Vector3(1, 0, 1) + breakPoint * new Vector3(0, 1, 0),
                    breakPoint * new Vector3(1, 0, 1) + wholeBox.Max * new Vector3(0, 1, 0)
                    ),
                new BoundingBox2(
                    requestedUvBox.Min * new Vector2(1, 0) + breakUvPoint * new Vector2(0, 1),
                    breakUvPoint * new Vector2(1, 0) + requestedUvBox.Max * new Vector2(0, 1)
                    ).AlignWithin(wholeUvBox, new Vector2(0, 1)),
                tranform,
                boxAttributes);
            AddQuadToBox(
                new BoundingBox3(
                    wholeBox.Min * new Vector3(0, 1, 1) + breakPoint * new Vector3(1, 0, 0),
                    breakPoint * new Vector3(0, 1, 1) + wholeBox.Max * new Vector3(1, 0, 0)
                    ),
                new BoundingBox2(
                    requestedUvBox.Min * new Vector2(0, 1) + breakUvPoint * new Vector2(1, 0),
                    breakUvPoint * new Vector2(0, 1) + requestedUvBox.Max * new Vector2(1, 0)
                    ).AlignWithin(wholeUvBox, new Vector2(1, 0)),
                tranform,
                boxAttributes);
        }

        private static bool AddQuadToBox(BoundingBox3 pos, BoundingBox2 uv, Matrix4x4 tranform, BoxBuilder boxAttributes)
        {
            if (MathF.Abs(pos.Max.X - pos.Min.X) < 1e-6f)
                return false;
            if (MathF.Abs(pos.Max.Y - pos.Min.Y) < 1e-6f)
                return false;

            var positions = boxAttributes.Positions;
            var normals = boxAttributes.Normals;
            var texCoords = boxAttributes.TexCoords;

            var startIndex = positions.Count;

            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(0, 0, 0)), tranform));
            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(1, 0, 0)), tranform));
            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(1, 1, 0)), tranform));
            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(0, 1, 0)), tranform));

            var n = Vector3.TransformNormal(new Vector3(0, 0, 1), tranform);
            normals.Add(n);
            normals.Add(n);
            normals.Add(n);
            normals.Add(n);

            texCoords.Add(uv.Lerp(new Vector2(0, 0)));
            texCoords.Add(uv.Lerp(new Vector2(1, 0)));
            texCoords.Add(uv.Lerp(new Vector2(1, 1)));
            texCoords.Add(uv.Lerp(new Vector2(0, 1)));

            boxAttributes.Indices.Add(startIndex);
            boxAttributes.Indices.Add(startIndex + 1);
            boxAttributes.Indices.Add(startIndex + 2);
            boxAttributes.Indices.Add(startIndex);
            boxAttributes.Indices.Add(startIndex + 2);
            boxAttributes.Indices.Add(startIndex + 3);
            return true;
        }

        private static BoundingBox2 FlipY(BoundingBox2 boundingBox2)
        {
            return new BoundingBox2(new Vector2(boundingBox2.Min.X, boundingBox2.Max.Y), new Vector2(boundingBox2.Max.X, boundingBox2.Min.Y));
        }

        private static (float, float) GetSliceFactors(float maxDeviation, float requestedSize, float actualSize, float bevelSize)
        {
            var scale = requestedSize / actualSize;
            if (scale < 0)
                scale = 1.0f / scale;

            var deviation = MathF.Abs(1.0f - scale) * 100.0f;
            if (deviation < maxDeviation)
                return (1.0f, 0.0f);

            if (requestedSize < actualSize)
            {
                var bevelFactor = MathF.Min(2.0f * bevelSize / requestedSize, 0.5f);
                return (1.0f- bevelFactor, bevelFactor);
            }
            else
            {
                var fullWidthA = Math.Max(actualSize - 2.0f * bevelSize, bevelSize);
                var fullWidthB = requestedSize - fullWidthA;
                return (fullWidthA / requestedSize, fullWidthB / requestedSize);
            }
        }
    }
}