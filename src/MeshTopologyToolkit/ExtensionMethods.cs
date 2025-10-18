using System.Numerics;

namespace MeshTopologyToolkit
{
    public static class ExtensionMethods
    {
        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }
    }
}
