using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class ImageSharpImageFormat : IImageFormat
    {
        private static readonly SupportedExtension[] _supportedExtensions = new[]
        {
            new SupportedExtension("Portable Network Graphics", ".png"),
            new SupportedExtension("JPEG", ".jpg"),
            new SupportedExtension("JPEG", ".jpeg"),
            new SupportedExtension("JPEG", ".jfif"),
            new SupportedExtension("Bitmap", ".bmp"),
            new SupportedExtension("Graphics Interchange Format", ".gif"),
            new SupportedExtension("Tagged File Format", ".tiff"),
            new SupportedExtension("WebP", ".webp"),
            new SupportedExtension("Truevision TGA (TARGA)", ".tga"),
            new SupportedExtension("Portable BitMap", ".pbm"),
            new SupportedExtension("Quite OK Image Format", ".qoi"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _supportedExtensions;

        public bool TryRead(IFileSystemEntry entry, out ImageContainer image)
        {
            using var stream = entry.OpenRead();
            if (stream == null)
            {
                throw new Exception($"Failed to open stream for entry: {entry?.Name}");
            }
            var result = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(stream);
            var buf = new Color32[result.Width * result.Height];
            result.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<SixLabors.ImageSharp.PixelFormats.Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        ref SixLabors.ImageSharp.PixelFormats.Rgba32 pixel = ref row[x];
                        buf[x + y*result.Width] = new Color32(pixel.R, pixel.G, pixel.B, pixel.A);
                    }
                }
            });

            image = new ImageContainer(new LDRImageMipMap(result.Width, result.Height, 1, buf));
            
            return true;
        }

        public bool TryWrite(IFileSystemEntry entry, ImageContainer content)
        {
            if (content == null || content.Count == 0)
            {
                return false;
            }

            var mip = content[0];
            using var source = ToImage(ConvertColorsToRgba32(mip.GetPixels()), mip.Width, mip.Height);

            SixLabors.ImageSharp.Formats.IImageEncoder encoder;
            switch (Path.GetExtension(entry.Name).ToLowerInvariant())
            {
                case ".png":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(PngFormat.Instance);
                    break;
                case ".jpg":
                case ".jpeg":
                case ".jfif":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(JpegFormat.Instance);
                    break;
                case ".bmp":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(BmpFormat.Instance);
                    break;
                case ".gif":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(GifFormat.Instance);
                    break;
                case ".tiff":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(TiffFormat.Instance);
                    break;
                case ".webp":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(WebpFormat.Instance);
                    break;
                case ".tga":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(TgaFormat.Instance);
                    break;
                case ".pbm":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(PbmFormat.Instance);
                    break;
                case ".qoi":
                    encoder = source.Configuration.ImageFormatsManager.GetEncoder(QoiFormat.Instance);
                    break;
                default:
                    return false;
            }
            using (var stream = entry.OpenWrite())
            {
                source.Save(stream, encoder);
            }

            return true;
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


        public static void SaveAs(string fileName, Vector4[] pixels, int width, int height)
        {
            if (!new ImageSharpImageFormat().TryWrite(new FileSystemEntry(fileName), new ImageContainer(new HDRImageMipMap(width, height, 1, pixels))))
            {
                throw new Exception($"Failed to write image to file: {fileName}");
            }
        }

        public static void SaveAs(string fileName, Color32[] pixels, int width, int height)
        {
            if (!new ImageSharpImageFormat().TryWrite(new FileSystemEntry(fileName), new ImageContainer(new LDRImageMipMap(width, height, 1, pixels))))
            {
                throw new Exception($"Failed to write image to file: {fileName}");
            }
        }

        private static Rgba32[] ConvertColorsToRgba32(IReadOnlyList<Color32> pixels)
        {
            Rgba32[] rgbaPixels = new Rgba32[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
            {
                Color32 v = pixels[i];
                rgbaPixels[i] = new Rgba32(
                    v.R,   // R
                    v.G,   // G
                    v.B,   // B
                    v.A    // A
                );
            }
            return rgbaPixels;
        }

        private static Rgba32[] ConvertColorsToRgba32(IReadOnlyList<Vector4> pixels)
        {
            Rgba32[] rgbaPixels = new Rgba32[pixels.Count];
            for (int i = 0; i < pixels.Count; i++)
            {
                Color32 v = pixels[i];
                rgbaPixels[i] = new Rgba32(
                    v.R,   // R
                    v.G,   // G
                    v.B,   // B
                    v.A    // A
                );
            }
            return rgbaPixels;
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
    }
}
