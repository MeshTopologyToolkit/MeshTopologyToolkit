namespace MeshTopologyToolkit.Bsp
{
    public class BspFileFormat : IFileFormat
    {
        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Quake BSP", ".bsp"),
        };

        // TODO: Implement proper space transform.
        public SpaceTransform FormatToGltfTransform => SpaceTransform.Identity;

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            content = new FileContainer();
            return false;
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            return false;
        }
    }
}
