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

        public MeshReference(IMesh mesh, params Material[] materials):this(mesh, (IEnumerable<Material>)materials)
        {
        }

        public MeshReference(IMesh mesh, IEnumerable<Material> materials)
        {
            Mesh = mesh;
            _materials.AddRange(materials);
        }

        public IMesh Mesh { get; set; }
        public IList<Material> Materials => _materials;

        /// <summary>
        /// Optional skin. When set, joint indices of the mesh select from <see cref="Skin.Joints"/>.
        /// </summary>
        public Skin? Skin { get; set; }
    }
}
