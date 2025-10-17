namespace MeshTopologyToolkit
{
    public interface IFileFormat
    {
        bool TryRead(IFileSystemEntry entry, out FileContainer? content);

        bool TryWrite(IFileSystemEntry entry, FileContainer content);
    }
}
