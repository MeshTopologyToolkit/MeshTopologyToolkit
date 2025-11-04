using System.Collections.Generic;
using System.Linq;

namespace MeshTopologyToolkit
{
    public class UnifiedIndexedMesh : MeshBase, IMesh
    {
        private Dictionary<MeshAttributeKey, IMeshVertexAttribute> _attributes = new Dictionary<MeshAttributeKey, IMeshVertexAttribute>();
        private List<int> _indices = new List<int>();

        public UnifiedIndexedMesh()
        {
        }

        public UnifiedIndexedMesh(IMesh mesh)
        {
            var attrKeys = mesh.GetAttributeKeys().ToList();
            var numAttrs = attrKeys.Count;
            var vertexAttrIndices = new List<int>();
            var tmpIndices = new int[numAttrs];

            var attributes = new List<AttributeAndIndices>();
            var indexMap = new Dictionary<IndexRange, int>();
            

            foreach (var attrKey in attrKeys)
            {
                var attr = mesh.GetAttribute(attrKey);
                var indices = mesh.GetAttributeIndices(attrKey);
                attributes.Add(new AttributeAndIndices(attr, indices));
            }

            for (int i=0; i < attributes[0].Indices.Count; ++i)
            {
                for (int attrIndex=0; attrIndex < numAttrs; ++attrIndex)
                {
                    tmpIndices[attrIndex] = attributes[attrIndex].Indices[i];
                }
                var indexRange = new IndexRange(tmpIndices,0,numAttrs);
                if (!indexMap.TryGetValue(indexRange, out var newIndex))
                {
                    newIndex = vertexAttrIndices.Count / numAttrs;
                    vertexAttrIndices.AddRange(tmpIndices);
                    indexMap.Add(new IndexRange(vertexAttrIndices, newIndex* numAttrs, numAttrs), newIndex);
                }
                _indices.Add(newIndex);
            }

            for (int attrIndex = 0; attrIndex < numAttrs; ++attrIndex)
            {
                StridedListView<int> newIndexMap = new(vertexAttrIndices, attrIndex, numAttrs, indexMap.Count);
                _attributes.Add(attrKeys[attrIndex], attributes[attrIndex].Attribute.Remap(newIndexMap));
            }

            foreach (var drawCall in mesh.DrawCalls)
            {
                DrawCalls.Add(drawCall.Clone());
            }
        }

        /// <summary>
        /// Index buffer for the mesh.
        /// </summary>
        public IList<int> Indices => _indices;

        /// <summary>
        /// Add attribute to the mesh. If attribute is null, it is ignored.
        /// </summary>
        /// <param name="key">Attribute key.</param>
        /// <param name="attribute">Attribute data.</param>
        public void AddAttribute(MeshAttributeKey key, IMeshVertexAttribute? attribute)
        {
            if (attribute != null)
                _attributes.Add(key, attribute);
        }

        /// <inheritdoc/>
        public SeparatedIndexedMesh AsSeparated()
        {
            return new SeparatedIndexedMesh(this);
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
