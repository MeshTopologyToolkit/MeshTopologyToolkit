using Cocona;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Operators;
using MeshTopologyToolkit.Stl;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace MeshTopologyToolkit.BasRelief
{
    public class GenerateBasRelief
    {
        [Command("basrelief", Description = "Generate bas-relief from image.")]
        public int Build(
            [Option('i', Description = "Input image")] string input,
            [Option('t', Description = "Bas-relief thickness")] float thickness = 0.1f,
            [Option('p', Description = "Padding thickness")] float padding = 0.0f,
            [Option('s', Description = "Scale")] float width = 1.0f,
            [Option('b', Description = "Blur radius")] float blurRadius = 0.0f,
            [Option('h', Description = "High pass filter radius")] float highPassRadius = 0.0f,
            [Option('o', Description = "Output STL file")] string? output = null)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                output = Path.ChangeExtension(input, ".stl");
            }

            using (Image<Rgba32> image = Image.Load<Rgba32>(input))
            {
                var heigthmap = new Heightmap(image);

                if (blurRadius > 0)
                    heigthmap = heigthmap.WithBlur(blurRadius);

                if (highPassRadius > 0)
                    heigthmap = heigthmap.WithHighPass(highPassRadius);

                heigthmap = heigthmap.WithScale(thickness, padding);

                var mesh = new UnifiedIndexedMesh(Path.GetFileNameWithoutExtension(output));
                var allPositions = new ListMeshVertexAttribute<Vector3>();
                var positions = new DictionaryMeshVertexAttribute<Vector3>();
                mesh.AddAttribute(MeshAttributeKey.Position, positions);
                var indices = mesh.Indices;

                var pixelSizeInUnits = width / (image.Width-1);

                Vector3 offset = new Vector3(-pixelSizeInUnits * (image.Width - 1) / 2.0f, -pixelSizeInUnits * (image.Height - 1) / 2.0f, 0.0f);

                var getVertex = (int x, int y, float h) =>
                {
                    return (offset + new Vector3(pixelSizeInUnits * x, pixelSizeInUnits * y, h))*new Vector3(1,-1,1);
                };
                for (int y = 0; y < heigthmap.Height; ++y)
                {

                    for (int x = 0; x < image.Width; ++x)
                    {
                        allPositions.Add(getVertex(x, y, heigthmap[x, y]));
                    }
                }

                Vector3 noZ = new Vector3(1, 1, 0);

                for (int x = 0; x < image.Width - 1; ++x)
                {
                    {
                        var a = allPositions[x];
                        var b = allPositions[x + 1];
                        if (a.Z > 0 || b.Z > 0)
                        {
                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(a * noZ);
                            var id = positions.Add(b * noZ);

                            indices.Add(ia);
                            indices.Add(ib);
                            indices.Add(ic);

                            indices.Add(ic);
                            indices.Add(ib);
                            indices.Add(id);
                        }
                    }
                    {
                        var pixelOffset = (image.Height - 1) * image.Width;
                        var a = allPositions[x+ pixelOffset];
                        var b = allPositions[x + 1+ pixelOffset];
                        if (a.Z > 0 || b.Z > 0)
                        {
                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(a * noZ);
                            var id = positions.Add(b * noZ);

                            indices.Add(ia);
                            indices.Add(ic);
                            indices.Add(ib);

                            indices.Add(ic);
                            indices.Add(id);
                            indices.Add(ib);
                        }
                    }
                }

                for (int y=0; y<image.Height-1; ++y)
                {
                    {
                        var a = allPositions[0 + y * image.Width];
                        var b = allPositions[0 + (y + 1) * image.Width];
                        if (a.Z > 0 || b.Z > 0)
                        {
                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(a * noZ);
                            var id = positions.Add(b * noZ);

                            indices.Add(ia);
                            indices.Add(ic);
                            indices.Add(ib);

                            indices.Add(ic);
                            indices.Add(id);
                            indices.Add(ib);
                        }
                    }
                    {
                        var a = allPositions[(y+1) * image.Width-1];
                        var b = allPositions[(y + 2) * image.Width-1];
                        if (a.Z > 0 || b.Z > 0)
                        {
                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(a * noZ);
                            var id = positions.Add(b * noZ);

                            indices.Add(ia);
                            indices.Add(ib);
                            indices.Add(ic);

                            indices.Add(ic);
                            indices.Add(ib);
                            indices.Add(id);
                        }
                    }
                    for (int x = 0; x < image.Width-1; ++x)
                    {
                        var a = allPositions[x + y * image.Width];
                        var b = allPositions[x+1 + y * image.Width];
                        var c = allPositions[x+1 + (y+1) * image.Width];
                        var d = allPositions[x + (y+1) * image.Width];
                        if (a.Z > 0 || b.Z > 0 || c.Z > 0)
                        {
                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(c);

                            indices.Add(ia);
                            indices.Add(ic);
                            indices.Add(ib);

                            ia = positions.Add(a * noZ);
                            ib = positions.Add(b * noZ);
                            ic = positions.Add(c * noZ);

                            indices.Add(ia);
                            indices.Add(ib);
                            indices.Add(ic);
                        }
                        if (a.Z > 0 || c.Z > 0 || d.Z > 0)
                        {
                            var ia = positions.Add(a);
                            var ic = positions.Add(c);
                            var id = positions.Add(d);

                            indices.Add(ia);
                            indices.Add(id);
                            indices.Add(ic);

                            ia = positions.Add(a * noZ);
                            ic = positions.Add(c * noZ);
                            id = positions.Add(d * noZ);

                            indices.Add(ia);
                            indices.Add(ic);
                            indices.Add(id);
                        }
                    }
                }

                mesh = mesh.WithTriangleList();

                var content = new FileContainer();
                content.AddSingleMeshScene(new EnsureNormalsOperator().Transform(mesh));

                var fileFormat = new FileFormatCollection(new StlFileFormat(), new GltfFileFormat());
                if (!fileFormat.TryWrite(new FileSystemEntry(output), content))
                    return 1;
            }

            return 0;
        }
    }

}