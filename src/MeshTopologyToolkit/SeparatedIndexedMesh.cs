using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class SeparatedIndexedMesh : MeshBase, IMesh
    {
        private Dictionary<MeshAttributeKey, AttributeAndIndices> _attributes = new Dictionary<MeshAttributeKey, AttributeAndIndices>();

        public SeparatedIndexedMesh(string? name = null): base(name)
        {
        }

        public SeparatedIndexedMesh(IMesh mesh)
        {
            var attributes = mesh.GetAttributeKeys();
            foreach (var attributeKey in attributes)
            {
                if (!mesh.TryGetAttribute(attributeKey, out var attribute))
                    throw new KeyNotFoundException($"Can't get attribute {attributeKey}");
                if (!mesh.TryGetAttributeIndices(attributeKey, out var indices))
                    throw new KeyNotFoundException($"Can't get attribute indices for {attributeKey}");

                var compactAttr = attribute.Compact(out var mapping);
                var compactIndices = indices.Select(_ => mapping[_]).ToList();
                _attributes.Add(attributeKey, new AttributeAndIndices(compactAttr, compactIndices));
            }

            foreach (var drawCall in mesh.DrawCalls)
            {
                DrawCalls.Add(drawCall.Clone());
            }
        }

        public void AddAttribute(MeshAttributeKey key, IMeshVertexAttribute? attribute, IReadOnlyList<int> indices)
        {
            if (attribute != null)
            {
                if (indices == null)
                    throw new ArgumentException("Indices for the not null attribute are missing");
                _attributes.Add(key, new AttributeAndIndices(attribute, indices));
            }
        }

        /// <summary>
        /// Set attribute to the mesh. If attribute is null, existing attribute gets deleted.
        /// </summary>
        /// <param name="key">Attribute key.</param>
        /// <param name="attribute">Attribute data.</param>
        public void SetAttribute(MeshAttributeKey key, IMeshVertexAttribute? attribute, IReadOnlyList<int> indices)
        {
            if (attribute != null)
            {
                if (indices == null)
                    throw new ArgumentException("Indices for the not null attribute are missing");
                _attributes[key] = new AttributeAndIndices(attribute, indices);
            }
            else
            {
                _attributes.Remove(key);
            }
        }

        /// <inheritdoc/>
        public bool TryGetAttribute(MeshAttributeKey key, out IMeshVertexAttribute attribute)
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                attribute = value.Attribute;
                return true;
            }
            attribute = new EmptyMeshAttribute();
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute<T>(MeshAttributeKey key, out IMeshVertexAttribute<T> attribute) where T : notnull
        {
            if (_attributes.TryGetValue(key, out var value))
            {
                var res = value.Attribute as IMeshVertexAttribute<T>;
                if (res != null)
                {
                    attribute = res;
                    return true;
                }
                if (value.Attribute.TryCast<T>(MeshVertexAttributeConverterProvider.Default, out res))
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
            if (_attributes.TryGetValue(key, out var value))
            {
                indices = value.Indices;
                return true;
            }
            indices = Array.Empty<int>();
            return false;
        }

        /// <inheritdoc/>
        public UnifiedIndexedMesh AsUnified()
        {
            return new UnifiedIndexedMesh(this);
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

        public bool HasAttribute(MeshAttributeKey key)
        {
            return _attributes.TryGetValue(key, out var value) && value.Indices != null && value.Attribute != null;
        }
    }
}
