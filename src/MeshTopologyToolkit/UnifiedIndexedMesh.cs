using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class UnifiedIndexedMesh : MeshBase, IMesh
    {
        private Dictionary<MeshAttributeKey, IMeshVertexAttribute> _attributes = new Dictionary<MeshAttributeKey, IMeshVertexAttribute>();
        private List<int> _indices = new List<int>();

        public UnifiedIndexedMesh(string? name = null)
        {
            Name = name;
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

            for (int i = 0; i < attributes[0].Indices.Count; ++i)
            {
                for (int attrIndex = 0; attrIndex < numAttrs; ++attrIndex)
                {
                    tmpIndices[attrIndex] = attributes[attrIndex].Indices[i];
                }
                var indexRange = new IndexRange(tmpIndices, 0, numAttrs);
                if (!indexMap.TryGetValue(indexRange, out var newIndex))
                {
                    newIndex = vertexAttrIndices.Count / numAttrs;
                    vertexAttrIndices.AddRange(tmpIndices);
                    indexMap.Add(new IndexRange(vertexAttrIndices, newIndex * numAttrs, numAttrs), newIndex);
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

        /// <summary>
        /// Set attribute to the mesh. If attribute is null, existing attribute gets deleted.
        /// </summary>
        /// <param name="key">Attribute key.</param>
        /// <param name="attribute">Attribute data.</param>
        public void SetAttribute(MeshAttributeKey key, IMeshVertexAttribute? attribute)
        {
            if (attribute != null)
                _attributes[key] = attribute;
            else
                _attributes.Remove(key);
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
        public bool TryGetAttribute(MeshAttributeKey key, out IMeshVertexAttribute attribute)
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                attribute = value;
                return attribute != null;
            }
            attribute = EmptyMeshAttribute.Instance;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T> attribute) where T : notnull
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                var res = value as IMeshVertexAttribute<T>;
                if (res != null)
                {
                    attribute = res;
                    return true;
                }
                if (value.TryCast<T>(MeshVertexAttributeConverterProvider.Default, out res))
                {
                    attribute = res;
                    return true;
                }
            }
            attribute = EmptyMeshAttribute<T>.Instance;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetAttributeIndices(MeshAttributeKey key, out IReadOnlyList<int> indices)
        {
            indices = _indices;
            return true;
        }

        public void AddIndices(IEnumerable<int> indices)
        {
            _indices.AddRange(indices);
        }

        public static UnifiedIndexedMesh Merge(params UnifiedIndexedMesh[] meshes)
        {
            return Merge((IReadOnlyList<UnifiedIndexedMesh>)meshes);
        }
        public static UnifiedIndexedMesh Merge(IReadOnlyCollection<UnifiedIndexedMesh> meshes)
        {
            List<MeshAttributeKey> attributeKeys;
            var attributeKeySet = new Dictionary<MeshAttributeKey, Type>();
            foreach (var mesh in meshes)
            {
                foreach (var key in mesh.GetAttributeKeys())
                {
                    if (!mesh.TryGetAttribute(key, out var attr))
                        continue;
                    attributeKeySet.Add(key, attr.GetElementType());
                }
            }
            attributeKeys = attributeKeySet.Keys.ToList();

            var mergedMesh = new UnifiedIndexedMesh();

            void MergeAttrs<T>(MeshAttributeKey key, T defaultValue) where T : notnull
            {
                var targetAttribute = new ListMeshVertexAttribute<T>();
                foreach (var mesh in meshes)
                {
                    if (mesh.TryGetAttribute<T>(key, out var attribute))
                    {
                        targetAttribute.AddRange(attribute);
                    }
                    else
                    {
                        targetAttribute.AddRange(Enumerable.Range(0, mesh.GetNumVertices()).Select(_ => defaultValue));
                    }
                }
                mergedMesh.SetAttribute(key, targetAttribute);
            }
            foreach (var key in attributeKeys)
            {
                if (attributeKeySet[key] == typeof(Vector3))
                {
                    MergeAttrs(key, Vector3.Zero);
                }
                else if (attributeKeySet[key] == typeof(Vector4))
                {
                    if (key == MeshAttributeKey.Tangent)
                        MergeAttrs(key, new Vector4(1, 0, 0, 1));
                    else
                        MergeAttrs(key, Vector4.Zero);
                }
                else if (attributeKeySet[key] == typeof(Vector2))
                {
                    MergeAttrs(key, Vector2.Zero);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var vertexOffset = 0;
            var indexOffset = 0;
            foreach (var mesh in meshes)
            {
                mergedMesh.AddIndices(mesh.Indices.Select(i => i + vertexOffset));
                foreach (var drawCall in mesh.DrawCalls)
                {
                    mergedMesh.DrawCalls.Add(new MeshDrawCall(drawCall.LodLevel, drawCall.MaterialIndex, drawCall.Type, drawCall.StartIndex + indexOffset, drawCall.NumIndices));
                }
                indexOffset += mesh.Indices.Count;
            }
            return mergedMesh;
        }

        public int GetNumVertices()
        {
            foreach (var attr in _attributes.Values)
            {
                return attr.Count;
            }
            return 0;
        }
    }
}
