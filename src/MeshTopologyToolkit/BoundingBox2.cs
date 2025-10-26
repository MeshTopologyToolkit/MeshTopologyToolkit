using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Represents an axis-aligned bounding box (AABB) in 2D space.
    /// The box is defined by its minimum and maximum corner points.
    /// </summary>
    public struct BoundingBox2
    {
        /// <summary>
        /// A bounding box at the origin with zero size (min == max == Vector2.Zero).
        /// </summary>
        public static readonly BoundingBox2 Zero = new BoundingBox2(Vector2.Zero, Vector2.Zero);

        /// <summary>
        /// An empty bounding box. Uses extreme values so that merging with any valid box
        /// produces that valid box.
        /// </summary>
        public static readonly BoundingBox2 Empty = new BoundingBox2(new Vector2(float.MaxValue), new Vector2(float.MinValue));

        private Vector2 _min;
        private Vector2 _max;

        /// <summary>
        /// Initializes a new instance of <see cref="BoundingBox2"/> with the specified minimum and maximum corners.
        /// </summary>
        /// <param name="min">The minimum corner (smallest X, Y, Z).</param>
        /// <param name="max">The maximum corner (largest X, Y, Z).</param>
        public BoundingBox2(Vector2 min, Vector2 max)
        {
            _min = min;
            _max = max;
        }

        /// <summary>
        /// Initializes a new cubic bounding box centered at <paramref name="center"/> with the given half extent.
        /// </summary>
        /// <param name="center">Center of the bounding box.</param>
        /// <param name="halfExtent">Half the size of the box along each axis.</param>
        public BoundingBox2(Vector2 center, float halfExtent)
        {
            _min = center - new Vector2(halfExtent);
            _max = center + new Vector2(halfExtent);
        }

        /// <summary>
        /// Gets a value indicating whether this bounding box is empty.
        /// A box is considered empty when any minimum component is greater than the corresponding maximum component.
        /// </summary>
        public bool IsEmpty => _min.X > _max.X || _min.Y > _max.Y;

        /// <summary>
        /// Gets the minimum corner of the bounding box (smallest X, Y).
        /// </summary>
        public Vector2 Min => _min;

        /// <summary>
        /// Gets the maximum corner of the bounding box (largest X, Y).
        /// </summary>
        public Vector2 Max => _max;

        /// <summary>
        /// Returns a new bounding box that contains both this box and <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other bounding box to merge with.</param>
        /// <returns>A bounding box that encloses both boxes.</returns>
        public BoundingBox2 Merge(BoundingBox2 other)
        {
            return new BoundingBox2(Vector2.Min(_min, other._min), Vector2.Max(_max, other._max));
        }

        /// <summary>
        /// Returns a new bounding box that contains both this box and the specified point.
        /// </summary>
        /// <param name="other">The point to include in the resulting box.</param>
        /// <returns>A bounding box that encloses this box and <paramref name="other"/>.</returns>
        public BoundingBox2 Merge(Vector2 other)
        {
            return new BoundingBox2(Vector2.Min(_min, other), Vector2.Max(_max, other));
        }

        /// <summary>
        /// Determines whether this bounding box intersects <paramref name="other"/>.
        /// Touching faces or edges are considered intersecting.
        /// </summary>
        /// <param name="other">The other bounding box to test against.</param>
        /// <returns><c>true</c> if the boxes overlap or touch; otherwise <c>false</c>.</returns>
        public bool Intersects(BoundingBox2 other)
        {
            return (_min.X <= other._max.X && _max.X >= other._min.X) &&
                   (_min.Y <= other._max.Y && _max.Y >= other._min.Y);
        }

        /// <summary>
        /// Determines whether the specified point is contained within this bounding box.
        /// Points on the boundary are considered contained.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns><c>true</c> if <paramref name="point"/> lies inside or on the bounds of the box; otherwise <c>false</c>.</returns>
        public bool Contains(Vector2 point)
        {
            return (point.X >= _min.X && point.X <= _max.X) &&
                   (point.Y >= _min.Y && point.Y <= _max.Y);
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
        public Vector2 Size()
        {
            return _max - _min;
        }

        /// <summary>
        /// Computes the area of the bounding box.
        /// Degenerate or negative extents are clamped to zero before multiplication.
        /// </summary>
        /// <returns>The non-negative area of the box.</returns>
        public float Area()
        {
            var d = Vector2.Max(Vector2.Zero, Size());
            return d.X * d.Y;
        }
    }
}
