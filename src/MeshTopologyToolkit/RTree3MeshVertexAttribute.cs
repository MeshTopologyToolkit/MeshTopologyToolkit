using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// A vertex attribute implementation that stores 3D positions and uses an <see cref="RTree3"/>
    /// spatial index to quickly find nearby vertices for welding (duplicate elimination).
    /// </summary>
    /// <remarks>
    /// - When a value is added via <see cref="Add"/>, a small axis-aligned bounding box around the
    ///   value is queried in the internal <see cref="RTree3"/>. If any existing stored value lies within
    ///   the configured weld radius, that existing index is returned instead of inserting a duplicate.
    /// - The class is not thread-safe and is intended for single-threaded use during mesh construction.
    /// </remarks>
    public class RTree3MeshVertexAttribute : MeshVertexAttributeBase<Vector3>, IMeshVertexAttribute<Vector3>
    {
        /// <summary>
        /// Backing list of stored attribute values.
        /// </summary>
        private List<Vector3> _values = new List<Vector3>();

        /// <summary>
        /// Spatial index used to find candidate nearby values efficiently.
        /// </summary>
        private RTree3 _rtree = new RTree3();

        /// <summary>
        /// Half-extent used to create a small AABB around newly inserted/query points.
        /// Computed from the provided weld radius (half of the weld radius).
        /// </summary>
        private float _boundingBoxExtend;

        /// <summary>
        /// Square of the configured weld radius. Used to compare squared distances to avoid sqrt.
        /// </summary>
        private float _weldRadiusSquared;

        /// <summary>
        /// Gets the stored value at the specified index.
        /// </summary>
        /// <param name="index">Index of the value to retrieve.</param>
        public override Vector3 this[int index] => _values[index];

        /// <summary>
        /// Gets the number of stored values.
        /// </summary>
        public override int Count => _values.Count;

        /// <summary>
        /// Creates a new <see cref="RTree3MeshVertexAttribute"/>.
        /// </summary>
        /// <param name="weldRadius">
        /// Distance threshold used to decide whether a new value is considered
        /// equal to an existing one (and therefore "welded" to it). Defaults to 1e-3.
        /// </param>
        /// <remarks>
        /// The constructor derives an axis-aligned bounding-box half-extent used for the R-tree query
        /// as <c>weldRadius * 0.5f</c> and stores <c>weldRadius * weldRadius</c> for squared-distance comparisons.
        /// </remarks>
        public RTree3MeshVertexAttribute(float weldRadius = 1e-3f)
        {
            _boundingBoxExtend = weldRadius * 0.5f;
            _weldRadiusSquared = weldRadius * weldRadius;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the stored attribute values.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{Vector3}"/> for the values.</returns>
        public override IEnumerator<Vector3> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        /// <summary>
        /// Adds a value to the attribute. If an existing stored value lies within the configured
        /// weld radius of <paramref name="value"/>, the existing index is returned and no new value
        /// is stored (welding). Otherwise the value is appended and its index is returned.
        /// </summary>
        /// <param name="value">The 3D position to add.</param>
        /// <returns>
        /// The index of an existing (welded) value or the index of the newly added value.
        /// </returns>
        /// <remarks>
        /// - Uses the internal <see cref="RTree3"/> to find candidate nearby points by querying
        ///   a small AABB centered on <paramref name="value"/> with half-extent <see cref="_boundingBoxExtend"/>.
        /// - Distance comparisons are performed using squared distances against <see cref="_weldRadiusSquared"/>
        ///   to avoid unnecessary square-root operations.
        /// - After inserting a new value the method updates both the R-tree (inserting the new box)
        ///   and the backing list.
        /// </remarks>
        public override int Add(Vector3 value)
        {
            var valueBbox = new BoundingBox3(value, _boundingBoxExtend);
            foreach (var index in _rtree.Query(valueBbox))
            {
                if (Vector3.DistanceSquared(_values[index], value) <= _weldRadiusSquared)
                    return index;
            }

            var rTreeIndex = _rtree.Insert(valueBbox);
#if DEBUG
            if (rTreeIndex != _values.Count)
                throw new System.InvalidOperationException("RTree index does not match values count.");
#endif
            _values.Add(value);

            return rTreeIndex;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal void AddRange(IEnumerable<Vector3> values)
        {
            foreach (var val in values)
            {
                Add(val);
            }
        }
    }
}
