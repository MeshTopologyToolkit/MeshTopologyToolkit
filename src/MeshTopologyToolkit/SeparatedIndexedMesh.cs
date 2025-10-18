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

        public SeparatedIndexedMesh()
        {
        }

        public SeparatedIndexedMesh(IMesh mesh)
        {
            var attributes = mesh.GetAttributeKeys();
            foreach (var attributeKey in attributes)
            {
                if (!mesh.TryGetAttribute(attributeKey, out var attribute) || attribute == null)
                    throw new KeyNotFoundException($"Can't get attribute {attributeKey}");
                if (!mesh.TryGetAttributeIndices(attributeKey, out var indices) || indices == null)
                    throw new KeyNotFoundException($"Can't get attribute indices for {attributeKey}");

                var compactAttr = attribute.Compact(out var mapping);
                var compactIndices = indices.Select(_ => mapping[_]).ToList();
                _attributes.Add(attributeKey, new AttributeAndIndices(compactAttr, compactIndices));
            }
        }

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
        public bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T>? attribute) where T : notnull
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

        public IReadOnlyCollection<MeshAttributeKey> GetAttributeKeys()
        {
            return _attributes.Keys;
        }
    }
}
