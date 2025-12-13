using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class ReadOnlyListMeshVertexAttribute<T> : MeshVertexAttributeBase<T>, IMeshVertexAttribute<T> where T : notnull
    {
        private IReadOnlyList<T> _values;

        public ReadOnlyListMeshVertexAttribute(IReadOnlyList<T> values)
        {
            _values = values;
        }

        public override T this[int index] => _values[index];

        public override int Count => _values.Count;

        public override int Add(T value)
        {
            throw new System.InvalidOperationException("The mesh attribute is read-only");
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
