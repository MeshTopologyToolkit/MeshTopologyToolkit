namespace MeshTopologyToolkit
{
    public interface IFileSystemEntry
    {
        bool Exists { get; }

        IFileSystemEntry GetNeigbourEntry(string fileName);

        Stream OpenRead();

        Stream OpenWrite();
    }
}
