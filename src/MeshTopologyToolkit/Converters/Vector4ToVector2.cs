using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector4ToVector2 : IMeshVertexAttributeConverter<Vector4, Vector2>
    {
        public Vector2 Convert(Vector4 value)
        {
            return new Vector2(value.X, value.Y);
        }
    }
}
