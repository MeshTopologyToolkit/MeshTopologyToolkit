using System;
using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class ConstMeshVertexAttribute<T> : MeshVertexAttributeBase<T>, IMeshVertexAttribute<T> where T : notnull
    {
        T _value;
        public ConstMeshVertexAttribute(T value, int count = 1)
        {
            _value = value;
            Count = count;
        }

        public override T this[int index] => _value;

        public override int Count { get; }

        public override int Add(T value)
        {
            throw new NotImplementedException("Can't add elements to immutable attribute container");
        }

        /// <inheritdoc/>
        public new int Lerp(int from, int to, float amount)
        {
            return 0;
        }

        public new IMeshVertexAttribute Compact(out IReadOnlyList<int> indexMap)
        {
            indexMap = new ConstMeshVertexAttribute<int>(0, Count);
            var attr = new DictionaryMeshVertexAttribute<T>();
            attr.Add(_value);
            return attr;
        }

        public override IEnumerator<T> GetEnumerator()
        {
            return new MeshVertexAttributeEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
