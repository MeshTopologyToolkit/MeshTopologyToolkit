namespace MeshTopologyToolkit
{
    public class SeparatedIndexedMesh : IMesh
    {
        struct AttributeAndIndices
        {
            public IMeshVertexAttribute Attribute;
            public IReadOnlyList<int> Indices;

            public AttributeAndIndices(IMeshVertexAttribute attr, IReadOnlyList<int> indices)
            {
                Attribute = attr;
                Indices = indices;
            }
        }
        private Dictionary<MeshAttributeKey, AttributeAndIndices> _attributes = new Dictionary<MeshAttributeKey, AttributeAndIndices>();

        public void AddAttribute(MeshAttributeKey key, IMeshVertexAttribute attribute, IReadOnlyList<int> indices)
        {
            _attributes.Add(key, new AttributeAndIndices(attribute, indices));
        }

        /// <inheritdoc/>
        public bool TryGetAttribute(MeshAttributeKey key, out IMeshVertexAttribute? attribute)
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                attribute = value.Attribute;
                return true;
            }
            attribute = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T>? attribute)
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                attribute = value.Attribute as IMeshVertexAttribute<T>;
                return attribute != null;
            }
            attribute = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetAttributeIndices(MeshAttributeKey key, out IReadOnlyList<int>? indices)
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                indices = value.Indices;
                return true;
            }
            indices = null;
            return false;
        }

        /// <inheritdoc/>
        public UnifiedIndexedMesh AsUnified()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public SeparatedIndexedMesh AsSeparated()
        {
            return this;
        }
    }
}
