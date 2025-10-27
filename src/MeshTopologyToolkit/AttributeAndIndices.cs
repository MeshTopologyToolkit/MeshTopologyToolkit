using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    internal struct AttributeAndIndices
    {
        public IMeshVertexAttribute Attribute;
        public IReadOnlyList<int> Indices;

        public AttributeAndIndices(IMeshVertexAttribute attr, IReadOnlyList<int> indices)
        {
            Attribute = attr;
            Indices = indices;
        }
    }
}
