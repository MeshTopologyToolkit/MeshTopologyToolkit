using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class ListMeshVertexAttribute<T> : List<T>, IMeshVertexAttribute<T> where T: notnull
    {
        public bool TryCast<TTo>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<TTo>? attribute) where TTo: notnull
        {
            if (typeof(T) == typeof(TTo))
            {
                attribute = (IMeshVertexAttribute<TTo>)this;
                return true;
            }

            if (converterProvider.TryGetConverter<T, TTo>(out var converter) && converter != null)
            {
                attribute = new MeshVertexAttributeAdapter<T, TTo>(this, converter);
                return true;
            }

            attribute = null;
            return false;
        }

        public IMeshVertexAttribute Compact(out IReadOnlyList<int> indexMap)
        {
            var values = (IReadOnlyList<T>)this;
            var map = new int[values.Count];
            indexMap = map;
            var res = new DictionaryMeshVertexAttribute<T>();
            for (int i = 0; i < values.Count; ++i)
            {
                map[i] = res.Add(values[i]);
            }
            return res;
        }

        int IMeshVertexAttribute<T>.Add(T value)
        {
            var index = Count;
            Add(value);
            return index;
        }
    }
}
