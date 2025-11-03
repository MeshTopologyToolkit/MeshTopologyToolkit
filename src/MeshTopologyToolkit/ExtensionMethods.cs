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

        public static void EnsureTangents(this IMesh mesh)
        {
            if (mesh.HasAttribute(MeshAttributeKey.Tangent))
            {
                return;
            }   

            if (mesh is SeparatedIndexedMesh separatedIndexedMesh)
            {
                var positions = separatedIndexedMesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
                var positionIndices = separatedIndexedMesh.GetAttributeIndices(MeshAttributeKey.Position);
                if (!separatedIndexedMesh.TryGetAttribute<Vector2>(MeshAttributeKey.TexCoord, out var texCoords) ||
                    !separatedIndexedMesh.TryGetAttributeIndices(MeshAttributeKey.TexCoord, out var texCoordIndices))
                {
                    texCoords = new ConstMeshVertexAttribute<Vector2>(Vector2.Zero, 1);
                    texCoordIndices = new ConstMeshVertexAttribute<int>(0, positionIndices.Count);
                }
                IReadOnlyList<int>? normalIndices = null;
                if (!separatedIndexedMesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var normals) ||
                    !separatedIndexedMesh.TryGetAttributeIndices(MeshAttributeKey.Normal, out normalIndices))
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
                        accumulateTangent(face.A, face.B, face.C);
                    }
                }

                var tangentIndices = new int[positionIndices.Count];
                var tangents = new DictionaryMeshVertexAttribute<Vector4>();

                for (int i=0; i< accTangent.Length; i++)
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
                        // Gram-Schmidt orthogonalize
                        t = Vector3.Normalize(t - n * Vector3.Dot(n, t));
                        // Calculate handedness
                        var handedness = (Vector3.Dot(Vector3.Cross(n, t), b) < 0.0f) ? -1.0f : 1.0f;
                        var tangent = new Vector4(t, handedness);
                        tangentIndices[i] = tangents.Add(tangent);
                    }
                }

                separatedIndexedMesh.AddAttribute(MeshAttributeKey.Tangent, tangents, tangentIndices);
            }
            else if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                throw new Exception("Tangent generation for UnifiedIndexedMesh not implemented yet.");
            }
            else
            {
                throw new NotImplementedException($"Unknown mesh type. Only {nameof(SeparatedIndexedMesh)} and {nameof(UnifiedIndexedMesh)} supported.");
            }
        }
    }
}
