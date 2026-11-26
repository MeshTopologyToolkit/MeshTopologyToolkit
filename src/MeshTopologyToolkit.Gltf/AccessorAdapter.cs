using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

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
            _accessorAdapter = DefaultAdapter();
        }

        /// <summary>Adds the neutral value <paramref name="count"/> times, for the indices of primitives that did not have this attribute.</summary>
        internal void AddDefaultValues(int count)
        {
            if (count <= 0)
                return;
            var defaults = DefaultAdapter();
            for (int i = 0; i < count; i++)
            {
                defaults.AddValueByIndex(0, MeshVertexAttribute!, MeshVertexAttributeIndices!);
            }
        }

        /// <summary>What a vertex has when its primitive lacks the attribute: white for colours (zero would make the mesh black), zero for the rest.</summary>
        private IAccessorAdapter DefaultAdapter()
        {
            if (OriginalKey.StartsWith("COLOR", StringComparison.Ordinal))
            {
                if (_accessorAdapter is Vec4AccessorAdapter)
                    return new ConstAdapter<Vector4>(Vector4.One);
                if (_accessorAdapter is Vec3AccessorAdapter)
                    return new ConstAdapter<Vector3>(Vector3.One);
            }
            return _accessorAdapter.MakeDefaultValueAdapter();
        }
    }
}

