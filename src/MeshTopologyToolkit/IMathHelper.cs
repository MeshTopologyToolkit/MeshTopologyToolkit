namespace MeshTopologyToolkit
{
    public interface IMathHelper<T>
    {
        /// <summary>
        /// Performs a linear interpolation between two values based on the given weighting.
        /// </summary>
        /// <param name="from">The first value.</param>
        /// <param name="to">The second value.</param>
        /// <param name="amount">A value between 0 and 1 that indicates the weight of "to".</param>
        /// <returns>The interpolated value.</returns>
        T Lerp(T from, T to, float amount);
    }
}
