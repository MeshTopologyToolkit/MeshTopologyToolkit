using System;
using System.Collections.Generic;

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

            image = new ImageContainer(new LRDImageMipMap(result.Width, result.Height, 1, buf));
            
            return true;
        }
    }
}
