using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace MeshTopologyToolkit.TextureFormats
{
    public static class Converter
    {
        private static Rgba32[] ConvertColorsToRgba32(IReadOnlyList<Vector4> pixels)
        {
            Rgba32[] rgbaPixels = new Rgba32[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
            {
                Vector4 v = pixels[i];
                // Clamp in case values are slightly outside 0..1
                v = Vector4.Clamp(v, Vector4.Zero, Vector4.One);
                rgbaPixels[i] = new Rgba32(
                    v.X,   // R  (float 0..1 accepted)
                    v.Y,   // G
                    v.Z,   // B
                    v.W    // A
                );
            }
            return rgbaPixels;
        }

        private static Rgba32[] ConvertColorsToRgba32(IReadOnlyList<Color32> pixels)
        {
            Rgba32[] rgbaPixels = new Rgba32[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
            {
                Color32 v = pixels[i];
                // Clamp in case values are slightly outside 0..1
                rgbaPixels[i] = new Rgba32(
                    v.R,   // R  (float 0..1 accepted)
                    v.G,   // G
                    v.B,   // B
                    v.A    // A
                );
            }
            return rgbaPixels;
        }

        public static void SaveAsPng(Stream stream, Vector4[] pixels, int width, int height)
        {
            SaveAsPngImpl(stream, ConvertColorsToRgba32(pixels), width, height);
        }

        public static void SaveAsPng(Stream stream, Color32[] pixels, int width, int height)
        {
            SaveAsPngImpl(stream, ConvertColorsToRgba32(pixels), width, height);
        }

        private static void SaveAsPngImpl(Stream stream, Rgba32[] rgba32s, int width, int height)
        {
            // Create ImageSharp image
            using var image = new Image<Rgba32>(width, height);

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    image[x, y] = rgba32s[index++];
                }
            }

            // Save as PNG
            image.SaveAsPng(stream);
        }

        public static void SaveAs(string v, Vector4[] pixels, int width, int height)
        {
            switch (Path.GetExtension(v).ToLowerInvariant())
            {
                case ".png":
                    using (var stream = File.Create(v))
                    {
                        SaveAsPng(stream, pixels, width, height);
                    }
                    break;
                default:
                    throw new NotSupportedException($"File extension not supported: {v}");
            }
        }
    }

}
