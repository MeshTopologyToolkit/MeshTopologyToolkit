using StbImageSharp;
using StbImageWriteSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Reads and writes common image formats using the public domain StbImageSharp and StbImageWriteSharp libraries.
    /// </summary>
    /// <remarks>
    /// PNG, JPEG, BMP and TGA can be read and written. GIF can only be read.
    /// </remarks>
    public class StbImageFormat : IImageFormat
    {
        private const int JpegQuality = 90;

        private static readonly SupportedExtension[] _supportedExtensions = new[]
        {
            new SupportedExtension("Portable Network Graphics", ".png"),
            new SupportedExtension("JPEG", ".jpg"),
            new SupportedExtension("JPEG", ".jpeg"),
            new SupportedExtension("JPEG", ".jfif"),
            new SupportedExtension("Bitmap", ".bmp"),
            new SupportedExtension("Graphics Interchange Format (read only)", ".gif"),
            new SupportedExtension("Truevision TGA (TARGA)", ".tga"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _supportedExtensions;

        public bool TryRead(IFileSystemEntry entry, out ImageContainer image)
        {
            using var stream = entry.OpenRead();
            if (stream == null)
            {
                throw new Exception($"Failed to open stream for entry: {entry?.Name}");
            }

            var result = ImageResult.FromStream(stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
            var buf = new Color32[result.Width * result.Height];
            var data = result.Data;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = new Color32(data[i * 4], data[i * 4 + 1], data[i * 4 + 2], data[i * 4 + 3]);
            }

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
            var extension = Path.GetExtension(entry.Name).ToLowerInvariant();
            switch (extension)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".jfif":
                case ".bmp":
                case ".tga":
                    break;
                default:
                    return false;
            }

            var rgba = ToRgbaBytes(mip.GetPixels());
            using (var stream = entry.OpenWrite())
            {
                Write(stream, extension, rgba, mip.Width, mip.Height);
            }
            return true;
        }

        private static void Write(Stream stream, string extension, byte[] rgba, int width, int height)
        {
            var writer = new ImageWriter();
            switch (extension)
            {
                case ".png":
                    writer.WritePng(rgba, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
                    break;
                case ".jpg":
                case ".jpeg":
                case ".jfif":
                    writer.WriteJpg(rgba, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream, JpegQuality);
                    break;
                case ".bmp":
                    writer.WriteBmp(rgba, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
                    break;
                case ".tga":
                    writer.WriteTga(rgba, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
                    break;
                default:
                    throw new NotSupportedException($"Writing {extension} files is not supported.");
            }
        }

        public static void SaveAsPng(Stream stream, Vector4[] pixels, int width, int height)
        {
            Write(stream, ".png", ToRgbaBytes(pixels), width, height);
        }

        public static void SaveAsPng(Stream stream, Color32[] pixels, int width, int height)
        {
            Write(stream, ".png", ToRgbaBytes(pixels), width, height);
        }

        public static void SaveAsJpeg(Stream stream, Color32[] pixels, int width, int height)
        {
            Write(stream, ".jpg", ToRgbaBytes(pixels), width, height);
        }

        public static void SaveAs(string fileName, Vector4[] pixels, int width, int height)
        {
            if (!new StbImageFormat().TryWrite(new FileSystemEntry(fileName), new ImageContainer(new HDRImageMipMap(width, height, 1, pixels))))
            {
                throw new Exception($"Failed to write image to file: {fileName}");
            }
        }

        public static void SaveAs(string fileName, Color32[] pixels, int width, int height)
        {
            if (!new StbImageFormat().TryWrite(new FileSystemEntry(fileName), new ImageContainer(new LDRImageMipMap(width, height, 1, pixels))))
            {
                throw new Exception($"Failed to write image to file: {fileName}");
            }
        }

        private static byte[] ToRgbaBytes(IReadOnlyList<Color32> pixels)
        {
            var bytes = new byte[pixels.Count * 4];
            for (int i = 0; i < pixels.Count; i++)
            {
                var c = pixels[i];
                bytes[i * 4] = c.R;
                bytes[i * 4 + 1] = c.G;
                bytes[i * 4 + 2] = c.B;
                bytes[i * 4 + 3] = c.A;
            }
            return bytes;
        }

        private static byte[] ToRgbaBytes(IReadOnlyList<Vector4> pixels)
        {
            var bytes = new byte[pixels.Count * 4];
            for (int i = 0; i < pixels.Count; i++)
            {
                Color32 c = pixels[i];
                bytes[i * 4] = c.R;
                bytes[i * 4 + 1] = c.G;
                bytes[i * 4 + 2] = c.B;
                bytes[i * 4 + 3] = c.A;
            }
            return bytes;
        }
    }
}
