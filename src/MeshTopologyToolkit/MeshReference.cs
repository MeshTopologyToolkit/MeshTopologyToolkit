using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class MeshReference
    {
        public MeshReference(IMesh mesh)
        {
            Mesh = mesh;
        }

        public IMesh Mesh { get; set; }
        public IList<Material> Materials { get; } = new List<Material>();
    }
}
