using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MeshTopologyToolkit
{
    public class StridedIndexContainer: IReadOnlyList<StridedIndexRange>
    {
        private List<MeshAttributeKey> _attributeKeys;
        private int _stide;
        private List<int> _indices;

        public StridedIndexContainer(IEnumerable<MeshAttributeKey> attributeKeys)
        {
            _attributeKeys = new List<MeshAttributeKey>();
            _attributeKeys.AddRange(attributeKeys);
            _indices = new List<int>();
            _stide = _attributeKeys.Count;
        }

        public StridedIndexContainer(IMesh mesh)
        {
            _attributeKeys = mesh.GetAttributeKeys().ToList();
            _stide = _attributeKeys.Count;
            if (_stide == 0)
            {
                _indices = new List<int>();
                return;
            }

            var numIndices = mesh.GetAttributeIndices(_attributeKeys.First()).Count;
            _indices = new List<int>(numIndices * _stide);

            var attrIndices = _attributeKeys.Select(key => mesh.GetAttributeIndices(key)).ToList();
            for (int vertexIndex=0; vertexIndex< numIndices; ++vertexIndex)
            {
                for (int attributeIndex=0; attributeIndex<_stide; ++attributeIndex)
                {
                    _indices.Add(attrIndices[attributeIndex][vertexIndex]);
                }
            }
        }

        public IReadOnlyList<MeshAttributeKey> Keys => _attributeKeys;

        public IReadOnlyList<int> Indices => _indices;

        public StridedIndexRange this[int vertexIndex]
        {
            get
            {
                return new StridedIndexRange(_indices, vertexIndex * _stide, _stide);
            }
        }

        public int Count => (_stide > 0) ? _indices.Count / _stide : 0;

        public void Add(StridedIndexRange indices)
        {
            if (indices.Count != _stide)
                throw new ArgumentException($"Number of indices {indices.Count} doesn't match number of attributes {_stide}");
            _indices.AddRange(indices);
        }

        public void Add(IReadOnlyList<int> indices)
        {
            if (indices.Count != _stide)
                throw new ArgumentException($"Number of indices {indices.Count} doesn't match number of attributes {_stide}");
            _indices.AddRange(indices);
        }

        public IEnumerator<StridedIndexRange> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
