namespace MeshTopologyToolkit
{
    /// <summary>
    /// A value at a moment in time. Tangents are only meaningful for <see cref="AnimationInterpolation.CubicSpline"/> curves
    /// and follow the glTF convention (they are scaled by the interval between keys when evaluated).
    /// </summary>
    public struct AnimationKey<T> where T : struct
    {
        public AnimationKey(float time, T value)
        {
            Time = time;
            Value = value;
            InTangent = default;
            OutTangent = default;
        }

        public AnimationKey(float time, T value, T inTangent, T outTangent)
        {
            Time = time;
            Value = value;
            InTangent = inTangent;
            OutTangent = outTangent;
        }

        public float Time { get; set; }
        public T Value { get; set; }
        public T InTangent { get; set; }
        public T OutTangent { get; set; }
    }
}
