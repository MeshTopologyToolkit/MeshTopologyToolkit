namespace MeshTopologyToolkit.TrimGenerator
{
    public class TrimGenerationArguments
    {
        public IReadOnlyList<int> TrimHeights { get; }

        public int WidthInPixels { get; }

        public int HeightInPixels { get; }

        public int BevelInPixels { get; }

        public float WidthInUnits { get; }

        public float UVOffsetInPixels { get; }

        public TrimGenerationArguments(int[] trimHeight, int width = 1024, int bevelInPixels = 8, float widthInUnits = 5.0f, float uvOffsetInPixels = 0.5f)
        {
            TrimHeights = trimHeight;
            WidthInPixels = width;
            BevelInPixels = bevelInPixels;
            HeightInPixels = trimHeight.Sum().NextPowerOfTwo();
            WidthInUnits = widthInUnits;
            UVOffsetInPixels = uvOffsetInPixels;
        }
    }
}