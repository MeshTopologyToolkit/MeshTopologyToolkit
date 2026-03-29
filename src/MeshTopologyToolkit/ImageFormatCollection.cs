using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MeshTopologyToolkit
{
    public class ImageFormatCollection : IImageFormat
    {
        private List<IImageFormat> _formats;

        private bool _forceGltfSpace = false;

        public ImageFormatCollection(IEnumerable<IImageFormat> formats)
        {
            _formats = new List<IImageFormat>(formats);
            SupportedExtensions = _formats.SelectMany(_ => _.SupportedExtensions).ToList();
        }

        public ImageFormatCollection(params IImageFormat[] formats) : this((IEnumerable<IImageFormat>)formats)
        {
        }

        public IReadOnlyList<SupportedExtension> SupportedExtensions { get; private set; }

        public bool TryRead(IFileSystemEntry entry, out ImageContainer content)
        {
            var ext = Path.GetExtension(entry.Name);
            foreach (var format in _formats)
            {
                if (format.SupportedExtensions.Any(_ => _.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    if (format.TryRead(entry, out content))
                    {
                        return true;
                    }
                }
            }
            content = new ImageContainer();
            return false;
        }

        public bool TryWrite(IFileSystemEntry entry, ImageContainer content)
        {
            var ext = Path.GetExtension(entry.Name);
            foreach (var format in _formats)
            {
                if (format.SupportedExtensions.Any(_ => _.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    && format.TryWrite(entry, content))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
