using System;
using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class MeshVertexAttributeView<T> : MeshVertexAttributeBase<T>, IMeshVertexAttribute<T> where T : notnull
    {
        IReadOnlyList<T> _values;
        public MeshVertexAttributeView(IReadOnlyList<T> values)
        {
            _values = values;
        }

        public T this[int index] => _values[index];

        public int Count => _values.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        int IMeshVertexAttribute<T>.Add(T value)
        {
            throw new NotImplementedException("Can't add value to attribute container view");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
