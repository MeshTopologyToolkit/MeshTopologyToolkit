namespace MeshTopologyToolkit
{
    public class MeshReference
    {
        public MeshReference(IMesh mesh)
        {
            Mesh = mesh;
        }

        public IMesh Mesh { get; set; }
    }
}
