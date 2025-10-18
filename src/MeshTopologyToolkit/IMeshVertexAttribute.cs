using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public interface IMeshVertexAttribute
    {
        bool TryCast<T>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<T>? attribute) where T : notnull;

        /// <summary>
        /// Converts attribute to <see cref="DictionaryMeshVertexAttribute<T>"/> and provides mapping from original indices to new indices.
        /// </summary>
        /// <param name="indexMap">Map from original indices to new indices.</param>
        /// <returns>New instance of <see cref="DictionaryMeshVertexAttribute<T>"/></returns>
        IMeshVertexAttribute Compact(out IReadOnlyList<int> indexMap);
    }

    public interface IMeshVertexAttribute<T>: IMeshVertexAttribute, IReadOnlyList<T> where T : notnull
    {
        int Add(T value);
    }
}
