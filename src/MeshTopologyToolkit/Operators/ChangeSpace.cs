using System;
using System.Numerics;

namespace MeshTopologyToolkit.Operators
{
    public class ChangeSpace : ContentOperatorBase
    {
        public ChangeSpace(Matrix4x4 transoform, bool flipV = false, bool flipFaceIndices = false)
        {
            if (transoform != Matrix4x4.Identity)
                throw new NotImplementedException("ChangeSpace operator currently only supports identity transform.");
            if (flipFaceIndices)
                throw new NotImplementedException("ChangeSpace operator currently does not support flipping face indices.");

            FlipV = flipV;
        }

        public bool FlipV { get; private set; }

        public override IMesh Transform(IMesh mesh)
        {
            if (!mesh.TryGetAttribute<Vector2>(MeshAttributeKey.TexCoord, out var texCoords))
                return mesh;

            if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                var res = new UnifiedIndexedMesh(unifiedIndexedMesh.Name);
                foreach (var key in mesh.GetAttributeKeys())
                {
                    if (FlipV && key.Name == MeshAttributeNames.TexCoord)
                    {
                        var data = new ListMeshVertexAttribute<Vector2>(texCoords.Count);
                        foreach (var uv in texCoords)
                        {
                            var newUv = uv;
                            newUv.Y = 1.0f - newUv.Y;
                            data.Add(newUv);
                        }
                        res.SetAttribute(key, data);
                    }
                    else
                    {
                        res.SetAttribute(key, mesh.GetAttribute(key));
                    }
                }

                res.AddIndices(unifiedIndexedMesh.Indices);
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

                    if (FlipV && key.Name == MeshAttributeNames.TexCoord)
                    {
                        var data = new ListMeshVertexAttribute<Vector2>(texCoords.Count);
                        foreach (var uv in texCoords)
                        {
                            var newUv = uv;
                            newUv.Y = 1.0f - newUv.Y;
                            data.Add(newUv);
                        }
                        res.SetAttribute(key, data, separatedIndexedMesh.GetAttributeIndices(key));
                    }
                    else
                    {
                        res.SetAttribute(key, separatedIndexedMesh.GetAttribute(key), separatedIndexedMesh.GetAttributeIndices(key));
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
    }
}
