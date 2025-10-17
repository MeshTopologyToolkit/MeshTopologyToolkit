using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector2ToVector3 : IMeshVertexAttributeConverter<Vector2, Vector3>
    {
        public Vector3 Convert(Vector2 value)
        {
            return new Vector3(value.X, value.Y, 0.0f);
        }
    }
}
