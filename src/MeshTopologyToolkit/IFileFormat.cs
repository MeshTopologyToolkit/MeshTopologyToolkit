using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public interface IFileFormat
    {
        IReadOnlyList<SupportedExtension> SupportedExtensions { get; }

        bool TryRead(IFileSystemEntry entry, out FileContainer content);

        bool TryWrite(IFileSystemEntry entry, FileContainer content);
    }
}
