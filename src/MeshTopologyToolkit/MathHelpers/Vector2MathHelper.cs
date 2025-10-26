using System.Numerics;

namespace MeshTopologyToolkit.MathHelpers
{
    public class Vector2MathHelper : IMathHelper<Vector2>
    {
        public Vector2 Lerp(Vector2 from, Vector2 to, float amount)
        {
            return Vector2.Lerp(from, to, amount);
        }
    }
}
