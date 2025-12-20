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

        public UnifiedIndexedMesh(string? name = null) : base(name)
        {
        }

        public UnifiedIndexedMesh(IReadOnlyList<int> indices, params ValueTuple<MeshAttributeKey, IMeshVertexAttribute>[] attributes) : base()
        {
            _indices.AddRange(indices);
            foreach (var attr in attributes)
            {
                _attributes.Add(attr.Item1, attr.Item2);
            }
        }

        public UnifiedIndexedMesh(IMesh mesh)
        {
            var attrKeys = mesh.GetAttributeKeys().ToList();
            var numAttrs = attrKeys.Count;
            var stridedIndices = new StridedIndexContainer(attrKeys);
            var tmpIndices = new int[numAttrs];

            var attributes = new List<AttributeAndIndices>();
            var indexMap = new Dictionary<StridedIndexRange, int>();

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
                var indexRange = new StridedIndexRange(tmpIndices, 0, numAttrs);
                if (!indexMap.TryGetValue(indexRange, out var newIndex))
                {
                    newIndex = stridedIndices.Count;
                    stridedIndices.Add(tmpIndices);
                    indexMap.Add(stridedIndices[newIndex], newIndex);
                }
                _indices.Add(newIndex);
            }

            for (int attrIndex = 0; attrIndex < numAttrs; ++attrIndex)
            {
                StridedListView<int> newIndexMap = new(stridedIndices.Indices, attrIndex, numAttrs, indexMap.Count);
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
            return Merge(meshes.Select(m => new MeshAndTransform { Mesh = m, Transform = Matrix4x4.Identity }).ToList());
        }

        public static UnifiedIndexedMesh Merge(IReadOnlyCollection<MeshAndTransform> meshesAndTransforms)
        {
            List<MeshAttributeKey> attributeKeys;
            var attributeKeySet = new Dictionary<MeshAttributeKey, Type>();
            foreach (var mesheAndTransform in meshesAndTransforms)
            {
                var mesh = mesheAndTransform.Mesh;

                foreach (var key in mesh.GetAttributeKeys())
                {
                    if (!mesh.TryGetAttribute(key, out var attr))
                        continue;
                    if (!attributeKeySet.TryGetValue(key, out var existingType))
                        attributeKeySet.Add(key, attr.GetElementType());
                    else if (existingType != attr.GetElementType())
                        throw new InvalidOperationException($"Cannot merge meshes: attribute type mismatch for key {key.Name}: {existingType.Name} vs {attr.GetElementType().Name}.");
                }
            }
            attributeKeys = attributeKeySet.Keys.ToList();

            var mergedMesh = new UnifiedIndexedMesh();

            void MergeAttrs<T>(MeshAttributeKey key, T defaultValue, Func<T, Matrix4x4,T> proj) where T : notnull
            {
                var targetAttribute = new ListMeshVertexAttribute<T>();
                foreach (var meshAndTransform in meshesAndTransforms)
                {
                    var mesh = meshAndTransform.Mesh;
                    var transform = meshAndTransform.Transform;

                    if (mesh.TryGetAttribute<T>(key, out var attribute))
                    {
                        targetAttribute.AddRange(attribute.Select(v=>proj(v, transform)));
                    }
                    else
                    {
                        var meshDefaultValue = proj(defaultValue, transform);
                        targetAttribute.AddRange(Enumerable.Range(0, mesh.GetNumVertices()).Select(_ => meshDefaultValue));
                    }
                }
                mergedMesh.SetAttribute(key, targetAttribute);
            }
            T AsIs<T>(T value, Matrix4x4 m) where T : notnull
            {
                return value;
            }
            foreach (var key in attributeKeys)
            {
                if (attributeKeySet[key] == typeof(Vector3))
                {
                    switch (key.Name)
                    {
                        case MeshAttributeNames.Position:
                            MergeAttrs(key, Vector3.Zero, (v,m)=>Vector3.Transform(v, m));
                            break;
                        case MeshAttributeNames.Normal:
                            MergeAttrs(key, Vector3.Zero, (v, m) => Vector3.TransformNormal(v, m).NormalizedOrDefault(Vector3.UnitY));
                            break;
                        case MeshAttributeNames.Tangent:
                            MergeAttrs(key, new Vector3(1,0,0), (v, m) => Vector3.TransformNormal(v, m).NormalizedOrDefault(Vector3.UnitX));
                            break;
                        default:
                            MergeAttrs(key, Vector3.Zero, AsIs);
                            break;
                    }
                }
                else if (attributeKeySet[key] == typeof(Vector4))
                {
                    switch (key.Name)
                    {
                        case MeshAttributeNames.Tangent:
                            MergeAttrs(key, new Vector4(1, 0, 0, 1), (v, m) => {
                                Vector3 xyz = new Vector3(v.X, v.Y, v.Z);
                                xyz = Vector3.TransformNormal(xyz, m).NormalizedOrDefault(Vector3.UnitY);
                                return new Vector4(xyz, v.W);
                            });
                            break;
                        default:
                            MergeAttrs(key, Vector4.Zero, AsIs);
                            break;
                    }
                }
                else if (attributeKeySet[key] == typeof(Vector2))
                {
                    MergeAttrs(key, Vector2.Zero, AsIs);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var vertexOffset = 0;
            var indexOffset = 0;
            foreach (var mesheAndTransform in meshesAndTransforms)
            {
                var mesh = mesheAndTransform.Mesh;

                mergedMesh.AddIndices(mesh.Indices.Select(i => i + vertexOffset));
                foreach (var drawCall in mesh.DrawCalls)
                {
                    mergedMesh.DrawCalls.Add(new MeshDrawCall(drawCall.LodLevel, drawCall.MaterialIndex, drawCall.Type, drawCall.StartIndex + indexOffset, drawCall.NumIndices));
                }
                indexOffset += mesh.Indices.Count;
                vertexOffset += mesh.GetNumVertices();
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

        public UnifiedIndexedMesh WithTriangleList()
        {
            DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, _indices.Count));
            return this;
        }

        /// <inheritdoc/>
        public bool RemoveAttribute(MeshAttributeKey key)
        {
            return _attributes.Remove(key);
        }

        public struct MeshAndTransform
        {
            public UnifiedIndexedMesh Mesh;
            public Matrix4x4 Transform;
        }
    }
}
