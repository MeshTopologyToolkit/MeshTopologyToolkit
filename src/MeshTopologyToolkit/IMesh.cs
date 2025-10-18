namespace MeshTopologyToolkit
{
    public interface IMesh
    {
        bool TryGetAttribute(MeshAttributeKey key, out IMeshVertexAttribute? attribute);
        bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T>? attribute);
        bool TryGetAttributeIndices(MeshAttributeKey key, out IReadOnlyList<int>? indices);

        UnifiedIndexedMesh AsUnified();

        SeparatedIndexedMesh AsSeparated();
    }
}
