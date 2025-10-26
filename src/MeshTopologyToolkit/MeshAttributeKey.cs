namespace MeshTopologyToolkit
{
    public struct MeshAttributeKey
    {
        public static readonly MeshAttributeKey Position = new MeshAttributeKey(MeshAttributeNames.Position, 0);
        public static readonly MeshAttributeKey Normal = new MeshAttributeKey(MeshAttributeNames.Normal, 0);
        public static readonly MeshAttributeKey Tangent = new MeshAttributeKey(MeshAttributeNames.Tangent, 0);
        public static readonly MeshAttributeKey TexCoord = new MeshAttributeKey(MeshAttributeNames.TexCoord, 0);
        public static readonly MeshAttributeKey Color = new MeshAttributeKey(MeshAttributeNames.Color, 0);
        public static readonly MeshAttributeKey Joints = new MeshAttributeKey(MeshAttributeNames.Joints, 0);
        public static readonly MeshAttributeKey Weights = new MeshAttributeKey(MeshAttributeNames.Weights, 0);

        private string _name;
        private int _channel;

        public MeshAttributeKey(string name, int channel)
        {
            _name = name;
            _channel = channel;
        }

        public string Name => _name;

        public int Channel => _channel;

        public bool IsValid { get
            {
                return !string.IsNullOrEmpty(_name);
            } 
        }

        public override string ToString()
        {
            if (_channel == 0)
            {
                return _name;
            }
            return $"{_name}[{_channel}]";
        }
    }
}
