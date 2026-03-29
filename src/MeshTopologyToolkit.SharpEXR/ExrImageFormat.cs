using SharpEXR;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class ExrImageFormat : IImageFormat
    {
        readonly static IReadOnlyList<SupportedExtension> _supportedExtensions = new[] { new SupportedExtension("OpenEXR Image", ".exr")  };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _supportedExtensions;

        public bool TryRead(IFileSystemEntry entry, out ImageContainer image)
        {
            using var stream = entry.OpenRead();

            if (stream == null)
            {
                throw new Exception($"Failed to open stream for entry: {entry?.Name}");
            }

            var exrFile = EXRFile.FromStream(stream);

            // EXR files can have multiple "parts" (layers). We'll take the first one.
            var part = exrFile.Parts[0];

            int width = part.DataWindow.Width;
            int height = part.DataWindow.Height;
            int pixelCount = width * height;

            // Initialize the result array
            Vector4[] result = new Vector4[pixelCount];

            stream.Dispose();

            using var partStream = entry.OpenRead();

            part.Open(partStream);

            float[] rChannel = part.GetFloats(ChannelConfiguration.RGB, true, GammaEncoding.Linear);

            for (int i = 0; i < pixelCount; i++)
            {
                result[i] = new Vector4(
                    rChannel[i*3 + 0],
                    rChannel[i * 3 + 1],
                    rChannel[i * 3 + 2],
                    1.0f
                );
            }

            image = new ImageContainer(new HDRImageMipMap(width, height, 1, result));

            return true;
        }

        public bool TryWrite(IFileSystemEntry entry, ImageContainer content)
        {
            return false;
        }
    }
}
