using System;
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

        public static IMeshVertexAttribute GetAttribute(this IMesh mesh, MeshAttributeKey key)
        {
            if (!mesh.TryGetAttribute(key, out var result) || result == null)
            {
                throw new KeyNotFoundException($"Attribute {key} not found");
            }
            return result;
        }

        public static IReadOnlyList<int> GetAttributeIndices(this IMesh mesh, MeshAttributeKey key)
        {
            if (!mesh.TryGetAttributeIndices(key, out var result) || result == null)
            {
                throw new KeyNotFoundException($"Attribute indices for {key} not found");
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

        /// <summary>
        /// Ensure the mesh has tangent attribute. If not, generate it.
        /// The tangent is generated assuming the input data is in GLTf style (right-handed UVs, bitangent pointing down).
        /// </summary>
        /// <param name="mesh">Mesh to modify.</param>
        public static void EnsureTangents(this IMesh mesh)
        {
            if (mesh.HasAttribute(MeshAttributeKey.Tangent))
            {
                return;
            }   

            if (mesh is SeparatedIndexedMesh separatedIndexedMesh)
            {
                EnsureSeparatedIndexedMeshTangents(separatedIndexedMesh);
            }
            else if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                EnsureUnifiedIndexedMeshTangents(unifiedIndexedMesh);
            }
            else
            {
                throw new NotImplementedException($"Unknown mesh type. Only {nameof(SeparatedIndexedMesh)} and {nameof(UnifiedIndexedMesh)} supported.");
            }
        }

        private static void EnsureSeparatedIndexedMeshTangents(SeparatedIndexedMesh mesh)
        {
            var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
            var positionIndices = mesh.GetAttributeIndices(MeshAttributeKey.Position);
            if (!mesh.TryGetAttribute<Vector2>(MeshAttributeKey.TexCoord, out var texCoords) ||
                !mesh.TryGetAttributeIndices(MeshAttributeKey.TexCoord, out var texCoordIndices))
            {
                texCoords = new ConstMeshVertexAttribute<Vector2>(Vector2.Zero, 1);
                texCoordIndices = new ConstMeshVertexAttribute<int>(0, positionIndices.Count);
            }
            IReadOnlyList<int>? normalIndices = null;
            if (!mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var normals) ||
                !mesh.TryGetAttributeIndices(MeshAttributeKey.Normal, out normalIndices))
            {
            }

            var accTangent = new Vector3[positionIndices.Count];
            var accBitangent = new Vector3[positionIndices.Count];

            var accumulateTangent = (int a, int b, int c) =>
            {
                var v0 = positions[positionIndices[a]];
                var v1 = positions[positionIndices[b]];
                var v2 = positions[positionIndices[c]];
                var uv0 = texCoords![texCoordIndices![a]];
                var uv1 = texCoords[texCoordIndices[b]];
                var uv2 = texCoords[texCoordIndices[c]];
                var deltaPos1 = v1 - v0;
                var deltaPos2 = v2 - v0;
                var deltaUV1 = (uv1 - uv0);// * new Vector2(1, -1);
                var deltaUV2 = (uv2 - uv0);// * new Vector2(1, -1);
                var r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
                var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
                var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;
                accTangent[a] += tangent;
                accTangent[b] += tangent;
                accTangent[c] += tangent;
                accBitangent[a] += bitangent;
                accBitangent[b] += bitangent;
                accBitangent[c] += bitangent;
            };

            foreach (var drawCall in mesh.DrawCalls)
            {
                foreach (var face in drawCall.GetFaces())
                {
                    accumulateTangent(face.A, face.B, face.C);
                }
            }

            var tangentIndices = new int[positionIndices.Count];
            var tangents = new TangentRTree3MeshVertexAttribute();

            for (int i = 0; i < accTangent.Length; i++)
            {
                var n = (normals != null) ? Vector3.Normalize(normals[normalIndices![i]]) : Vector3.UnitZ;
                Vector3 t = Vector3.Normalize(accTangent[i]);
                Vector3 b = Vector3.Normalize(accBitangent[i]);
                if (t == Vector3.Zero)
                {
                    tangentIndices[i] = tangents.Add(new Vector4(0, 0, 1, 1));
                }
                else
                {
                    tangentIndices[i] = AddTangent(tangents, n, t, b);
                }
            }

            mesh.AddAttribute(MeshAttributeKey.Tangent, tangents, tangentIndices);
        }


        private static void EnsureUnifiedIndexedMeshTangents(UnifiedIndexedMesh mesh)
        {
            var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
            var indices = mesh.Indices;

            if (!mesh.TryGetAttribute<Vector2>(MeshAttributeKey.TexCoord, out var texCoords))
            {
                texCoords = new ConstMeshVertexAttribute<Vector2>(Vector2.Zero, 1);
            }
            if (!mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var normals))
            {
            }

            var accTangent = new Vector3[positions.Count];
            var accBitangent = new Vector3[positions.Count];

            var accumulateTangent = (int a, int b, int c) =>
            {
                var v0 = positions[a];
                var v1 = positions[b];
                var v2 = positions[c];
                var uv0 = texCoords![a];
                var uv1 = texCoords[b];
                var uv2 = texCoords[c];
                var deltaPos1 = v1 - v0;
                var deltaPos2 = v2 - v0;
                var deltaUV1 = uv1 - uv0;
                var deltaUV2 = uv2 - uv0;
                var r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV1.Y * deltaUV2.X);
                var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
                var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;
                accTangent[a] += tangent;
                accTangent[b] += tangent;
                accTangent[c] += tangent;
                accBitangent[a] += bitangent;
                accBitangent[b] += bitangent;
                accBitangent[c] += bitangent;
            };

            foreach (var drawCall in mesh.DrawCalls)
            {
                foreach (var face in drawCall.GetFaces())
                {
                    accumulateTangent(indices[face.A], indices[face.B], indices[face.C]);
                }
            }

            var tangents = new ListMeshVertexAttribute<Vector4>();

            for (int i = 0; i < accTangent.Length; i++)
            {
                var n = (normals != null) ? Vector3.Normalize(normals![i]) : Vector3.UnitZ;
                Vector3 t = Vector3.Normalize(accTangent[i]);
                Vector3 b = Vector3.Normalize(accBitangent[i]);
                if (t == Vector3.Zero)
                {
                    tangents.Add(new Vector4(0, 0, 1, 1));
                }
                else
                {
                    AddTangent(tangents, n, t, b);
                }
            }

            mesh.AddAttribute(MeshAttributeKey.Tangent, tangents);
        }

        private static int AddTangent(IMeshVertexAttribute<Vector4> tangents, Vector3 n, Vector3 t, Vector3 b)
        {
            // Gram-Schmidt orthogonalize
            t = Vector3.Normalize(t - n * Vector3.Dot(n, t));
            // Calculate handedness
            var handedness = (Vector3.Dot(Vector3.Cross(n, t), b) < 0.0f) ? -1.0f : 1.0f;
            var tangent = new Vector4(new Vector3(-t.X, t.Y, -t.Z), handedness);
            return tangents.Add(tangent);
        }
    }
}
