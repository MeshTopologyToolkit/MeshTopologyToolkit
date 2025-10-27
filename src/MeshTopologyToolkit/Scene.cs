namespace MeshTopologyToolkit
{
    public class Scene : Node
    {
        public Scene(string? name = null): base(name)
        {
        }

        public override string ToString()
        {
            if (Name != null)
                return $"Scene \"{Name}\"";
            return "Scene";
        }
    }
}
