using Cocona;
using MeshTopologyToolkit.Gltf;
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
            [Option('t', Description = "Thickness")] float thickness = 0.1f,
            [Option('s', Description = "Scale")] float width = 1.0f,
            [Option('o', Description = "Output STL file")] string? output = null)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                output = Path.ChangeExtension(input, ".stl");
            }

            using (Image<Rgba32> image = Image.Load<Rgba32>(input))
            {
                var mesh = new UnifiedIndexedMesh(Path.GetFileNameWithoutExtension(output));
                var allPositions = new ListMeshVertexAttribute<Vector3>();
                var positions = new DictionaryMeshVertexAttribute<Vector3>();
                mesh.AddAttribute(MeshAttributeKey.Position, positions);
                var indices = mesh.Indices;

                var pixelSizeInUnits = width / (image.Width-1);

                Vector3 offset = new Vector3(-pixelSizeInUnits * (image.Width - 1) / 2.0f, -pixelSizeInUnits * (image.Height - 1) / 2.0f, 0.0f);

                var getVertex = (int x, int y, Rgba32 color) =>
                {
                    var h = color.R * (1.0f / 255.0f);
                    h *= thickness;
                    return offset + new Vector3(pixelSizeInUnits * x, pixelSizeInUnits * y, h);
                };


                image.ProcessPixelRows(accessor => {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            ref Rgba32 pixel = ref row[x];
                            allPositions.Add(getVertex(x, y, pixel));
                        }
                    }
                });

                for (int y=0; y<image.Height-1; ++y)
                {
                    for (int x = 0; x < image.Width - 1; ++x)
                    {
                        var a = allPositions[x + y * image.Width];
                        var b = allPositions[x+1 + y * image.Width];
                        var c = allPositions[x+1 + (y+1) * image.Width];
                        var d = allPositions[x + (y+1) * image.Width];
                        if (a.Z > 0 || b.Z > 0 || c.Z > 0 || d.Z > 0)
                        {
                            var ia = positions.Add(a);
                            var ib = positions.Add(b);
                            var ic = positions.Add(c);
                            var id = positions.Add(d);

                            indices.Add(ia);
                            indices.Add(ib);
                            indices.Add(ic);

                            indices.Add(ia);
                            indices.Add(ic);
                            indices.Add(id);
                        }
                    }
                }

                mesh = mesh.WithTriangleList();

                var content = new FileContainer();
                content.AddSingleMeshScene(mesh);

                var fileFormat = new FileFormatCollection(new StlFileFormat(), new GltfFileFormat());
                if (!fileFormat.TryWrite(new FileSystemEntry(output), content))
                    return 1;
            }

            return 0;
        }
    }
}