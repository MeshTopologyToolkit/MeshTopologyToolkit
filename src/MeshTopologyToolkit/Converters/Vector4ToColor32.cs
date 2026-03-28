using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector4ToColor32 : IMeshVertexAttributeConverter<Vector4, Color32>
    {
        public Color32 Convert(Vector4 value)
        {
            return new Color32(value);
        }
    }
}
