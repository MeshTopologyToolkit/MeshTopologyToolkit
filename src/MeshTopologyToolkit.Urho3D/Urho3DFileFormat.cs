namespace MeshTopologyToolkit.Urho3D
{
    public class Urho3DFileFormat : IFileFormat
    {
        public bool TryRead(IFileSystemEntry entry, out FileContainer? content)
        {
            content = null;
            if (!entry.Exists)
                return false;

            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;

                content = new FileContainer();
                return true;
            }
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            return false;
        }
    }
}
