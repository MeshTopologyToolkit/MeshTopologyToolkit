using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Color32ToVector4 : IMeshVertexAttributeConverter<Color32, Vector4>
    {
        public Vector4 Convert(Color32 value)
        {
            return value;
        }
    }
}
