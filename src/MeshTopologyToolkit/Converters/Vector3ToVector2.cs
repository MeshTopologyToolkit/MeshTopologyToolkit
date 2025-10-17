using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector3ToVector2 : IMeshVertexAttributeConverter<Vector3, Vector2>
    {
        public Vector2 Convert(Vector3 value)
        {
            return new Vector2(value.X, value.Y);
        }
    }
}
