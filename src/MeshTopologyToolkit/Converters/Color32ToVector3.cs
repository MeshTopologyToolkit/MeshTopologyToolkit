using System.Numerics;

namespace MeshTopologyToolkit.Converters
{
    public class Color32ToVector3 : IMeshVertexAttributeConverter<Color32, Vector3>
    {
        public Vector3 Convert(Color32 value)
        {
            return value;
        }
    }
}
