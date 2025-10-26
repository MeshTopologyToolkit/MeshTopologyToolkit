using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

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
            var map = new int[Count];
            indexMap = map;
            var res = new DictionaryMeshVertexAttribute<TTo>();
            for (int i = 0; i < Count; ++i)
            {
                map[i] = res.Add(this[i]);
            }
            return res;
        }

        public IMeshVertexAttribute Compact(float weldRadius, out IReadOnlyList<int> indexMap)
        {
            var map = new int[Count];
            indexMap = map;

            IMeshVertexAttribute<TTo> res;
            if (typeof(TTo) == typeof(Vector2))
            {
                res = (IMeshVertexAttribute<TTo>)(new RTree2MeshVertexAttribute(weldRadius));
            }
            else if (typeof(TTo) == typeof(Vector3))
            {
                res = (IMeshVertexAttribute<TTo>)(new RTree3MeshVertexAttribute(weldRadius));
            }
            else
            {
                res = new DictionaryMeshVertexAttribute<TTo>();
            }

            for (int i = 0; i < Count; ++i)
            {
                map[i] = res.Add(this[i]);
            }
            return res;
        }

        /// <inheritdoc/>
        public int Lerp(int from, int to, float amount)
        {
            return _source.Lerp(from, to, amount);
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
