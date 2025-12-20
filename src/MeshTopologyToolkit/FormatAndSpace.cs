namespace MeshTopologyToolkit
{
    public struct FormatAndSpace
    {
        public IFileFormat FileFormat { get; }

        public SpaceTransform Transform { get; }

        public FormatAndSpace(IFileFormat fileFormat, SpaceTransform transform)
        {
            FileFormat = fileFormat;
            Transform = transform;
        }

        public FormatAndSpace(IFileFormat fileFormat)
        {
            FileFormat = fileFormat;
            Transform = SpaceTransform.Identity;
        }
    }
}
