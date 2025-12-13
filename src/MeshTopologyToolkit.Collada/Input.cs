namespace MeshTopologyToolkit.Collada
{
    public class Input
    {
        public Input(string semantic, int offset, string source, int? set, IReadOnlyList<int> indices)
        {
            Semantic = semantic;
            Offset = offset;
            Set = set;
            Source = source;
            Indices = indices;
        }

        public string Semantic { get; }
        public int Offset { get; }
        public int? Set { get; }
        public string Source { get; }
        public IReadOnlyList<int> Indices { get; }
    }
}
