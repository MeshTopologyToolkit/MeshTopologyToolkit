using Cocona;
using MeshTopologyToolkit.TextureFormats;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateMaskMapCommand
    {
        [Command("maskmap", Description = "Generate mask from trim height data.")]
        public int Build(
            [Option('t', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth);

            var colors = BuildMaskMap(args);

            Converter.SaveAs(output ?? "mask.png", colors, args.WidthInPixels, args.HeightInPixels);

            return 0;
        }

        private Color32[] BuildMaskMap(TrimGenerationArguments arguments)
        {
            Color32[] pixels = new Color32[arguments.WidthInPixels * arguments.HeightInPixels];
            var n = new Color32(255, 255, 255, 255);
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

        private void DrawBevel(Color32[] pixels, int width, int top, int trimHeight, int bevel)
        {
            bevel = (int)Math.Min((float)bevel, trimHeight / 4.0f);
            if (bevel < 1)
            {
                return;
            }

            var n = new Color32(0, 0, 0, 255);
            for (int y = 0; y < trimHeight; ++y)
            {
                for (int x = 0; x < bevel; ++x)
                {
                    pixels[(y + top) * width + x] = n;
                }
                for (int x = width-bevel; x < width; ++x)
                {
                    pixels[(y + top) * width + x] = n;
                }
            }

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < bevel; ++y)
                {
                    pixels[(y + top) * width + x] = n;
                }
                for (int y = trimHeight-bevel; y < trimHeight; ++y)
                {
                    pixels[(y + top) * width + x] = n;
                }
            }

        }
    }
}