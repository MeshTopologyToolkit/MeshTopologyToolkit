namespace MeshTopologyToolkit
{
    public struct MeshAttributeKey
    {
        private string _name;
        private int _channel;

        public MeshAttributeKey(string name, int channel)
        {
            _name = name;
            _channel = channel;
        }

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
