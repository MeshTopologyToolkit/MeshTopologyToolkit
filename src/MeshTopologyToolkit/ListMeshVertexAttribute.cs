using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class ListMeshVertexAttribute<T> : List<T>, IMeshVertexAttribute<T> where T : notnull
    {
        public ListMeshVertexAttribute()
        {
        }

        public ListMeshVertexAttribute(int capacity) : base(capacity)
        {
        }

        public Type GetElementType()
        {
            return typeof(T);
        }

        public bool TryCast<TTo>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<TTo> attribute) where TTo : notnull
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

            attribute = EmptyMeshAttribute<TTo>.Instance;
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


        public IMeshVertexAttribute Compact(float weldRadius, out IReadOnlyList<int> indexMap)
        {
            if (weldRadius <= 0.0f)
                return Compact(out indexMap);

            var map = new int[Count];
            indexMap = map;

            IMeshVertexAttribute<T> res;
            if (typeof(T) == typeof(Vector2))
            {
                res = (IMeshVertexAttribute<T>)(new RTree2MeshVertexAttribute(weldRadius));
            }
            else if (typeof(T) == typeof(Vector3))
            {
                res = (IMeshVertexAttribute<T>)(new RTree3MeshVertexAttribute(weldRadius));
            }
            else
            {
                res = new DictionaryMeshVertexAttribute<T>();
            }

            for (int i = 0; i < Count; ++i)
            {
                map[i] = res.Add(this[i]);
            }
            return res;
        }

        /// <inheritdoc/>
        public IMeshVertexAttribute Remap(IReadOnlyList<int> indexMap)
        {
            var result = new ListMeshVertexAttribute<T>();
            foreach (var index in indexMap)
            {
                result.Add(this[index]);
            }
            return result;
        }


        int IMeshVertexAttribute<T>.Add(T value)
        {
            var index = Count;
            Add(value);
            return index;
        }

        /// <inheritdoc/>
        public int Lerp(int from, int to, float amount)
        {
            return ((IMeshVertexAttribute<T>)this).Add(MathHelper<T>.Default.Lerp(this[from], this[to], amount));
        }
    }
}
