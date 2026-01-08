using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.Text;

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
            using var image = ToImage(ConvertColorsToRgba32(pixels), width, height);
            image.SaveAsPng(stream);
        }

        public static void SaveAsPng(Stream stream, Color32[] pixels, int width, int height)
        {
            using var image = ToImage(ConvertColorsToRgba32(pixels), width, height);
            image.SaveAsPng(stream);
        }

        public static void SaveAsJpeg(Stream stream, Color32[] pixels, int width, int height)
        {
            using var image = ToImage(ConvertColorsToRgba32(pixels), width, height);
            image.SaveAsJpeg(stream);
        }

        private static Image<Rgba32> ToImage(Rgba32[] rgba32s, int width, int height)
        {
            // Create ImageSharp image
            var image = new Image<Rgba32>(width, height);

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    image[x, y] = rgba32s[index++];
                }
            }

            return image;
        }

        public static void SaveAs(string fileName, Vector4[] pixels, int width, int height)
        {
            using var source = ToImage(ConvertColorsToRgba32(pixels), width, height);
            SaveAsImpl(fileName, source);
        }

        public static void SaveAs(string fileName, Color32[] pixels, int width, int height)
        {
            using var source = ToImage(ConvertColorsToRgba32(pixels), width, height);
            SaveAsImpl(fileName, source);
        }

        public static void SaveAsDds(string fileName, IReadOnlyList<Color32[]> mipLevels, int width, int height)
        {
            using var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);

            int mipCount = mipLevels.Count;

            writer.Write(Encoding.ASCII.GetBytes("DDS "));

            writer.Write(124);                // dwSize
            writer.Write(0x0002100F);          // dwFlags
            writer.Write(height);             // dwHeight
            writer.Write(width);              // dwWidth
            writer.Write(width * 4);           // dwPitchOrLinearSize
            writer.Write(0);                  // dwDepth
            writer.Write(mipCount);            // dwMipMapCount

            for (int i = 0; i < 11; i++)
                writer.Write(0);

            writer.Write(32);                 // dwSize
            writer.Write(0x41);               // dwFlags (DDPF_RGB | DDPF_ALPHAPIXELS)
            writer.Write(0);                  // dwFourCC
            writer.Write(32);                 // dwRGBBitCount

            writer.Write(0x000000FF);          // R mask
            writer.Write(0x0000FF00);          // G mask
            writer.Write(0x00FF0000);          // B mask
            writer.Write(unchecked((int)0xFF000000)); // A mask

            writer.Write(0x00401008);          // dwCaps (TEXTURE | MIPMAP | COMPLEX)
            writer.Write(0);                  // dwCaps2
            writer.Write(0);                  // dwCaps3
            writer.Write(0);                  // dwCaps4
            writer.Write(0);                  // dwReserved2

            int mipWidth = width;
            int mipHeight = height;

            for (int mip = 0; mip < mipCount; mip++)
            {
                var pixels = mipLevels[mip];

                int expectedPixelCount = mipWidth * mipHeight;
                if (pixels.Length != expectedPixelCount)
                {
                    throw new InvalidOperationException(
                        $"Mip {mip} has {pixels.Length} pixels but expected {expectedPixelCount}.");
                }

                for (int i = 0; i < pixels.Length; i++)
                {
                    var c = pixels[i];
                    writer.Write(c.R);
                    writer.Write(c.G);
                    writer.Write(c.B);
                    writer.Write(c.A);
                }

                mipWidth = Math.Max(1, mipWidth >> 1);
                mipHeight = Math.Max(1, mipHeight >> 1);
            }
        }

        private static void SaveAsImpl(string fileName, Image<Rgba32> source)
        {
            SixLabors.ImageSharp.Formats.IImageEncoder encoder;
            switch (Path.GetExtension(fileName).ToLowerInvariant())
            {
                case ".png":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(PngFormat.Instance);
                    break;
                case ".jpg":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(JpegFormat.Instance);
                    break;
                default:
                    throw new NotSupportedException($"File extension not supported: {fileName}");
            }
            using (var stream = File.Create(fileName))
            {
                source.Save(stream, encoder);
            }

        }
    }

}
