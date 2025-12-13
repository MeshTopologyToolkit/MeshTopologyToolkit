using System.Collections.Generic;

namespace MeshTopologyToolkit.Gltf
{
    public class ConstAdapter<T> : IAccessorAdapter where T : notnull
    {
        private T _value;

        public ConstAdapter(T value)
        {
            _value = value;
        }

        public void AddValueByIndex(uint primIndex, IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<T>)values;
            indices.Add(vals.Add(_value));
        }

        public void AddDefaultValue(IMeshVertexAttribute values, List<int> indices)
        {
            var vals = (IMeshVertexAttribute<T>)values;
            indices.Add(vals.Add(_value));
        }

        public IMeshVertexAttribute CreateMeshAttribute()
        {
            return new DictionaryMeshVertexAttribute<T>();
        }

        public IAccessorAdapter MakeDefaultValueAdapter()
        {
            return this;
        }
    }
}

