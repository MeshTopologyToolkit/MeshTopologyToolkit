using System.Collections;

namespace MeshTopologyToolkit
{
    public class MeshVertexAttributeAdapter<TFrom, TTo>: IMeshVertexAttribute<TTo>
    {
        IMeshVertexAttribute<TFrom> _source;
        IMeshVertexAttributeConverter<TFrom, TTo> _converter;
        public MeshVertexAttributeAdapter(IMeshVertexAttribute<TFrom> source, IMeshVertexAttributeConverter<TFrom, TTo> converter)
        {
            _source = source;
            _converter = converter;
        }

        public TTo this[int index] => _converter.Convert(_source[index]);

        public int Count => _source.Count;

        public IEnumerator<TTo> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool TryCast<T>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<T>? attribute)
        {
            return _source.TryCast<T>(converterProvider, out attribute);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
