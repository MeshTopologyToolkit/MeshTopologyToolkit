using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    public interface IAccessorAdapter
    {
        void AddValueByIndex(uint primIndex, IMeshVertexAttribute values, List<int> indices);

        void AddDefaultValue(IMeshVertexAttribute values, List<int> indices);

        IMeshVertexAttribute CreateMeshAttribute();
    }
}

