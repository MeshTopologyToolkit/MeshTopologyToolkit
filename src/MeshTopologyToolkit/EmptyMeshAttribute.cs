using System;
using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Empty mesh attribute. This only exists for to avoid null reference exceptions in the code.
    /// </summary>
    public class EmptyMeshAttribute : IMeshVertexAttribute
    {
        public static readonly EmptyMeshAttribute Instance = new EmptyMeshAttribute();

        public virtual Type GetElementType()
        {
            return typeof(void);
        }

        /// <summary>
        /// Get number of vertices in attribute.
        /// </summary>
        public virtual int Count => 0;

        public IMeshVertexAttribute Compact(out IReadOnlyList<int> indexMap)
        {
            indexMap = Array.Empty<int>();
            return this;
        }

        public IMeshVertexAttribute Compact(float weldRadius, out IReadOnlyList<int> indexMap)
        {
            indexMap = Array.Empty<int>();
            return this;
        }

        public int Lerp(int from, int to, float amount)
        {
            ThrowError();
            return 0;
        }

        private static void ThrowError()
        {
            throw new InvalidOperationException("Mesh attribute doesn't exist. Check result of TryGetAttribute method and assume attribute is missing if it returns false.");
        }

        public IMeshVertexAttribute Remap(IReadOnlyList<int> indexMap)
        {
            return this;
        }

        public bool TryCast<T>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<T> attribute) where T : notnull
        {
            throw new InvalidOperationException("Mesh attribute doesn't exist. Check result of TryGetAttribute method and assume attribute is missing if it returns false.");
        }
    }

    /// <summary>
    /// Empty mesh attribute. This only exists for to avoid null reference exceptions in the code.
    /// </summary>
    public class EmptyMeshAttribute<T> : EmptyMeshAttribute, IMeshVertexAttribute<T> where T : notnull
    {
        public new static readonly EmptyMeshAttribute<T> Instance = new EmptyMeshAttribute<T>();

        public T this[int index] => throw new InvalidOperationException("Mesh attribute doesn't exist. Check result of TryGetAttribute method and assume attribute is missing if it returns false.");

        public override Type GetElementType()
        {
            return typeof(T);
        }

        public int Add(T value)
        {
            throw new InvalidOperationException("Mesh attribute doesn't exist. Check result of TryGetAttribute method and assume attribute is missing if it returns false.");
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
