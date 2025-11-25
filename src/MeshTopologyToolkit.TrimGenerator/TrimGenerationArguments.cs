using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class TrimGenerationArguments
    {
        public IReadOnlyList<int> TrimHeights { get; }

        public IReadOnlyList<TrimRecepie> TrimRecepies { get; }

        public int WidthInPixels { get; }

        public int HeightInPixels { get; }

        public int BevelInPixels { get; }

        public float WidthInUnits { get; }

        public float UVOffsetInPixels { get; }

        public TrimGenerationArguments(int[] trimHeights, int width = 1024, int bevelInPixels = 8, float widthInUnits = 5.0f, float uvOffsetInPixels = 0.5f)
        {
            TrimHeights = trimHeights;
            WidthInPixels = width;
            BevelInPixels = bevelInPixels;
            HeightInPixels = trimHeights.Sum().NextPowerOfTwo();
            WidthInUnits = widthInUnits;
            UVOffsetInPixels = uvOffsetInPixels;

            var uvOffsetInPixels2 = uvOffsetInPixels * 2.0f;
            var widthInTexels = width - uvOffsetInPixels2;
            var texelSize = (float)widthInUnits / widthInTexels;
            var uvScale = new Vector2(1.0f / width, 1.0f / HeightInPixels);

            var y = 0.0f;
            var trimRecepies = new List<TrimRecepie>();
            foreach (var trimHeight in trimHeights)
            {
                var uvSize = new Vector2(width - uvOffsetInPixels2, trimHeight - uvOffsetInPixels2) * uvScale;
                var uvPos = new Vector2(uvOffsetInPixels, y + uvOffsetInPixels) * uvScale;
                var sizeInUnits = new Vector2(widthInTexels, trimHeight - uvOffsetInPixels2) * texelSize;
                trimRecepies.Add(new TrimRecepie { TexCoord = uvPos, TexCoordSize = uvSize, SizeInUnits = sizeInUnits });
                y += trimHeight;
            }
            TrimRecepies = trimRecepies;
        }

        internal TrimRecepie FindMatchingRecepie(float height)
        {
            var recepie = TrimRecepies[0];
            var bestDiviation = float.MaxValue;
            foreach (var trimRecepie in TrimRecepies)
            {
                var div = trimRecepie.SizeInUnits.Y / height;
                if (div < 1) div = 1.0f / div;
                if (div < bestDiviation)
                {
                    bestDiviation = div;
                    recepie = trimRecepie;
                }
            }
            return recepie;
        }
    }
}