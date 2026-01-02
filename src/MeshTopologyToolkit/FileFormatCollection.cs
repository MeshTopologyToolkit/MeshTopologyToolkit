using MeshTopologyToolkit.Operators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MeshTopologyToolkit
{
    public class FileFormatCollection : IFileFormat
    {
        private List<IFileFormat> _formats;

        private bool _forceGltfSpace = false;

        public FileFormatCollection(IEnumerable<IFileFormat> formats): this(false, formats)
        {
        }

        public FileFormatCollection(bool forceGltfSpace, IEnumerable < IFileFormat> formats)
        {
            _forceGltfSpace = forceGltfSpace;
            _formats = new List<IFileFormat>(formats);
            SupportedExtensions = _formats.SelectMany(_ => _.SupportedExtensions).ToList();
        }

        public FileFormatCollection(params IFileFormat[] formats) : this(false, (IEnumerable<IFileFormat>)formats)
        {
        }

        public FileFormatCollection(bool forceGltfSpace, params IFileFormat[] formats) : this(forceGltfSpace, (IEnumerable<IFileFormat>)formats)
        {
        }

        public IReadOnlyList<SupportedExtension> SupportedExtensions { get; private set; }

        public bool ForceGltfSpace => _forceGltfSpace;

        public SpaceTransform FormatToGltfTransform { get
            {
                if (_forceGltfSpace)
                    return SpaceTransform.Identity;
                throw new NotSupportedException($"{nameof(FileFormatCollection)} does not have a single {nameof(FormatToGltfTransform)} when forceGltfSpace is false.");
            } }

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            var ext = Path.GetExtension(entry.Name);
            foreach (var format in _formats)
            {
                if (format.SupportedExtensions.Any(_ => _.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    if (format.TryRead(entry, out content))
                    {
                        if (_forceGltfSpace && content.FileToGltfTransform != null && !content.FileToGltfTransform.IsIdentity())
                        {
                            content = new SpaceTransformOperator(content.FileToGltfTransform).Transform(content);
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
            foreach (var format in _formats)
            {
                if (format.SupportedExtensions.Any(_ => _.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    FileContainer contentCopy = content;
                    SpaceTransform transform = format.FormatToGltfTransform;
                    if (_forceGltfSpace && !transform.IsIdentity())
                    {
                        transform = transform.Invert();
                        contentCopy = new SpaceTransformOperator(transform).Transform(contentCopy);
                    }
                    if (format.TryWrite(entry, contentCopy))
                        return true;
                }
            }
            return false;
        }
    }
}
