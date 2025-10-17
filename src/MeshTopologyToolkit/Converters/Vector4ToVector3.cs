using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector4ToVector3 : IMeshVertexAttributeConverter<Vector4, Vector3>
    {
        public Vector3 Convert(Vector4 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }
    }
}
