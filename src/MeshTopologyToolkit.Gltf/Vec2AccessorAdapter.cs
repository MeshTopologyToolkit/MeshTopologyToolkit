using SharpGLTF.Memory;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    public class Vec2AccessorAdapter : IAccessorAdapter
    {
        private IAccessorArray<Vector2> _values;

        public Vec2AccessorAdapter(IAccessorArray<Vector2> values)
        {
            _values = values;
        }

        public void AddValueByIndex(uint primIndex, IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector2>)values;
            var v = _values[(int)primIndex];
            indices.Add(vals.Add(v));
        }

        public void AddDefaultValue(IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector2>)values;
            var v = Vector2.Zero;
            indices.Add(vals.Add(v));
        }

        public IAccessorAdapter MakeDefaultValueAdapter()
        {
            return new ConstAdapter<Vector2>(Vector2.Zero);
        }

        public IMeshVertexAttribute CreateMeshAttribute()
        {
            return new DictionaryMeshVertexAttribute<Vector2>();
        }
    }
}


