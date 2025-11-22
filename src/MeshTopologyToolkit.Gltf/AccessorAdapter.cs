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

            if (accessor.Format.Dimensions == DimensionType.VEC3)
            {
                _accessorAdapter = new Vec3AccessorAdapter(accessor.AsVector3Array());
            }
            else if (accessor.Format.Dimensions == DimensionType.VEC2)
            {
                _accessorAdapter = new Vec2AccessorAdapter(accessor.AsVector2Array());
            }
            else if (accessor.Format.Dimensions == DimensionType.VEC4)
            {
                _accessorAdapter = new Vec4AccessorAdapter(accessor.AsVector4Array());
            }
            else
            {
                throw new NotImplementedException($"Accessor {key} format '{accessor.Format.Dimensions} {accessor.Format.Encoding} {accessor.Format.Normalized}' is not implemented.");
            }
        }

        internal void AddValueByIndex(uint primIndex)
        {
            _accessorAdapter.AddValueByIndex(primIndex, MeshVertexAttribute!, MeshVertexAttributeIndices!);
        }

        internal void CreateMeshVertexAttribute()
        {
            MeshVertexAttribute = _accessorAdapter.CreateMeshAttribute();
            MeshVertexAttributeIndices = new List<int>();
        }

        internal void SwitchToDefaultAdapter()
        {
            _accessorAdapter = _accessorAdapter.MakeDefaultValueAdapter();
        }
    }
}

