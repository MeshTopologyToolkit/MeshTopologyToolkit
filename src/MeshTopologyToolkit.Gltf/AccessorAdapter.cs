using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MeshTopologyToolkit.Gltf
{
    internal class AccessorAdapter
    {
        private IAccessorAdapter _accessorAdapter;

        public string OriginalKey { get; }

        public MeshAttributeKey AttributeKey { get; }

        public Accessor Accessor { get; }

        public IMeshVertexAttribute? MeshVertexAttribute { get; set; }
        public List<int>? MeshVertexAttributeIndices { get; set; }

        public AccessorAdapter(string key, Accessor accessor)
        {
            OriginalKey = key;
            Accessor = accessor;
            var channelDelimeter = key.LastIndexOf('_');
            if (channelDelimeter >= 0)
            {
                AttributeKey = new MeshAttributeKey(key.Substring(0, channelDelimeter), int.Parse(key.Substring(channelDelimeter + 1), CultureInfo.InvariantCulture));
            }
            else
            {
                AttributeKey = new MeshAttributeKey(key, 0);
            }

            if (accessor.Format == new AttributeFormat(DimensionType.VEC3, EncodingType.FLOAT))
            {
                _accessorAdapter = new Vec3AccessorAdapter(accessor);
            }
            else
            {
                throw new NotImplementedException($"Accessor format '{accessor.Format}' is not implemented.");
            }
        }

        internal void AddValueByIndex(uint primIndex)
        {
            _accessorAdapter.AddValueByIndex(primIndex, MeshVertexAttribute, MeshVertexAttributeIndices);
        }

        internal void CreateMeshVertexAttribute()
        {
            MeshVertexAttribute = _accessorAdapter.CreateMeshAttribute();
            MeshVertexAttributeIndices = new List<int>();
        }
    }
}

