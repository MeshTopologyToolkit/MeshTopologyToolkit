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

        Stream OpenRead();

        Stream OpenWrite();
    }
}
