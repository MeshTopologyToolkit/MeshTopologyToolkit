using System.Collections;

namespace MeshTopologyToolkit
{
    public class MeshVertexAttributeView<T> : MeshVertexAttributeBase<T>, IMeshVertexAttribute<T>
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
