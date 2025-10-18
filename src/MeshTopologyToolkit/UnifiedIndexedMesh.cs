namespace MeshTopologyToolkit
{
    public class UnifiedIndexedMesh : MeshBase, IMesh
    {
        private Dictionary<MeshAttributeKey, IMeshVertexAttribute> _attributes = new Dictionary<MeshAttributeKey, IMeshVertexAttribute>();
        private List<int> _indices = new List<int>();

        /// <summary>
        /// Index buffer for the mesh.
        /// </summary>
        public IList<int> Indices => _indices;

        public void AddAttribute(MeshAttributeKey key, IMeshVertexAttribute attribute)
        {
            _attributes.Add(key, attribute);
        }

        /// <inheritdoc/>
        public SeparatedIndexedMesh AsSeparated()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public UnifiedIndexedMesh AsUnified()
        {
            return this;
        }

        public IReadOnlyCollection<MeshAttributeKey> GetAttributeKeys()
        {
            return _attributes.Keys;
        }

        public bool HasAttribute(MeshAttributeKey key)
        {
            return _attributes.TryGetValue(key, out var value) && value != null;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute(MeshAttributeKey key, out IMeshVertexAttribute? attribute)
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                attribute = value;
                return attribute != null;
            }
            attribute = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T>? attribute) where T : notnull
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                attribute = value as IMeshVertexAttribute<T>;
                return attribute != null;
            }
            attribute = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetAttributeIndices(MeshAttributeKey key, out IReadOnlyList<int>? indices)
        {
            indices = _indices;
            return true;
        }
    }
}
