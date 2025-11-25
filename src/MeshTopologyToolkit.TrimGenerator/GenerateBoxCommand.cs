using Cocona;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Stl;
using MeshTopologyToolkit.TextureFormats;
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

            var normalTexture = GenerateNormalMapCommand.BuildPng(args);

            var material = new Material("Default", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            material.SetTexture(MaterialParam.Normal, new Texture(normalTexture));

            var size = new Vector3(sizeX, sizeY, sizeZ);
            var halfSize = size * 0.5f;
            var mesh = new UnifiedIndexedMesh();

            var positions = new ListMeshVertexAttribute<Vector3>();
            var normals = new ListMeshVertexAttribute<Vector3>();
            var texCoords = new ListMeshVertexAttribute<Vector2>();
            var tangents = new ListMeshVertexAttribute<Vector4>();

            var addQuad = (BoundingBox3 pos, BoundingBox2 uv, Matrix4x4 tranform) =>
            {
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

                mesh.Indices.Add(startIndex);
                mesh.Indices.Add(startIndex + 1);
                mesh.Indices.Add(startIndex + 2);

                mesh.Indices.Add(startIndex);
                mesh.Indices.Add(startIndex + 2);
                mesh.Indices.Add(startIndex + 3);
            };

            var addSide = (float width, float height, Matrix4x4 tranform) =>
            {
                if (height > width)
                {
                    (height, width) = (width, height);
                    tranform = Matrix4x4.CreateRotationZ(MathF.PI * 0.5f) * tranform;
                }

                var recepie = args.FindMatchingRecepie(height);
                var halfSize = new Vector3(width, height, 0) * 0.5f;
                addQuad(
                    new BoundingBox3(halfSize * new Vector3(-1, -1, 1), halfSize * new Vector3(1, 1, 1)),
                    new BoundingBox2(recepie.TexCoord, recepie.TexCoord + recepie.TexCoordSize),
                    tranform
                    );
            };

            addSide(size.X, size.Y, Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Z)));
            addSide(size.X, size.Y, Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Z)) * Matrix4x4.CreateRotationY(MathF.PI));
            addSide(size.Z, size.Y, Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.X)) * Matrix4x4.CreateRotationY(MathF.PI * 0.5f));
            addSide(size.Z, size.Y, Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.X)) * Matrix4x4.CreateRotationY(-MathF.PI * 0.5f));
            addSide(size.X, size.Z, Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Y)) * Matrix4x4.CreateRotationX(MathF.PI * 0.5f));
            addSide(size.X, size.Z, Matrix4x4.CreateTranslation(new Vector3(0, 0, halfSize.Y)) * Matrix4x4.CreateRotationX(-MathF.PI * 0.5f));

            mesh.AddAttribute(MeshAttributeKey.Position, positions);
            mesh.AddAttribute(MeshAttributeKey.Normal, normals);
            mesh.AddAttribute(MeshAttributeKey.TexCoord, texCoords);
            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, mesh.Indices.Count));
            mesh.EnsureTangents();

            //var mesh = Shapes.BuildBox(new ShapeGenerationOptions(MeshAttributeMask.All).WithScale(new Vector3(sizeX, sizeY, sizeZ)));

            var container = new FileContainer();
            container.AddSingleMeshScene(new MeshReference(mesh, material));

            return new FileFormatCollection(new GltfFileFormat(), new StlFileFormat()).TryWrite(output ?? "box.glb", container) ?0:1;
        }
    }
}