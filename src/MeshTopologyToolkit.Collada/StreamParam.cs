namespace MeshTopologyToolkit.Collada
{
    public class StreamParam
    {
        public StreamParam(string name)
        {
            Name = name;
            Type = "float";
        }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
