using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public interface IImageFormat
    {
        /// <summary>
        /// Gets the list of file extensions (and human-friendly names) this format implementation supports.
        /// The host or registry can use this to select an appropriate <see cref="IImageFormat"/> for a given file name.
        /// </summary>
        IReadOnlyList<SupportedExtension> SupportedExtensions { get; }

        public bool TryRead(IFileSystemEntry entry, out ImageContainer image);
    }
}
