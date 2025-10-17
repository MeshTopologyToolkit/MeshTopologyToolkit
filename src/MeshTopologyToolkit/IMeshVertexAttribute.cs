using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public interface IMeshVertexAttribute
    {
        bool TryCast<T>(IMeshVertexAttributeConverterProvider converterProvider, out IMeshVertexAttribute<T>? attribute);
    }

    public interface IMeshVertexAttribute<T>: IMeshVertexAttribute, IReadOnlyList<T>
    {
    }
}
