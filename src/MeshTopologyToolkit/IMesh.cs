namespace MeshTopologyToolkit
{
    public interface IMesh
    {
        bool TryGetAttribute(MeshAttributeKey key, out IMeshVertexAttribute? attribute);
        bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T>? attribute) where T : notnull;
        bool TryGetAttributeIndices(MeshAttributeKey key, out IReadOnlyList<int>? indices);

        UnifiedIndexedMesh AsUnified();

        SeparatedIndexedMesh AsSeparated();
        IReadOnlyCollection<MeshAttributeKey> GetAttributeKeys();

        IList<MeshDrawCall> DrawCalls { get; }
    }
}
