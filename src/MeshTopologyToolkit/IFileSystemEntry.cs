using System.IO;

namespace MeshTopologyToolkit
{
    public interface IFileSystemEntry
    {
        bool Exists { get; }

        /// <summary>
        /// File name of equivalent.
        /// </summary>
        string Name { get; }

        IFileSystemEntry GetNeigbourEntry(string fileName);

        /// <summary>
        /// Open file system entry to read.
        /// </summary>
        /// <returns>File entry stream or null if entry doesn't exist.</returns>
        Stream? OpenRead();

        Stream OpenWrite();
    }
}
