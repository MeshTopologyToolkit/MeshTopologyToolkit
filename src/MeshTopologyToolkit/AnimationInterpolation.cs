namespace MeshTopologyToolkit
{
    public enum AnimationInterpolation
    {
        /// <summary>The value is held until the next key.</summary>
        Step,
        /// <summary>Linear interpolation (spherical for rotations).</summary>
        Linear,
        /// <summary>Cubic Hermite spline that uses key tangents.</summary>
        CubicSpline,
    }
}
