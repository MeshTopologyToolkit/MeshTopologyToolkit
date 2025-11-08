using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// A vertex attribute implementation that stores 3D tangents with binormal handedness
    /// and uses an <see cref="RTree3"/> spatial index to quickly find nearby vertices for 
    /// welding (duplicate elimination).
    /// </summary>
    /// <remarks>
    /// - When a value is added via <see cref="Add"/>, a small axis-aligned bounding box around the
    ///   value is queried in the internal <see cref="RTree3"/>. If any existing stored value lies within
    ///   the configured weld radius, that existing index is returned instead of inserting a duplicate.
    /// - The class is not thread-safe and is intended for single-threaded use during mesh construction.
    /// </remarks>
    public class TangentRTree3MeshVertexAttribute : MeshVertexAttributeBase<Vector4>, IMeshVertexAttribute<Vector4>
    {
        struct ValuePair
        {
            public ValuePair(Vector3 tangent)
            {
                Tangent = tangent;
                Positive = -1;
                Negative = -1;
            }

            public Vector3 Tangent;
            public int Positive;
            public int Negative;
        }

        /// <summary>
        /// Backing list of stored attribute values.
        /// </summary>
        private List<Vector4> _values = new List<Vector4>();

        /// <summary>
        /// Backing list of stored attribute value pair indices for binormal handedness.
        /// </summary>
        private List<ValuePair> _valuePairs = new List<ValuePair>();

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
        public override Vector4 this[int index] => _values[index];

        /// <summary>
        /// Gets the number of stored values.
        /// </summary>
        public override int Count => _values.Count;

        /// <summary>
        /// Creates a new <see cref="TangentRTree3MeshVertexAttribute"/>.
        /// </summary>
        /// <param name="weldRadius">
        /// Distance threshold used to decide whether a new value is considered
        /// equal to an existing one (and therefore "welded" to it). Defaults to 1e-3.
        /// </param>
        /// <remarks>
        /// The constructor derives an axis-aligned bounding-box half-extent used for the R-tree query
        /// as <c>weldRadius * 0.5f</c> and stores <c>weldRadius * weldRadius</c> for squared-distance comparisons.
        /// </remarks>
        public TangentRTree3MeshVertexAttribute(float weldRadius = 1e-3f)
        {
            _boundingBoxExtend = weldRadius * 0.5f;
            _weldRadiusSquared = weldRadius * weldRadius;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the stored attribute values.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{Vector3}"/> for the values.</returns>
        public override IEnumerator<Vector4> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        /// <summary>
        /// Adds a value to the attribute. If an existing stored value lies within the configured
        /// weld radius of <paramref name="value"/>, the existing index is returned and no new value
        /// is stored (welding). Otherwise the value is appended and its index is returned.
        /// </summary>
        /// <param name="value">The 3D tangent to add.</param>
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
        public override int Add(Vector4 value)
        {
            var tangent = new Vector3(value.X, value.Y, value.Z);
            var valueBbox = new BoundingBox3(tangent, _boundingBoxExtend);
            foreach (var index in _rtree.Query(valueBbox))
            {
                if (Vector3.DistanceSquared(_valuePairs[index].Tangent, tangent) <= _weldRadiusSquared)
                {
                    return GetOrAddIndex(index, value);
                }
            }

            var rTreeIndex = _rtree.Insert(valueBbox);
#if DEBUG
            if (rTreeIndex != _valuePairs.Count)
                throw new System.InvalidOperationException("RTree index does not match values count.");
#endif
            _valuePairs.Add(new ValuePair(tangent));

            return GetOrAddIndex(rTreeIndex, value);
        }

        private int GetOrAddIndex(int index, Vector4 value)
        {
            if (value.W >= 0.0f)
            {
                var pair = _valuePairs[index];
                if (pair.Positive < 0)
                {
                    var resIndex = _values.Count;
                    _values.Add(value);
                    pair = new ValuePair(pair.Tangent) { Positive = resIndex, Negative = pair.Negative };
                    _valuePairs[index] = pair;
                    return resIndex;
                }
                return pair.Positive;
            }
            else
            {
                var pair = _valuePairs[index];
                if (pair.Negative < 0)
                {
                    var resIndex = _values.Count;
                    _values.Add(value);
                    pair = new ValuePair(pair.Tangent) { Negative = resIndex, Positive = pair.Positive };
                    _valuePairs[index] = pair;
                    return resIndex;
                }
                return pair.Negative;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
