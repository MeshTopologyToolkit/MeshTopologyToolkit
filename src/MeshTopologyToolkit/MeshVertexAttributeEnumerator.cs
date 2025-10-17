using System.Collections;

namespace MeshTopologyToolkit
{
    internal class MeshVertexAttributeEnumerator<T>: IEnumerator<T>
    {
        IMeshVertexAttribute<T> _values;
        int _index;
        public MeshVertexAttributeEnumerator(IMeshVertexAttribute<T> values)
        {
            _values = values;
            Reset();
        }

        public T Current => _values[_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_index + 1 < _values.Count)
            {
                ++_index;
                return true;
            }
            return false;

        }

        public void Reset()
        {
            _index = -1;
        }
    }
}
