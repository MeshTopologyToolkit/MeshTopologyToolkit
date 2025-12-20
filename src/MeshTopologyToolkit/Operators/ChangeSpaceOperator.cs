using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Operators
{
    public class ChangeSpaceOperator : ContentOperatorBase
    {
        Matrix4x4 _transform;
        public ChangeSpaceOperator(SpaceTransform transform)
        {
            _transform = transform.Transform;
            FlipV = transform.FlipV;
            FlipFaceIndices = transform.FlipFaceIndices;
        }

        public Matrix4x4 Transoform => _transform;

        public bool FlipV { get; private set; }
        public bool FlipFaceIndices { get; }

        public override Node Transform(Node node)
        {
            var res = base.Transform(node);
            if (_transform != Matrix4x4.Identity)
            {
                var m = res.Transform.ToMatrix();
                res.Transform = new MatrixTransform(m * _transform);
            }
            return res;
        }

        public override IMesh Transform(IMesh mesh)
        {
            if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                var res = new UnifiedIndexedMesh(unifiedIndexedMesh.Name);
                foreach (var key in mesh.GetAttributeKeys())
                {
                    switch (key.Name)
                    {
                        case MeshAttributeNames.Position:
                            {
                                var positions = mesh.GetAttribute<Vector3>(key);
                                var data = new ListMeshVertexAttribute<Vector3>(positions.Count);
                                foreach (var position in positions)
                                {
                                    data.Add(Vector3.Transform(position, _transform));
                                }
                                res.SetAttribute(key, data);
                            }
                            break;
                        case MeshAttributeNames.Normal:
                            {
                                var normals = mesh.GetAttribute<Vector3>(key);
                                var data = new ListMeshVertexAttribute<Vector3>(normals.Count);
                                foreach (var position in normals)
                                {
                                    data.Add(Vector3.Normalize(Vector3.TransformNormal(position, _transform)));
                                }
                                res.SetAttribute(key, data);
                            }
                            break;
                        case MeshAttributeNames.Tangent:
                            {
                                var attr = mesh.GetAttribute(key);
                                if (attr.GetElementType() == typeof(Vector4))
                                {
                                    var tangents = mesh.GetAttribute<Vector4>(key);
                                    var data = new ListMeshVertexAttribute<Vector4>(tangents.Count);
                                    foreach (var tangent in tangents)
                                    {
                                        var t = new Vector3(tangent.X, tangent.Y, tangent.Z);
                                        t = Vector3.Normalize(Vector3.TransformNormal(t, _transform));
                                        data.Add(new Vector4(t, tangent.W));
                                    }
                                    res.SetAttribute(key, data);
                                }
                                else
                                {
                                    var tangents = mesh.GetAttribute<Vector3>(key);
                                    var data = new ListMeshVertexAttribute<Vector3>(tangents.Count);
                                    foreach (var tangent in tangents)
                                    {
                                        data.Add(Vector3.Normalize(Vector3.TransformNormal(tangent, _transform)));
                                    }
                                    res.SetAttribute(key, data);
                                }
                            }
                            break;
                        case MeshAttributeNames.TexCoord:
                            if (FlipV)
                            {
                                var texCoords = mesh.GetAttribute<Vector2>(key);
                                var data = new ListMeshVertexAttribute<Vector2>(texCoords.Count);
                                foreach (var uv in texCoords)
                                {
                                    var newUv = uv;
                                    newUv.Y = 1.0f - newUv.Y;
                                    data.Add(newUv);
                                }
                                res.SetAttribute(key, data);
                            }
                            break;
                        default:
                            res.SetAttribute(key, mesh.GetAttribute(key));
                            break;
                    }
                }

                res.AddIndices(TransformIndices((IReadOnlyList<int>)unifiedIndexedMesh.Indices, unifiedIndexedMesh.DrawCalls));
                foreach (var drawCall in mesh.DrawCalls)
                {
                    res.DrawCalls.Add(drawCall.Clone());
                }
                return res;
            }
            else if (mesh is SeparatedIndexedMesh separatedIndexedMesh)
            {
                var res = new SeparatedIndexedMesh(separatedIndexedMesh.Name);
                foreach (var key in mesh.GetAttributeKeys())
                {
                    switch (key.Name)
                    {
                        case MeshAttributeNames.Position:
                            {
                                var positions = separatedIndexedMesh.GetAttribute<Vector3>(key);
                                var data = new ListMeshVertexAttribute<Vector3>(positions.Count);
                                foreach (var uv in positions)
                                {
                                    data.Add(Vector3.Transform(uv, _transform));
                                }
                                res.SetAttribute(key, data, TransformIndices(separatedIndexedMesh.GetAttributeIndices(key), separatedIndexedMesh.DrawCalls));
                            }
                            break;
                        case MeshAttributeNames.Normal:
                            {
                                var normal = separatedIndexedMesh.GetAttribute<Vector3>(key);
                                var data = new ListMeshVertexAttribute<Vector3>(normal.Count);
                                foreach (var uv in normal)
                                {
                                    data.Add(Vector3.Normalize(Vector3.TransformNormal(uv, _transform)));
                                }
                                res.SetAttribute(key, data, TransformIndices(separatedIndexedMesh.GetAttributeIndices(key), separatedIndexedMesh.DrawCalls));
                            }
                            break;
                        case MeshAttributeNames.TexCoord:
                            if (FlipV)
                            {
                                var texCoords = separatedIndexedMesh.GetAttribute<Vector2>(key);
                                var data = new ListMeshVertexAttribute<Vector2>(texCoords.Count);
                                foreach (var uv in texCoords)
                                {
                                    var newUv = uv;
                                    newUv.Y = 1.0f - newUv.Y;
                                    data.Add(newUv);
                                }
                                res.SetAttribute(key, data, TransformIndices(separatedIndexedMesh.GetAttributeIndices(key), separatedIndexedMesh.DrawCalls));
                            }
                            else
                            {
                                res.SetAttribute(key, separatedIndexedMesh.GetAttribute(key), TransformIndices(separatedIndexedMesh.GetAttributeIndices(key), separatedIndexedMesh.DrawCalls));
                            }
                            break;
                    }
                }

                foreach (var drawCall in mesh.DrawCalls)
                {
                    res.DrawCalls.Add(drawCall.Clone());
                }

                return res;
            }

            throw new NotSupportedException($"ChangeSpace operator does not support mesh type {mesh.GetType().FullName}");
        }

        private IReadOnlyList<int> TransformIndices(IReadOnlyList<int> originalIndices, IList<MeshDrawCall> drawCalls)
        {
            if (!FlipFaceIndices)
                return originalIndices;

            var indices = Copy(originalIndices);
            foreach (var drawCall in drawCalls)
            {
                switch (drawCall.Type)
                {
                    case MeshTopology.Points:
                    case MeshTopology.LineList:
                    case MeshTopology.LineLoop:
                    case MeshTopology.LineStrip:
                        break;
                    case MeshTopology.TriangleList:
                        {
                            for (int i = 0; i < drawCall.NumIndices; i += 3)
                            {
                                int idx0 = drawCall.StartIndex + i;
                                int idx1 = drawCall.StartIndex + i + 1;
                                int idx2 = drawCall.StartIndex + i + 2;
                                // Swap idx1 and idx2 to flip the triangle
                                int temp = indices[idx1];
                                indices[idx1] = indices[idx2];
                                indices[idx2] = temp;
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Flipping indices for {drawCall.Type} is not implemented.");
                }
            }
            return indices;
        }

        private int[] Copy(IReadOnlyList<int> indices)
        {
            var copy = new int[indices.Count];
            for (int i = 0; i < indices.Count; ++i)
            {
                copy[i] = indices[i];
            }
            return copy;
        }
    }
}
