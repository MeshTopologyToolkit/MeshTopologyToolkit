using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public abstract class MeshVertexAttributeBase<T>: IMeshVertexAttribute<T> where T : notnull
    {
        public abstract int Count { get; }

        public abstract T this[int index] { get; }

        public bool TryCast<TTo>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<TTo>? attribute) where TTo:notnull
        {
            if (typeof(T) == typeof(TTo))
            {
                attribute = (IMeshVertexAttribute<TTo>)this;
                return true;
            }

            if (converterProvider.TryGetConverter<T, TTo>(out var converter) && converter != null)
            {
                attribute = new MeshVertexAttributeAdapter<T, TTo>((IMeshVertexAttribute<T>)this, converter);
                return true;
            }

            attribute = null;
            return false;
        }

        public IMeshVertexAttribute<T> Compact(out IReadOnlyList<int> indexMap)
        {
            var map = new int[Count];
            indexMap = map;
            var res = new DictionaryMeshVertexAttribute<T>();
            for (int i = 0; i < Count; ++i)
            {
                map[i] = res.Add(this[i]);
            }
            return res;
        }

        public IMeshVertexAttribute<T> Compact(float weldRadius, out IReadOnlyList<int> indexMap)
        {
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
        public int Lerp(int from, int to, float amount)
        {
            return ((IMeshVertexAttribute<T>)this).Add(MathHelper<T>.Default.Lerp(this[from], this[to], amount));
        }

        public abstract int Add(T value);

        IMeshVertexAttribute IMeshVertexAttribute.Compact(out IReadOnlyList<int> indexMap)
        {
            return Compact(out indexMap);
        }

        IMeshVertexAttribute IMeshVertexAttribute.Compact(float weldRadius, out IReadOnlyList<int> indexMap)
        {
            return Compact(weldRadius, out indexMap);
        }

        public abstract IEnumerator<T> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
