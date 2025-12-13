using Cocona;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Operators;
using MeshTopologyToolkit.Stl;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace MeshTopologyToolkit.BasRelief
{
    public class GenerateRing
    {
        [Command("ring", Description = "Generate ring mesh from image.")]
        public int Build(
            [Option('i', Description = "Input image")] string input,
            [Option('t', Description = "Bas-relief thickness")] float thickness = 0.1f,
            [Option('p', Description = "Padding thickness")] float padding = 0.0f,
            [Option('r', Description = "Ring inner radius")] float radius = 1.0f,
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
                var positions = new DictionaryMeshVertexAttribute<Vector3>();
                mesh.AddAttribute(MeshAttributeKey.Position, positions);
                var indices = mesh.Indices;

                var ringLength = MathF.PI * (radius + padding) * 2.0f;

                var pixelSizeInUnits = ringLength / image.Width;

                var getVertex = (int x, int y, float h) =>
                {
                    var a = -MathF.PI * (x / (float)image.Width) * 2.0f;
                    var cos = MathF.Cos(a);
                    var sin = MathF.Sin(a);
                    return new Vector3(cos * (radius + h), -pixelSizeInUnits * y, sin * (radius + h));
                };

                for (int x = 0; x < image.Width; ++x)
                {
                    {
                        var y = 0;
                        var ha = heigthmap[x, y];
                        var hb = heigthmap[(x + 1) % image.Width, y];
                        if (ha > 0 || hb > 0)
                        {
                            var a = getVertex(x, y, ha);
                            var b = getVertex((x + 1) % image.Width, y, hb);

                            var c = getVertex(x, y, 0);
                            var d = getVertex((x + 1) % image.Width, y, 0);

                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(c);
                            var id = positions.Add(d);

                            indices.Add(ia);
                            indices.Add(ib);
                            indices.Add(ic);

                            indices.Add(ib);
                            indices.Add(id);
                            indices.Add(ic);
                        }
                    }

                    {
                        var y = image.Height - 1;
                        var ha = heigthmap[x, y];
                        var hb = heigthmap[(x + 1) % image.Width, y];
                        if (ha > 0 || hb > 0)
                        {
                            var a = getVertex(x, y, ha);
                            var b = getVertex((x + 1) % image.Width, y, hb);

                            var c = getVertex(x, y, 0);
                            var d = getVertex((x + 1) % image.Width, y, 0);

                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(c);
                            var id = positions.Add(d);

                            indices.Add(ia);
                            indices.Add(ic);
                            indices.Add(ib);

                            indices.Add(ib);
                            indices.Add(ic);
                            indices.Add(id);
                        }
                    }
                }

                for (int y = 0; y < image.Height - 1; ++y)
                {
                    for (int x = 0; x < image.Width; ++x)
                    {
                        var ha = heigthmap[x, y];
                        var hb = heigthmap[(x + 1) % image.Width, y];
                        var hc = heigthmap[(x + 1) % image.Width, y + 1];
                        var hd = heigthmap[x, y + 1];
                        if (ha > 0 || hb > 0 || hc > 0)
                        {
                            var a = getVertex(x, y, ha);
                            var b = getVertex(x + 1, y, hb);
                            var c = getVertex(x + 1, y + 1, hc);

                            var ia = positions.Add(a);
                            var ic = positions.Add(c);
                            var ib = positions.Add(b);

                            indices.Add(ia);
                            indices.Add(ic);
                            indices.Add(ib);

                            a = getVertex(x, y, 0.0f);
                            b = getVertex(x + 1, y, 0.0f);
                            c = getVertex(x + 1, y + 1, 0.0f);

                            ia = positions.Add(a);
                            ic = positions.Add(c);
                            ib = positions.Add(b);

                            indices.Add(ia);
                            indices.Add(ib);
                            indices.Add(ic);

                        }
                        if (ha > 0 || hc > 0 || hd > 0)
                        {
                            var a = getVertex(x, y, ha);
                            var c = getVertex(x + 1, y + 1, hc);
                            var d = getVertex(x, y + 1, hd);

                            var ia = positions.Add(a);
                            var id = positions.Add(d);
                            var ic = positions.Add(c);

                            indices.Add(ia);
                            indices.Add(id);
                            indices.Add(ic);

                            a = getVertex(x, y, 0.0f);
                            c = getVertex(x + 1, y + 1, 0.0f);
                            d = getVertex(x, y + 1, 0.0f);

                            ia = positions.Add(a);
                            id = positions.Add(d);
                            ic = positions.Add(c);

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