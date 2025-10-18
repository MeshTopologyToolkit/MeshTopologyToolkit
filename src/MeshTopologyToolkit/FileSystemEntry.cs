namespace MeshTopologyToolkit
{
    public class FileSystemEntry: IFileSystemEntry
    {
        FileInfo _fileInfo;
        public FileSystemEntry(FileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public FileSystemEntry(string path)
        {
            _fileInfo = new FileInfo(path);
        }

        public bool Exists => _fileInfo.Exists;

        public IFileSystemEntry GetNeigbourEntry(string fileName)
        {
            var dir = Path.GetDirectoryName(_fileInfo.FullName);
            if (dir == null) {
                return new FileSystemEntry(new FileInfo(fileName));
            }
            return new FileSystemEntry(new FileInfo(Path.Combine(dir, fileName)));
        }

        public Stream OpenRead()
        {
            return _fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream OpenWrite()
        {
            return _fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        }
    }
}
