using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class DictionaryMeshVertexAttribute<T> : MeshVertexAttributeBase<T>, IMeshVertexAttribute<T> where T: notnull
    {
        private List<T> _values = new List<T>();
        private Dictionary<T, int> _map = new Dictionary<T, int>();

        public T this[int index] => _values[index];

        public int Count => _values.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public int Add(T value)
        {
            if (_map.TryGetValue(value, out var index))
                return index;
            index = _values.Count;
            _values.Add(value);
            _map.Add(value, index);
            return index;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
