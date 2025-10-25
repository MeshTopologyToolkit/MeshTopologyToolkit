using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Represents an axis-aligned bounding box (AABB) in 3D space.
    /// The box is defined by its minimum and maximum corner points.
    /// </summary>
    public struct BoundingBox3
    {
        /// <summary>
        /// A bounding box at the origin with zero size (min == max == Vector3.Zero).
        /// </summary>
        public static readonly BoundingBox3 Zero = new BoundingBox3(Vector3.Zero, Vector3.Zero);

        /// <summary>
        /// An empty bounding box. Uses extreme values so that merging with any valid box
        /// produces that valid box.
        /// </summary>
        public static readonly BoundingBox3 Empty = new BoundingBox3(new Vector3(float.MaxValue), new Vector3(float.MinValue));

        private Vector3 _min;
        private Vector3 _max;

        /// <summary>
        /// Initializes a new instance of <see cref="BoundingBox3"/> with the specified minimum and maximum corners.
        /// </summary>
        /// <param name="min">The minimum corner (smallest X, Y, Z).</param>
        /// <param name="max">The maximum corner (largest X, Y, Z).</param>
        public BoundingBox3(Vector3 min, Vector3 max)
        {
            _min = min;
            _max = max;
        }

        /// <summary>
        /// Initializes a new cubic bounding box centered at <paramref name="center"/> with the given half extent.
        /// </summary>
        /// <param name="center">Center of the bounding box.</param>
        /// <param name="halfExtent">Half the size of the box along each axis.</param>
        public BoundingBox3(Vector3 center, float halfExtent)
        {
            _min = center - new Vector3(halfExtent);
            _max = center + new Vector3(halfExtent);
        }

        /// <summary>
        /// Gets a value indicating whether this bounding box is empty.
        /// A box is considered empty when any minimum component is greater than the corresponding maximum component.
        /// </summary>
        public bool IsEmpty => _min.X > _max.X || _min.Y > _max.Y || _min.Z > _max.Z;

        /// <summary>
        /// Gets the minimum corner of the bounding box (smallest X, Y, Z).
        /// </summary>
        public Vector3 Min => _min;

        /// <summary>
        /// Gets the maximum corner of the bounding box (largest X, Y, Z).
        /// </summary>
        public Vector3 Max => _max;

        /// <summary>
        /// Returns a new bounding box that contains both this box and <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other bounding box to merge with.</param>
        /// <returns>A bounding box that encloses both boxes.</returns>
        public BoundingBox3 Merge(BoundingBox3 other)
        {
            return new BoundingBox3(Vector3.Min(_min, other._min), Vector3.Max(_max, other._max));
        }

        /// <summary>
        /// Returns a new bounding box that contains both this box and the specified point.
        /// </summary>
        /// <param name="other">The point to include in the resulting box.</param>
        /// <returns>A bounding box that encloses this box and <paramref name="other"/>.</returns>
        public BoundingBox3 Merge(Vector3 other)
        {
            return new BoundingBox3(Vector3.Min(_min, other), Vector3.Max(_max, other));
        }

        /// <summary>
        /// Determines whether this bounding box intersects <paramref name="other"/>.
        /// Touching faces or edges are considered intersecting.
        /// </summary>
        /// <param name="other">The other bounding box to test against.</param>
        /// <returns><c>true</c> if the boxes overlap or touch; otherwise <c>false</c>.</returns>
        public bool Intersects(BoundingBox3 other)
        {
            return (_min.X <= other._max.X && _max.X >= other._min.X) &&
                   (_min.Y <= other._max.Y && _max.Y >= other._min.Y) &&
                   (_min.Z <= other._max.Z && _max.Z >= other._min.Z);
        }

        /// <summary>
        /// Determines whether the specified point is contained within this bounding box.
        /// Points on the boundary are considered contained.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns><c>true</c> if <paramref name="point"/> lies inside or on the bounds of the box; otherwise <c>false</c>.</returns>
        public bool Contains(Vector3 point)
        {
            return (point.X >= _min.X && point.X <= _max.X) &&
                   (point.Y >= _min.Y && point.Y <= _max.Y) &&
                   (point.Z >= _min.Z && point.Z <= _max.Z);
        }

        /// <summary>
        /// Returns a string representation of the bounding box in the form "(Min, Max)".
        /// </summary>
        /// <returns>A string containing the min and max corners.</returns>
        public override string ToString()
        {
            return $"({_min}, {_max})";
        }

        /// <summary>
        /// Returns the size (extent) of the bounding box: Max - Min.
        /// For an empty box this may contain negative components.
        /// </summary>
        /// <returns>The size vector of the box.</returns>
        public Vector3 Size()
        {
            return _max - _min;
        }

        /// <summary>
        /// Computes the volume of the bounding box.
        /// Degenerate or negative extents are clamped to zero before multiplication.
        /// </summary>
        /// <returns>The non-negative volume of the box.</returns>
        public float Volume()
        {
            var d = Vector3.Max(Vector3.Zero, Size());
            return d.X * d.Y * d.Z;
        }
    }
}
