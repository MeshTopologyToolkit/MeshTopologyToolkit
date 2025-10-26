using System.Numerics;

namespace MeshTopologyToolkit.MathHelpers
{
    public class Vector4MathHelper : IMathHelper<Vector4>
    {
        /// <summary>
        /// Performs a linear interpolation between two values based on the given weighting.
        /// </summary>
        /// <param name="from">The first value.</param>
        /// <param name="to">The second value.</param>
        /// <param name="amount">A value between 0 and 1 that indicates the weight of "to".</param>
        /// <returns>The interpolated value.</returns>
        public Vector4 Lerp(Vector4 from, Vector4 to, float amount)
        {
            return Vector4.Lerp(from, to, amount);
        }
    }
}
