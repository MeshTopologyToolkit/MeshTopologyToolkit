using System;

namespace MeshTopologyToolkit
{
    public struct SupportedExtension
    {
        public SupportedExtension(string name, string extension)
        {
            Name = name;
            Extension = extension;
            if (!extension.StartsWith("."))
            {
                throw new ArgumentException($"Extension must start with a dot: {extension}");
            }
        }
        public string Name { get; }

        public string Extension { get; }
    }

}
