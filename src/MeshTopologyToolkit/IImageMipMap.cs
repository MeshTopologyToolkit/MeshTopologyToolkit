using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public interface IImageMipMap
    {
        int Width { get; }
        int Height { get; }
        int Depth { get; }

        IReadOnlyList<Vector4> GetHdrPixels();

        IReadOnlyList<Color32> GetPixels();
    }
}
