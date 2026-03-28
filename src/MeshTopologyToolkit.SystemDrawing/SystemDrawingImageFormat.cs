using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace MeshTopologyToolkit
{
    public class SystemDrawingImageFormat : IImageFormat
    {
        private readonly static IReadOnlyList<SupportedExtension> _supportedExtensions = new[] {
            new SupportedExtension("Portable Network Graphics", ".png"),
            new SupportedExtension("JPEG", ".jpg"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _supportedExtensions;

        public bool TryRead(IFileSystemEntry entry, out ImageContainer image)
        {
            System.IO.Stream? stream = entry.OpenRead();
            if (stream == null)
            {
                throw new FileNotFoundException($"Can't open image from {entry?.Name}.");
            }
            var bmp = Image.FromStream(stream) as Bitmap;
            if (bmp == null)
            {
                throw new FileNotFoundException($"Can't read image from {entry?.Name}.");
            }

            image = new ImageContainer(new LRDImageMipMap(bmp.Width, bmp.Height, 1, GetPixels(bmp)));

            return true;
        }

        private IReadOnlyList<Color32> GetPixels(Bitmap bmp)
        {
            var buf = new Color32[bmp.Width * bmp.Height];

            for (int y=0; y<bmp.Height; y++)
            {
                for (int x = 0; x<bmp.Width; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    buf[y * bmp.Width + x] = new Color32 { R = c.R, G = c.G, B = c.B, A = c.A };
                }
            }

            return buf;
        }
    }
}
