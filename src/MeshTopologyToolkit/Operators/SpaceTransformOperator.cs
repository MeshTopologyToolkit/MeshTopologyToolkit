using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;

namespace MeshTopologyToolkit.Operators
{
    public class SpaceTransformOperator : ContentOperatorBase
    {
        private SpaceTransformer _transformer;

        public SpaceTransformOperator(SpaceTransform transform)
        {
            _transformer = transform.Transformer;
            FlipV = transform.FlipV;
            FlipFaceIndices = transform.FlipFaceIndices;
        }

        public SpaceTransformer Transformer => _transformer;

        public bool FlipV { get; private set; }

        public bool FlipFaceIndices { get; }

        public override ITransform Transform(ITransform transform)
        {
            return _transformer.TransformNode(transform);
        }

        public override IMesh Transform(IMesh mesh)
        {
            if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                var res = new UnifiedIndexedMesh(unifiedIndexedMesh.Name);
                foreach (var key in mesh.GetAttributeKeys())
                {
                    var data = TransformValues(key.Name, unifiedIndexedMesh.GetAttribute(key));
                    res.SetAttribute(key, data);
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
                    var data = TransformValues(key.Name, separatedIndexedMesh.GetAttribute(key));
                    res.SetAttribute(key, data, TransformIndices(separatedIndexedMesh.GetAttributeIndices(key), separatedIndexedMesh.DrawCalls));
                }

                foreach (var drawCall in mesh.DrawCalls)
                {
                    res.DrawCalls.Add(drawCall.Clone());
                }

                return res;
            }

            throw new NotSupportedException($"ChangeSpace operator does not support mesh type {mesh.GetType().FullName}");
        }

        private IMeshVertexAttribute? TransformValues(string name, IMeshVertexAttribute meshVertexAttribute)
        {
            switch (name)
            {
                case MeshAttributeNames.Position:
                    {
                        if (!meshVertexAttribute.TryCast<Vector3>(MeshVertexAttributeConverterProvider.Default, out var positions))
                            return null;
                        var data = new ListMeshVertexAttribute<Vector3>(positions.Count);
                        foreach (var position in positions)
                        {
                            data.Add(_transformer.TransformPosition(position));
                        }
                        return data;
                    }
                case MeshAttributeNames.Normal:
                    {
                        if (!meshVertexAttribute.TryCast<Vector3>(MeshVertexAttributeConverterProvider.Default, out var normals))
                            return null;
                        var data = new ListMeshVertexAttribute<Vector3>(normals.Count);
                        foreach (var normal in normals)
                        {
                            data.Add(_transformer.TransformNormal(normal));
                        }
                        return data;
                    }
                case MeshAttributeNames.Tangent:
                    {
                        if (meshVertexAttribute.GetElementType() == typeof(Vector4))
                        {
                            if (!meshVertexAttribute.TryCast<Vector4>(MeshVertexAttributeConverterProvider.Default, out var tangents))
                                return null;
                            var data = new ListMeshVertexAttribute<Vector4>(tangents.Count);
                            foreach (var tangent in tangents)
                            {
                                data.Add(_transformer.TransformTangent(tangent));
                            }
                            return data;
                        }
                        else
                        {
                            if (!meshVertexAttribute.TryCast<Vector3>(MeshVertexAttributeConverterProvider.Default, out var tangents))
                                return null;
                            var data = new ListMeshVertexAttribute<Vector3>(tangents.Count);
                            foreach (var tangent in tangents)
                            {
                                data.Add(_transformer.TransformNormal(tangent));
                            }
                            return data;
                        }
                    }
                case MeshAttributeNames.TexCoord:
                    if (FlipV)
                    {
                        if (!meshVertexAttribute.TryCast<Vector2>(MeshVertexAttributeConverterProvider.Default, out var texCoords))
                            return null;
                        var data = new ListMeshVertexAttribute<Vector2>(texCoords.Count);
                        foreach (var uv in texCoords)
                        {
                            var newUv = uv;
                            newUv.Y = 1.0f - newUv.Y;
                            data.Add(newUv);
                        }
                        return data;
                    }
                    else
                    {
                        return meshVertexAttribute;
                    }
                default:
                    return meshVertexAttribute;
            }
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
