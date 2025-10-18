using System.Collections;

namespace MeshTopologyToolkit
{
    public class MeshVertexAttributeAdapter<TFrom, TTo>: IMeshVertexAttribute<TTo> where TFrom : notnull where TTo: notnull
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

        public IMeshVertexAttribute Compact(out IReadOnlyList<int> indexMap)
        {
            var values = (IReadOnlyList<TTo>)this;
            var map = new int[values.Count];
            indexMap = map;
            var res = new DictionaryMeshVertexAttribute<TTo>();
            for (int i = 0; i < values.Count; ++i)
            {
                map[i] = res.Add(values[i]);
            }
            return res;
        }

        public IEnumerator<TTo> GetEnumerator()
        {
            return new MeshVertexAttributeEnumerator<TTo>(this);
        }

        public bool TryCast<T>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<T>? attribute) where T : notnull
        {
            return _source.TryCast<T>(converterProvider, out attribute);
        }

        int IMeshVertexAttribute<TTo>.Add(TTo value)
        {
            throw new NotImplementedException("Can't add elements to attribute cast adapter");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
