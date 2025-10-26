using System.Numerics;

namespace MeshTopologyToolkit.MathHelpers
{
    public class Vector3MathHelper : IMathHelper<Vector3>
    {
        public Vector3 Lerp(Vector3 from, Vector3 to, float amount)
        {
            return Vector3.Lerp(from, to, amount);
        }
    }
}
