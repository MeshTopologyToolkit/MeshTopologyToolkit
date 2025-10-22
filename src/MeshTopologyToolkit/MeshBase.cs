using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class MeshBase
    {
        IList<MeshDrawCall> _drawCalls = new List<MeshDrawCall>();
        public IList<MeshDrawCall> DrawCalls => _drawCalls;

        public string? Name { get; set; }
    }
}
