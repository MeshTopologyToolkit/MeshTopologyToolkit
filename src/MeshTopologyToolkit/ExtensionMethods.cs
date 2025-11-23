using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Various extension methods.
    /// </summary>
    public static partial class ExtensionMethods
    {
        public static IEnumerable<MeshDrawCall.Face> GetFaces(this IMesh mesh)
        {
            foreach (var drawCall in mesh.DrawCalls)
            {
                foreach (var face in drawCall.GetFaces())
                {
                    yield return face;
                }
            }
        }

        public static IMeshVertexAttribute<T> GetAttribute<T>(this IMesh mesh, MeshAttributeKey key) where T: notnull
        {
            if (!mesh.TryGetAttribute<T>(key, out var result))
            {
                throw new KeyNotFoundException($"Attribute {key} not found");
            }
            return result;
        }

        public static IMeshVertexAttribute GetAttribute(this IMesh mesh, MeshAttributeKey key)
        {
            if (!mesh.TryGetAttribute(key, out var result))
            {
                throw new KeyNotFoundException($"Attribute {key} not found");
            }
            return result;
        }

        public static IReadOnlyList<int> GetAttributeIndices(this IMesh mesh, MeshAttributeKey key)
        {
            if (!mesh.TryGetAttributeIndices(key, out var result))
            {
                throw new KeyNotFoundException($"Attribute indices for {key} not found");
            }
            return result;
        }

        public static Vector3 NormalizedOrDefault(this Vector3 val, Vector3 defaultValue)
        {
            var res = Vector3.Normalize(val);
            if (res.IsNanOrInf())
                return defaultValue;
            return res;
        }

        public static Vector2 NormalizedOrDefault(this Vector2 val, Vector2 defaultValue)
        {
            var res = Vector2.Normalize(val);
            if (res.IsNanOrInf())
                return defaultValue;
            return res;
        }

        public static int NumberOfBits(this int n)
        {
            if (n == 0) return 0;

            int count = 0;
            while (n != 0)
            {
                count++;
                n >>= 1;
            }

            return count;
        }

        public static int NextPowerOfTwo(this int n)
        {
            if (n < 1)
                return 1; // handle non-positive numbers

            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;
            n++;

            return n;
        }

        public static bool IsNanOrInf(this Vector2 val)
        {
            return val.X.IsNanOrInf() || val.Y.IsNanOrInf();
        }

        public static bool IsNanOrInf(this Vector3 val)
        {
            return val.X.IsNanOrInf() || val.Y.IsNanOrInf() || val.Z.IsNanOrInf();
        }

        public static bool IsNanOrInf(this Vector4 val)
        {
            return val.X.IsNanOrInf() || val.Y.IsNanOrInf() || val.Z.IsNanOrInf() || val.W.IsNanOrInf();
        }

        public static bool IsNanOrInf(this float val)
        {
            return float.IsNaN(val) || float.IsInfinity(val);
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

        public static byte[] ReadAllBytes(this IFileSystemEntry? entry)
        {
            if (entry == null)
            {
                return Array.Empty<byte>();
            }
            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return Array.Empty<byte>();

                var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static void ApplyUVProjection(this IMesh mesh, Matrix4x4 prjectionMatrix)
        {
            var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
            var texCoords = new ListMeshVertexAttribute<Vector2>();
            foreach (var pos in positions)
            {
                var proj = Vector3.Transform(pos, prjectionMatrix);
                texCoords.Add(new Vector2(proj.X, proj.Y));
            }
            if (mesh is SeparatedIndexedMesh separatedIndexedMesh)
            {
                separatedIndexedMesh.SetAttribute(MeshAttributeKey.TexCoord, texCoords, mesh.GetAttributeIndices(MeshAttributeKey.Position));
            }
            else if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                unifiedIndexedMesh.SetAttribute(MeshAttributeKey.TexCoord, texCoords);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
