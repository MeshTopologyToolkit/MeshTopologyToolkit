using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit.Operators
{
    public class MergeOperator : IContentOperator
    {
        HashSet<Texture> _visitedTextures = new HashSet<Texture>();

        HashSet<Material> _visitedMaterials = new HashSet<Material>();

        FileContainer? _container;

        public FileContainer Transform(FileContainer container)
        {
            
            var result = new FileContainer();

            if (_container != null)
            {
                throw new NotImplementedException($"{this.GetType().Name} is not designed to be reused");
            }
            _container = container;

            foreach (var scene in container.Scenes)
            {
                _container.Scenes.Add(Transform(scene));
            }

            _container = null;

            return result;
        }

        public Scene Transform(Scene scene)
        {
            Scene resultScene = new Scene(scene.Name);
            Node resultNode = new Node();
            resultScene.AddChild(resultNode);
            resultNode.Mesh = Merge(scene.VisitAllChildren().Where(n => n.Mesh?.Mesh != null).ToList());
            return resultScene;
        }

        private MeshReference Merge(List<Node> nodes)
        {
            Material? defaultMaterial = null;
            var materials = new DictionaryMeshVertexAttribute<Material>();
            foreach (var m in nodes.SelectMany(n => n.Mesh!.Materials))
                materials.Add(m ?? defaultMaterial ?? (defaultMaterial = new Material("Default")));

            var mesh = new SeparatedIndexedMesh();

            var res = new MeshReference(mesh, materials);

            return res;
        }

        public IMesh Transform(IMesh mesh)
        {
            throw new NotImplementedException($"{this.GetType().Name} is not designed to operate on individual meshes");
        }

        public Material Transform(Material material)
        {
            if (_container == null)
            {
                throw new NotImplementedException($"{this.GetType().Name} is not designed to operate on individual materials");
            }
            return Visit(_visitedMaterials, material, _container.Materials);
        }

        public Texture Transform(Texture texture)
        {
            if (_container == null)
            {
                throw new NotImplementedException($"{this.GetType().Name} is not designed to operate on individual textures");
            }
            return Visit(_visitedTextures, texture, _container.Textures);
        }

        private T Visit<T>(HashSet<T> cache, T value, IList<T> collection)
        {
            if (value == null)
                return value;

            if (cache.Add(value))
            {
                collection.Add(value);
            }

            return value;

        }
    }
    public class EnsureTangentsOperator : ContentOperatorBase
    {
        private struct ComponentIndices
        {
            public int Position;
            public int TexCoord;
            public int Normal;
        }

        public override IMesh Transform(IMesh mesh)
        {
            mesh = base.Transform(mesh);

            if (mesh.HasAttribute(MeshAttributeKey.Tangent))
            {
                return mesh;
            }

            mesh = new EnsureNormalsOperator().Transform(mesh);

            if (mesh is SeparatedIndexedMesh separatedIndexedMesh)
            {
                EnsureSeparatedIndexedMeshTangents(separatedIndexedMesh);
                return separatedIndexedMesh;
            }
            else if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                EnsureUnifiedIndexedMeshTangents(unifiedIndexedMesh);
                return unifiedIndexedMesh;
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

            var normals = mesh.GetAttribute<Vector3>(MeshAttributeKey.Normal);
            var normalIndices = mesh.GetAttributeIndices(MeshAttributeKey.Normal);

            var uniqueVertexMap = new Dictionary<ComponentIndices, int>();
            var uniqueVertexNormals = new List<Vector3>();
            var tangentIndices = new int[positionIndices.Count];
            for (int i = 0; i < positionIndices.Count; i++)
            {
                var key = new ComponentIndices
                {
                    Position = positionIndices[i],
                    Normal = normalIndices[i],
                    TexCoord = texCoordIndices[i]
                };
                if (uniqueVertexMap.TryGetValue(key, out var index))
                {
                    tangentIndices[i] = index;
                }
                else
                {
                    tangentIndices[i] = uniqueVertexMap.Count;
                    uniqueVertexMap.Add(key, uniqueVertexMap.Count);
                    uniqueVertexNormals.Add(normals[key.Normal]);
                }

            }

            var accTangent = new Vector3[uniqueVertexMap.Count];
            var accBitangent = new Vector3[uniqueVertexMap.Count];

            var accumulateTangent = (int a, int b, int c) =>
            {
                var v0 = positions[positionIndices[a]];
                var v1 = positions[positionIndices[b]];
                var v2 = positions[positionIndices[c]];
                var uv0 = texCoords[texCoordIndices[a]];
                var uv1 = texCoords[texCoordIndices[b]];
                var uv2 = texCoords[texCoordIndices[c]];
                var deltaPos1 = v1 - v0;
                var deltaPos2 = v2 - v0;
                var deltaUV1 = (uv1 - uv0);// * new Vector2(1, -1);
                var deltaUV2 = (uv2 - uv0);// * new Vector2(1, -1);
                var r = 1.0f / (deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y);
                if (r.IsNanOrInf())
                    r = 0.0f;
                var tangent = (deltaPos1 * deltaUV2.Y - deltaPos2 * deltaUV1.Y) * r;
                var bitangent = (deltaPos2 * deltaUV1.X - deltaPos1 * deltaUV2.X) * r;
                accTangent[tangentIndices[a]] += tangent;
                accTangent[tangentIndices[b]] += tangent;
                accTangent[tangentIndices[c]] += tangent;
                accBitangent[tangentIndices[a]] += bitangent;
                accBitangent[tangentIndices[b]] += bitangent;
                accBitangent[tangentIndices[c]] += bitangent;
            };

            foreach (var face in mesh.GetFaces())
            {
                accumulateTangent(face.A, face.B, face.C);
            }

            var tangents = new TangentRTree3MeshVertexAttribute();
            var finalTangentIndices = new int[positionIndices.Count];
            for (int i = 0; i < accTangent.Length; i++)
            {
                var n = uniqueVertexNormals[i].NormalizedOrDefault(Vector3.UnitY);
                Vector3 t = accTangent[i].NormalizedOrDefault(Vector3.UnitX);
                Vector3 b = accBitangent[i].NormalizedOrDefault(Vector3.UnitZ);
                if (t == Vector3.Zero)
                {
                    finalTangentIndices[i] = tangents.Add(new Vector4(0, 0, 1, 1));
                }
                else
                {
                    finalTangentIndices[i] = AddTangent(tangents, n, t, b);
                }
            }

            mesh.AddAttribute(MeshAttributeKey.Tangent, tangents, tangentIndices.Select(i => finalTangentIndices[i]).ToList());
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

            foreach (var face in mesh.GetFaces())
            {
                accumulateTangent(indices[face.A], indices[face.B], indices[face.C]);
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
            t = (t - n * Vector3.Dot(n, t)).NormalizedOrDefault(Vector3.UnitX);
            // Calculate handedness
            var handedness = (Vector3.Dot(Vector3.Cross(n, t), b) < 0.0f) ? 1.0f : -1.0f;
            var tangent = new Vector4(new Vector3(t.X, t.Y, t.Z), handedness);
            return tangents.Add(tangent);
        }

    }
}
