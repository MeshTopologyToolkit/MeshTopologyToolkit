using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Abstraction for a file format capable of reading and writing mesh-related assets.
    /// Implementations provide the set of supported extensions and methods to attempt reading
    /// or writing a <see cref="FileContainer"/> from/to a given <see cref="IFileSystemEntry"/>.
    /// </summary>
    public interface IFileFormat
    {
        /// <summary>
        /// Gets the list of file extensions (and human-friendly names) this format implementation supports.
        /// The host or registry can use this to select an appropriate <see cref="IFileFormat"/> for a given file name.
        /// </summary>
        IReadOnlyList<SupportedExtension> SupportedExtensions { get; }

        /// <summary>
        /// Attempts to read the provided file system entry and produce a <see cref="FileContainer"/>.
        /// </summary>
        /// <param name="entry">The file system entry to read from. Implementations should handle missing entries gracefully.</param>
        /// <param name="content">
        /// When this method returns, contains the parsed <see cref="FileContainer"/> if the read succeeded;
        /// otherwise empty <see cref="FileContainer"/>.
        /// </param>
        /// <returns><c>true</c> if the file was successfully read and <paramref name="content"/> contains meaningful data; otherwise <c>false</c>.</returns>
        bool TryRead(IFileSystemEntry entry, out FileContainer content);

        /// <summary>
        /// Attempts to write the provided <see cref="FileContainer"/> to the specified file system entry.
        /// </summary>
        /// <param name="entry">Target file system entry to write into. Implementations may create or overwrite the entry.</param>
        /// <param name="content">The content to serialize and write.</param>
        /// <returns><c>true</c> if the content was successfully written; otherwise <c>false</c>.</returns>
        bool TryWrite(IFileSystemEntry entry, FileContainer content);
    }
}
