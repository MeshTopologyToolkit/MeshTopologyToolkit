using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector3ToVector4 : IMeshVertexAttributeConverter<Vector3, Vector4>
    {
        public Vector4 Convert(Vector3 value)
        {
            return new Vector4(value.X, value.Y, value.Z, 0.0f);
        }
    }
}
