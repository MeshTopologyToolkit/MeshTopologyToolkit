using System.Reflection;

namespace MeshTopologyToolkit
{
    public class StreamFileSystemEntry : IFileSystemEntry
    {
        Func<Stream?> _openStream;
        public StreamFileSystemEntry(Func<Stream?> openStream)
        {
            _openStream = openStream;
        }

        public bool Exists
        {
            get
            {
                using (var stream = _openStream())
                {
                    return stream != null;
                }
            }
        }

        public static StreamFileSystemEntry FromEmbeddedResource(string resourceName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetManifestResourceNames().Contains(resourceName))
                {
                    return new StreamFileSystemEntry(() => assembly.GetManifestResourceStream(resourceName));
                }
            }
            throw new FileNotFoundException($"Embedded resource {resourceName} not found");
        }

        public static StreamFileSystemEntry FromEmbeddedResource(Assembly assembly, string resourceName)
        {
            var names = assembly.GetManifestResourceNames();
            if (!names.Contains(resourceName))
            {
                if (names.Length == 0)
                    throw new FileNotFoundException($"Embedded resource {resourceName} not found in {assembly}");
                throw new FileNotFoundException($"Embedded resource {resourceName} not found in {assembly}, some candidates are " + string.Join(", ", names.Take(3)));
            }
            return new StreamFileSystemEntry(() => assembly.GetManifestResourceStream(resourceName));
        }

        public IFileSystemEntry GetNeigbourEntry(string fileName)
        {
            throw new NotImplementedException();
        }

        public Stream OpenRead()
        {
            var result = _openStream();
            if (result == null || !result.CanRead)
            {
                throw new FileNotFoundException("Can't open stream to read");
            }
            return result;
        }

        public Stream OpenWrite()
        {
            var result = _openStream();
            if (result == null || !result.CanWrite)
            {
                throw new FileNotFoundException("Can't open stream to write");
            }
            return result;
        }
    }

}
