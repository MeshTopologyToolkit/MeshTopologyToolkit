using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Represents a mesh with vertex attributes and draw calls (submeshes, geometries).
    /// Provides a uniform API to query attributes (typed and untyped), retrieve attribute index mappings,
    /// and convert between unified and separated indexed representations.
    /// </summary>
    public interface IMesh
    {
        /// <summary>
        /// The list of draw calls that describe how the mesh should be rendered.
        /// Modifying this collection affects how the mesh will be interpreted by renderers and exporters.
        /// </summary>
        IList<MeshDrawCall> DrawCalls { get; }

        /// <summary>
        /// Optional name for the mesh (for diagnostics, exporters, or scene organization).
        /// </summary>
        string? Name { get; set; }

        /// <summary>
        /// Returns <c>true</c> when the mesh contains an attribute identified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The attribute key to query.</param>
        /// <returns><c>true</c> if the attribute exists; otherwise <c>false</c>.</returns>
        bool HasAttribute(MeshAttributeKey key);

        /// <summary>
        /// Tries to get an attribute by <paramref name="key"/> as the untyped <see cref="IMeshVertexAttribute"/>.
        /// </summary>
        /// <param name="key">The attribute key to look up.</param>
        /// <param name="attribute">
        /// When this method returns, contains the attribute instance if found; otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if the attribute was found; otherwise <c>false</c>.</returns>
        bool TryGetAttribute(MeshAttributeKey key, out IMeshVertexAttribute attribute);

        /// <summary>
        /// Tries to get an attribute by <paramref name="key"/> cast to the typed
        /// <see cref="IMeshVertexAttribute{T}"/> for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the attribute. Must be non-nullable.</typeparam>
        /// <param name="key">The attribute key to look up.</param>
        /// <param name="attribute">
        /// When this method returns, contains the typed attribute instance if found and cast succeeded; otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if the typed attribute was found and cast; otherwise <c>false</c>.</returns>
        bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T> attribute) where T : notnull;

        /// <summary>
        /// Tries to get the index mapping associated with an attribute.
        /// For separated indexed meshes this returns the per-attribute index list; for unified meshes this returns the shared index list.
        /// </summary>
        /// <param name="key">The attribute key whose indices to retrieve.</param>
        /// <param name="indices">
        /// When this method returns, contains the index list for the attribute if available; otherwise <c>null</c>.
        /// </param>
        /// <returns><c>true</c> if an index list exists for the attribute; otherwise <c>false</c>.</returns>
        bool TryGetAttributeIndices(MeshAttributeKey key, out IReadOnlyList<int> indices);

        /// <summary>
        /// Produces a unified indexed view of the mesh where a single index buffer indexes all attributes.
        /// Implementations should return an instance that reflects the same geometry content using a unified index representation.
        /// </summary>
        /// <returns>A <see cref="UnifiedIndexedMesh"/> representing this mesh.</returns>
        UnifiedIndexedMesh AsUnified();

        /// <summary>
        /// Produces a separated indexed view of the mesh where each attribute may have its own index list.
        /// Implementations should return an instance that reflects the same geometry content using separated attribute indices.
        /// </summary>
        /// <returns>A <see cref="SeparatedIndexedMesh"/> representing this mesh.</returns>
        SeparatedIndexedMesh AsSeparated();

        /// <summary>
        /// Returns the set of attribute keys currently present in the mesh.
        /// The returned collection enumerates the keys but may not reflect subsequent mutations to the mesh.
        /// </summary>
        /// <returns>A read-only collection of <see cref="MeshAttributeKey"/> instances.</returns>
        IReadOnlyCollection<MeshAttributeKey> GetAttributeKeys();
    }
}
