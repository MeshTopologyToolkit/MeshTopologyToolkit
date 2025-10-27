using System.Collections.Generic;
using System.IO;

namespace MeshTopologyToolkit
{
    public class InMemoryFileSystemEntry : IFileSystemEntry
    {
        Dictionary<string, byte[]> _files;
        string _name;

        public InMemoryFileSystemEntry(string name, Dictionary<string, byte[]> files)
        {
            _name = name;
            _files = files;
        }

        public bool Exists => _files.ContainsKey(_name);
        public string Name => _name;

        public IFileSystemEntry GetNeigbourEntry(string fileName)
        {
            return new InMemoryFileSystemEntry(fileName, _files);
        }

        public Stream? OpenRead()
        {
            if (!_files.TryGetValue(_name, out var bytes))
                return null;

            var readStream = new MemoryStream(bytes);
            return readStream;
        }
        public Stream OpenWrite()
        {
            return new EntryMemoryStream(_name, _files);
        }
        public override string ToString()
        {
            return Name ?? base.ToString();
        }

        internal class EntryMemoryStream: MemoryStream
        {
            Dictionary<string, byte[]> _files;
            string _name;

            public EntryMemoryStream(string name, Dictionary<string, byte[]> files)
            {
                _name = name;
                _files = files;
            }

            public override void Close()
            {
                base.Close();
                _files[_name] = this.ToArray();
            }
        }
    }
}
