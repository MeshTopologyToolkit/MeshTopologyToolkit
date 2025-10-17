using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector2ToVector4 : IMeshVertexAttributeConverter<Vector2, Vector4>
    {
        public Vector4 Convert(Vector2 value)
        {
            return new Vector4(value.X, value.Y, 0.0f, 0.0f);
        }
    }
}
