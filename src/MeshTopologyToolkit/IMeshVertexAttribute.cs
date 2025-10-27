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

        /// <summary>
        /// Converts attribute to <see cref="RTree*MeshVertexAttribute<T>"/> or <see cref="DictionaryMeshVertexAttribute<T>"/> and provides mapping from original indices to new indices.
        /// </summary>
        /// <param name="weldRadius">Weld radius for values. Only used if RTree is created.</param>
        /// <param name="indexMap">Map from original indices to new indices.</param>
        /// <returns>New instance of <see cref="RTree*MeshVertexAttribute<T>"/> or <see cref="DictionaryMeshVertexAttribute<T>"/></returns>
        IMeshVertexAttribute Compact(float weldRadius, out IReadOnlyList<int> indexMap);


        /// <summary>
        /// Create new vertex attribute container by remapping values according to the given index map.
        /// </summary>
        /// <param name="indexMap">Index map.</param>
        /// <returns>Remapped attribute container.</returns>
        IMeshVertexAttribute Remap(IReadOnlyList<int> indexMap);

        /// <summary>
        /// Performs a linear interpolation between two values based on the given weighting.
        /// </summary>
        /// <param name="from">The first value index.</param>
        /// <param name="to">The second value index.</param>
        /// <param name="amount">A value between 0 and 1 that indicates the weight of "to".</param>
        /// <returns>The interpolated value index.</returns>
        int Lerp(int from, int to, float amount);
    }

    public interface IMeshVertexAttribute<T>: IMeshVertexAttribute, IReadOnlyList<T> where T : notnull
    {
        int Add(T value);
    }
}
