namespace MeshTopologyToolkit
{
    public class Node
    {
        public ITransform Transform { get; set; } = TRSTransform.Identity;

        public MeshReference? Mesh { get; set; }

        public IList<Node> Children { get; } = new List<Node>();
    }
}
