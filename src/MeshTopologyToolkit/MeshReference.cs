using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class MeshReference
    {
        List<Material> _materials = new List<Material>();

        public MeshReference(IMesh mesh)
        {
            Mesh = mesh;
        }

        public MeshReference(IMesh mesh, params Material[] materials)
        {
            Mesh = mesh;
            _materials.AddRange(materials);
        }

        public IMesh Mesh { get; set; }
        public IList<Material> Materials => _materials;
    }
}
