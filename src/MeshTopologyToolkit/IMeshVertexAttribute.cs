using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public interface IMeshVertexAttribute
    {
    }

    public interface IMeshVertexAttribute<T>: IReadOnlyList<T>
    {
    }
}
