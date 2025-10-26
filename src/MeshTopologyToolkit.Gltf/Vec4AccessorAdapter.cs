using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    public class Vec4AccessorAdapter : IAccessorAdapter
    {
        private IAccessorArray<Vector4> _values;

        public Vec4AccessorAdapter(IAccessorArray<Vector4> values)
        {
            _values = values;
        }

        public void AddValueByIndex(uint primIndex, IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector4>)values;
            var v = _values[(int)primIndex];
            indices.Add(vals.Add(v));
        }

        public void AddDefaultValue(IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector4>)values;
            var v = Vector4.Zero;
            indices.Add(vals.Add(v));
        }

        public IMeshVertexAttribute CreateMeshAttribute()
        {
            return new DictionaryMeshVertexAttribute<Vector4>();
        }

        public IAccessorAdapter MakeDefaultValueAdapter()
        {
            return new ConstAdapter<Vector4>(Vector4.Zero);
        }
    }
}


