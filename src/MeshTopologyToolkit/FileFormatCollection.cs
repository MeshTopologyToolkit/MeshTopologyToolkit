using MeshTopologyToolkit.Operators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MeshTopologyToolkit
{
    public class FileFormatCollection : IFileFormat
    {
        IList<FormatAndSpace> _formats;

        public FileFormatCollection(IEnumerable<FormatAndSpace> formats)
        {
            _formats = new List<FormatAndSpace>(formats);
            SupportedExtensions = _formats.SelectMany(_ => _.FileFormat.SupportedExtensions).ToList();
        }

        public FileFormatCollection(IEnumerable<IFileFormat> formats):this(formats.Select(f=>new FormatAndSpace(f, SpaceTransform.Identity)))
        {
        }

        public FileFormatCollection(params FormatAndSpace[] formats) : this((IEnumerable<FormatAndSpace>)formats)
        {
        }

        public FileFormatCollection(params IFileFormat[] formats) : this((IEnumerable<IFileFormat>)formats)
        {
        }
        public IReadOnlyList<SupportedExtension> SupportedExtensions { get; private set; }

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            var ext = Path.GetExtension(entry.Name);
            foreach (var formatAndSpace in _formats)
            {
                var format = formatAndSpace.FileFormat;

                if (format.SupportedExtensions.Any(_ => _.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    if (format.TryRead(entry, out content))
                    {
                        if (!formatAndSpace.Transform.IsIdentity())
                        {
                            content = new SpaceTransformOperator(formatAndSpace.Transform).Transform(content);
                        }
                        return true;
                    }
                }
            }
            content = new FileContainer();
            return false;
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            var ext = Path.GetExtension(entry.Name);
            foreach (var formatAndSpace in _formats)
            {
                var format = formatAndSpace.FileFormat;

                if (format.SupportedExtensions.Any(_ => _.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    SpaceTransform transform = formatAndSpace.Transform;
                    if (transform != SpaceTransform.Identity)
                    {
                        transform = transform.Invert();
                        content = new SpaceTransformOperator(transform).Transform(content);
                    }
                    if (format.TryWrite(entry, content))
                        return true;
                }
            }
            return false;
        }
    }
}
