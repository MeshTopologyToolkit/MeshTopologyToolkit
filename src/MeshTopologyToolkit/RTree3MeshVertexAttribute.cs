using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class RTree3MeshVertexAttribute : MeshVertexAttributeBase<Vector3>, IMeshVertexAttribute<Vector3>
    {
        private List<Vector3> _values = new List<Vector3>();
        private RTree3 _rtree = new RTree3();
        private float _weldRadius;
        private float _weldRadiusSquared;

        public Vector3 this[int index] => _values[index];

        public int Count => _values.Count;

        public RTree3MeshVertexAttribute(float weldRadius = 1e-6f)
        {
            _weldRadius = weldRadius;
            _weldRadiusSquared = weldRadius * weldRadius;
        }

        public IEnumerator<Vector3> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public int Add(Vector3 value)
        {
            var valueBbox = new BoundingBox3(value,_weldRadius);
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
    }
}
