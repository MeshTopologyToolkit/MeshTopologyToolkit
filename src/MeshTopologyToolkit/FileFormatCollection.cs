using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MeshTopologyToolkit
{
    public class FileFormatCollection : IFileFormat
    {
        IList<IFileFormat> _formats;

        public FileFormatCollection(IEnumerable<IFileFormat> formats)
        {
            _formats = new List<IFileFormat>(formats);
            SupportedExtensions = _formats.SelectMany(_ => _.SupportedExtensions).ToList();
        }

        public FileFormatCollection(params IFileFormat[] formats):this((IEnumerable<IFileFormat>)formats)
        {
        }

        public IReadOnlyList<SupportedExtension> SupportedExtensions { get; private set; }

        public bool TryRead(IFileSystemEntry entry, out FileContainer? content)
        {
            var ext = Path.GetExtension(entry.Name);
            foreach (var format in _formats)
            {
                if (format.SupportedExtensions.Any(_=>_.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    if (format.TryRead(entry, out content))
                        return true;
                }
            }
            content = null;
            return false;
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            var ext = Path.GetExtension(entry.Name);
            foreach (var format in _formats)
            {
                if (format.SupportedExtensions.Any(_ => _.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    if (format.TryWrite(entry, content))
                        return true;
                }
            }
            return false;
        }
    }
}
