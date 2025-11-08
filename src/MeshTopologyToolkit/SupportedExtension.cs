namespace MeshTopologyToolkit
{
    public struct SupportedExtension
    {
        public SupportedExtension(string name, string extension)
        {
            Name = name;
            Extension = extension;
        }
        public string Name { get; }

        public string Extension { get; }
    }

}
