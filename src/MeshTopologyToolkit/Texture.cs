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

        /// <summary>How the image repeats along U (the sampler's wrap S in glTF).</summary>
        public TextureWrap WrapU { get; set; } = TextureWrap.Repeat;

        /// <summary>How the image repeats along V (the sampler's wrap T in glTF).</summary>
        public TextureWrap WrapV { get; set; } = TextureWrap.Repeat;
    }
}
