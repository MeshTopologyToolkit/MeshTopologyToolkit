using Cocona;
using MeshTopologyToolkit.TextureFormats;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateNormalMapCommand
    {
        [Command("normalmap", Description = "Generate trim normal map from trim height data.")]
        public int Build(
            [Option('t', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option(Description = "Full trim width in world units")] float widthInUnits = 5.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth);

            var colors = BuildNormalMap(args);

            Converter.SaveAs(output ?? "normals.png", colors, args.WidthInPixels, args.HeightInPixels);

            return 0;
        }

        internal static IFileSystemEntry BuildPng(TrimGenerationArguments args)
        {
            var normalMapGenerator = new GenerateNormalMapCommand();
            var colors = normalMapGenerator.BuildNormalMap(args);

            var ms = new MemoryStream();
            Converter.SaveAsPng(ms, colors, args.WidthInPixels, args.HeightInPixels);
            return new InMemoryFileSystemEntry("normals.png", ms.ToArray());
        }

        public Color32[] BuildNormalMap(TrimGenerationArguments arguments)
        {
            var pixels = BuildHdrNormalMap(arguments);
            var colors = new Color32[pixels.Length];
            for (int i = 0; i < colors.Length; ++i)
            {
                colors[i] = Color32.FromNormal(pixels[i]);
            }
            return colors;
        }


        private Vector3[] BuildHdrNormalMap(TrimGenerationArguments arguments)
        {
            Vector3[] pixels = new Vector3[arguments.WidthInPixels * arguments.HeightInPixels];
            var n = new Vector3(0, 0, 1);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = n;
            }
            var y = 0;
            foreach (var trimHeight in arguments.TrimHeights)
            {
                if (trimHeight >= 2)
                {
                    DrawBevel(pixels, arguments.WidthInPixels, y, trimHeight, arguments.BevelInPixels);
                }
                y += trimHeight;
            }
            return pixels;
        }

        private void DrawBevel(Vector3[] pixels, int width, int top, int trimHeight, int bevel)
        {
            bevel = (int)Math.Min((float)bevel, trimHeight / 4.0f);
            if (bevel < 1)
            {
                return;
            }

            var quaterPi = MathF.PI * 0.25f;
            var corner = Vector3.Normalize(new Vector3(1f, 1f, 1f));
            for (int y = 0; y < trimHeight; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Vector3 n = Vector3.Zero;

                    if (y < bevel)
                    {
                        var a = quaterPi + quaterPi * (y / (float)bevel);
                        n += new Vector3(0f, MathF.Cos(a), 0.0f);
                    }
                    else if (y >= trimHeight - bevel)
                    {
                        var a = quaterPi + quaterPi * ((trimHeight - 1 - y) / (float)bevel);
                        n += new Vector3(0f, -MathF.Cos(a), 0.0f);
                    }
                    if (x < bevel)
                    {
                        var a = quaterPi + quaterPi * (x / (float)bevel);
                        n += new Vector3(-MathF.Cos(a), 0f, 0.0f);
                    }
                    else if (x >= width - bevel)
                    {
                        var a = quaterPi + quaterPi * ((width - 1 - x) / (float)bevel);
                        n += new Vector3(MathF.Cos(a), 0f, 0.0f);
                    }

                    var l = n.LengthSquared();
                    if (l > 1.0f)
                    {
                        n = Vector3.Normalize(n);
                    }
                    else
                    {
                        n = new Vector3(n.X, n.Y, MathF.Sqrt(1.0f - l));
                        while (n.Z < corner.Z - 0.005f)
                        {
                            n.Z = corner.Z;
                            n = Vector3.Normalize(n);
                        }
                    }

                    pixels[(y + top) * width + x] = n;
                }
            }
        }
    }
}