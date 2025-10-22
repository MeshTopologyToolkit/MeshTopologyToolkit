using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public static class ExtensionMethods
    {
        public static IMeshVertexAttribute<T> GetAttribute<T>(this IMesh mesh, MeshAttributeKey key) where T: notnull
        {
            if (!mesh.TryGetAttribute<T>(key, out var result) || result == null)
            {
                throw new KeyNotFoundException($"Attribute {key} not found");
            }
            return result;
        }

        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            return new Vector3(x, y, z);
        }

        public static void Write(this BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
        }
    }
}
