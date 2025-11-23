namespace MeshTopologyToolkit.TrimGenerator
{
    public class TrimGenerationArguments
    {
        public IReadOnlyList<int> TrimHeights { get; }

        public int WidthInPixels { get; }

        public float WidthInUnits { get; }

        public float UVOffsetInPixels { get; }

        public TrimGenerationArguments(int[] trimHeight, int width = 1024, float widthInUnits = 5.0f, float uvOffsetInPixels = 0.5f)
        {
            TrimHeights = trimHeight;
            WidthInPixels = width;
            WidthInUnits = widthInUnits;
            UVOffsetInPixels = uvOffsetInPixels;
        }
    }
}