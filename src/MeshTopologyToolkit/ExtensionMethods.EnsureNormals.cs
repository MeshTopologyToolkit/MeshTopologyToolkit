using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// EnsureNormals extension methods and it's implementation details.
    /// </summary>
    public static partial class ExtensionMethods
    {
        public static void EnsureNormals(this IMesh mesh)
        {

            if (mesh.HasAttribute(MeshAttributeKey.Normal))
            {
                return;
            }

            var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
            var indices = mesh.GetAttributeIndices(MeshAttributeKey.Position);
            var values = new Vector3[positions.Count];
            var normals = new ReadOnlyListMeshVertexAttribute<Vector3>(values);
            foreach (var face in mesh.GetFaces())
            {
                var a = positions[indices[face.A]];
                var b = positions[indices[face.B]];
                var c = positions[indices[face.C]];
                var normal = Vector3.Cross(a - c, b - c);
                var len = normal.Length();
                if (len > float.Epsilon)
                {
                    normal = normal * (1.0f / len);
                    values[indices[face.A]] += normal;
                    values[indices[face.B]] += normal;
                    values[indices[face.C]] += normal;
                }
            }
            for (int index = 0; index < values.Length; index++)
            {
                Vector3 normal = values[index];
                var len = normal.Length();
                if (len > float.Epsilon)
                {
                    normal = normal * (1.0f / len);
                }
                else
                {
                    normal = Vector3.Zero;
                }
                values[index] = normal;
            }

            if (mesh is SeparatedIndexedMesh separatedIndexedMesh)
            {
                separatedIndexedMesh.AddAttribute(MeshAttributeKey.Normal, normals, indices);
            }
            else if (mesh is UnifiedIndexedMesh unifiedIndexedMesh)
            {
                unifiedIndexedMesh.AddAttribute(MeshAttributeKey.Normal, normals);
            }
            else
            {
                throw new NotImplementedException($"Unknown mesh type. Only {nameof(SeparatedIndexedMesh)} and {nameof(UnifiedIndexedMesh)} supported.");
            }
        }
    }
}