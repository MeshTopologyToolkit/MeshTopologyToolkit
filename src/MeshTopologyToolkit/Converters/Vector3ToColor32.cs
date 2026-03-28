using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Vector3ToColor32 : IMeshVertexAttributeConverter<Vector3, Color32>
    {
        public Color32 Convert(Vector3 value)
        {
            return new Color32(value);
        }
    }
}
