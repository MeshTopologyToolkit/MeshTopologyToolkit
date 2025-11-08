using SharpGLTF.Memory;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    public class Vec3AccessorAdapter : IAccessorAdapter
    {
        private IAccessorArray<Vector3> _values;

        public Vec3AccessorAdapter(IAccessorArray<Vector3> values)
        {
            _values = values;
        }

        public void AddValueByIndex(uint primIndex, IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector3>)values;
            var v = _values[(int)primIndex];
            indices.Add(vals.Add(v));
        }

        public void AddDefaultValue(IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector3>)values;
            var v = Vector3.Zero;
            indices.Add(vals.Add(v));
        }

        public IMeshVertexAttribute CreateMeshAttribute()
        {
            return new DictionaryMeshVertexAttribute<Vector3>();
        }

        public IAccessorAdapter MakeDefaultValueAdapter()
        {
            return new ConstAdapter<Vector3>(Vector3.Zero);
        }
    }
}


