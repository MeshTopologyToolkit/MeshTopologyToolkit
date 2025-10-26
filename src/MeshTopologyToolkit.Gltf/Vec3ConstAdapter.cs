using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    public class Vec3ConstAdapter : IAccessorAdapter
    {
        private Vector3 _value;

        public Vec3ConstAdapter(Vector3 value)
        {
            _value = value;
        }

        public void AddValueByIndex(uint primIndex, IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector3>)values;
            indices.Add(vals.Add(_value));
        }

        public void AddDefaultValue(IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<Vector3>)values;
            indices.Add(vals.Add(_value));
        }

        public IMeshVertexAttribute CreateMeshAttribute()
        {
            return new DictionaryMeshVertexAttribute<Vector3>();
        }
    }
}

