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

        public override T this[int index] => _values[index];

        public override int Count => _values.Count;

        public override int Add(T value)
        {
            throw new NotImplementedException("Can't add value to attribute container view");
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
