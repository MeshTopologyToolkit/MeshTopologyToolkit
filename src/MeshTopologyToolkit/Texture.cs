namespace MeshTopologyToolkit
{
    public class Texture
    {
        public Texture()
        {
        }

        public Texture(IFileSystemEntry fileSystemEntry)
        {
            FileSystemEntry = fileSystemEntry;
        }

        public Texture(string name, byte[] bytes)
        {
            FileSystemEntry = new InMemoryFileSystemEntry(name, bytes);
        }

        public IFileSystemEntry? FileSystemEntry { get; set; }
    }
}
