using Cocona;
using MeshTopologyToolkit.TextureFormats;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateCheckerMapCommand
    {
        [Command("checkermap", Description = "Generate checker map.")]
        public int Build(
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option('h', Description = "Texture height in pixels")] int height = 1024,
            [Option('s', Description = "Maximum number of shades of gray, rounded to next power of 2")] int levels = 8,
            [Option('c', Description = "Cell size in pixels")] int cellSize = 0,
            [Option('g', Description = "Maximum number of grid levels, rounded to next power of 2")] int gridLevels = 0,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var pixels = Build(width, height, levels, cellSize, gridLevels);

            Converter.SaveAs(output ?? "checker.png", pixels, width, height);
            return 0;
        }

        private Vector4[] Build(int width, int height, int levels, int cellSize, int gridLevels)
        {
            var pixels = new Vector4[width * height];
            levels = levels.NextPowerOfTwo();
            var indexRange = (float)levels;
            if (cellSize <= 0)
                cellSize = Math.Max(1, Math.Min(width, height) / 16);
            var mask = levels - 1;
            var minLevel = 0.25f;
            var levelRange = 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var levelIndex = (x / cellSize) ^ (y / cellSize);
                    levelIndex = levelIndex & mask;
                    var level = levelIndex / indexRange * levelRange + minLevel;
                    pixels[y * width + x] = new Vector4(level, level, level, 1.0f);
                }
            }

            if (gridLevels > 0)
            {
                void DrawRect(int x1, int y1, int x2, int y2)
                {
                    for (int x = x1; x < x2; x++)
                    {
                        pixels[y1 * width + x] = Vector4.UnitW;
                        pixels[(y2 - 1) * width + x] = Vector4.UnitW;
                    }
                    for (int y = y1; y < y2; y++)
                    {
                        pixels[y * width + x1] = Vector4.UnitW;
                        pixels[y * width + (x2 - 1)] = Vector4.UnitW;
                    }
                }
                gridLevels = gridLevels.NextPowerOfTwo();
                for (int x = 0; x < gridLevels; ++x)
                {
                    for (int y = 0; y < gridLevels; y++)
                    {
                        DrawRect(x * width / gridLevels, y * width / gridLevels, (x + 1) * width / gridLevels, (y + 1) * width / gridLevels);
                    }
                }
            }

            if (true)
            {
                void DrawArrows(int x1, int y1)
                {
                    pixels[(y1 + 0) * width + (x1 + 1)] = new Vector4(1, 0, 0, 1);
                    pixels[(y1 + 0) * width + (x1 + 2)] = new Vector4(1, 0, 0, 1);
                    pixels[(y1 - 1) * width + (x1 + 3)] = new Vector4(1, 0, 0, 1);
                    pixels[(y1 + 1) * width + (x1 + 3)] = new Vector4(1, 0, 0, 1);
                    pixels[(y1 + 0) * width + (x1 + 4)] = new Vector4(1, 0, 0, 1);

                    pixels[(y1 - 1) * width + (x1 + 0)] = new Vector4(0, 1, 0, 1);
                    pixels[(y1 - 2) * width + (x1 + 0)] = new Vector4(0, 1, 0, 1);
                    pixels[(y1 - 3) * width + (x1 - 1)] = new Vector4(0, 1, 0, 1);
                    pixels[(y1 - 3) * width + (x1 + 1)] = new Vector4(0, 1, 0, 1);
                    pixels[(y1 - 4) * width + (x1 + 0)] = new Vector4(0, 1, 0, 1);
                }
                var step = cellSize;
                while (step < 9)
                {
                    step *= 2;
                }
                for (int x = 0; x <= width - step; x += step)
                {
                    for (int y = 0; y <= height - step; y += step)
                    {
                        DrawArrows(x + step / 2, y + step / 2);
                    }
                }
            }

            return pixels;
        }


        internal static IFileSystemEntry BuildPng(int width, int height, int levels, int cellSize, int gridLevels)
        {
            var generator = new GenerateCheckerMapCommand();
            var colors = generator.Build(width, height, levels, cellSize, gridLevels);

            var ms = new MemoryStream();
            Converter.SaveAsPng(ms, colors, width, height);
            return new InMemoryFileSystemEntry("checker.png", ms.ToArray());
        }
    }
}