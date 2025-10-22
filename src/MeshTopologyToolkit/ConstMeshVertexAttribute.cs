using System;
using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class ConstMeshVertexAttribute<T> : MeshVertexAttributeBase<T>, IMeshVertexAttribute<T> where T: notnull
    {
        T _value;
        public ConstMeshVertexAttribute(T value, int count = 1)
        {
            _value = value;
            Count = count;
        }

        public T this[int index] => _value;

        public int Count { get; set; }

        public new IMeshVertexAttribute Compact(out IReadOnlyList<int> indexMap)
        {
            indexMap = new ConstMeshVertexAttribute<int>(0, Count);
            var attr = new DictionaryMeshVertexAttribute<T>();
            attr.Add(_value);
            return attr;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MeshVertexAttributeEnumerator<T>(this);
        }

        int IMeshVertexAttribute<T>.Add(T value)
        {
            throw new NotImplementedException("Can't add elements to immutable attribute container");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
