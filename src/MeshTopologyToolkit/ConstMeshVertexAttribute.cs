using System.Collections;

namespace MeshTopologyToolkit
{
    public class ConstMeshVertexAttribute<T> : IMeshVertexAttribute<T>
    {
        T _value;
        public ConstMeshVertexAttribute(T value, int count = 1)
        {
            _value = value;
            Count = count;
        }

        public T this[int index] => _value;

        public int Count { get; set; }

        public IEnumerator<T> GetEnumerator()
        {
            return new MeshVertexAttributeEnumerator<T>(this);
        }

        public bool TryCast<TTo>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<TTo>? attribute)
        {
            if (typeof(T) == typeof(TTo))
            {
                attribute = (IMeshVertexAttribute<TTo>)this;
                return true;
            }

            if (converterProvider.TryGetConverter<T, TTo>(out var converter))
            {
                attribute = new MeshVertexAttributeAdapter<T, TTo>(this, converter);
                return true;
            }

            attribute = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
