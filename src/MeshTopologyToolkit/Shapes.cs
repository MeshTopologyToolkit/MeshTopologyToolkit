using System.Numerics;

namespace MeshTopologyToolkit
{
    public class Shapes
    {
        public static IMesh BuildBox(float size, MeshAttributeMask mask)
        {
            var mesh = new SeparatedIndexedMesh();
            var radius = size * 0.5f;
            {
                var values = new ListMeshVertexAttribute<Vector3>();
                values.Add(new Vector3(-1.0f, -1.0f, 1.0f) * radius);
                values.Add(new Vector3(-1.0f, 1.0f, 1.0f) * radius);
                values.Add(new Vector3(-1.0f, 1.0f, -1.0f) * radius);
                values.Add(new Vector3(-1.0f, -1.0f, -1.0f) * radius);
                values.Add(new Vector3(1.0f, 1.0f, 1.0f) * radius);
                values.Add(new Vector3(1.0f, 1.0f, -1.0f) * radius);
                values.Add(new Vector3(1.0f, -1.0f, 1.0f) * radius);
                values.Add(new Vector3(1.0f, -1.0f, -1.0f) * radius);
                var indices = new int[] { 0, 1, 2, 0, 2, 3, 1, 4, 5, 1, 5, 2, 4, 6, 7, 4, 7, 5, 6, 0, 3, 6, 3, 7, 4, 1, 0, 4, 0, 6, 3, 2, 5, 7, 3, 5 };
                mesh.AddAttribute(MeshAttributeKey.Position, values, indices);
            }
            if ((mask & MeshAttributeMask.Normal) == MeshAttributeMask.Normal)
            {
                var values = new ListMeshVertexAttribute<Vector3>();
                values.Add(new Vector3(-1f, 0f, 0f));
                values.Add(new Vector3(0f, 1f, 0f));
                values.Add(new Vector3(1f, 0f, 0f));
                values.Add(new Vector3(0f, -1f, 0f));
                values.Add(new Vector3(0f, 0f, 1f));
                values.Add(new Vector3(0f, 0f, -1f));
                var indices = new int[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5 };
                mesh.AddAttribute(MeshAttributeKey.Normal, values, indices);
            }
            if ((mask & MeshAttributeMask.TexCoord) == MeshAttributeMask.TexCoord)
            {
                var values = new ListMeshVertexAttribute<Vector2>();
                values.Add(new Vector2(1f, 0f));
                values.Add(new Vector2(0f, 0f));
                values.Add(new Vector2(0f, 1f));
                values.Add(new Vector2(1f, 1f));
                var indices = new int[] { 0, 1, 2, 0, 2, 3, 0, 1, 2, 0, 2, 3, 0, 1, 2, 0, 2, 3, 0, 1, 2, 0, 2, 3, 0, 1, 2, 0, 2, 3, 3, 0, 1, 2, 3, 1 };
                mesh.AddAttribute(MeshAttributeKey.TexCoord, values, indices);
            }
            if ((mask & MeshAttributeMask.Color) == MeshAttributeMask.Color)
            {
                var values = new ConstMeshVertexAttribute<Vector4>(Vector4.One, 1);
                var indices = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                mesh.AddAttribute(MeshAttributeKey.Color, values, indices);
            }
            mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.TriangleList, 0, mesh.GetAttributeIndices(MeshAttributeKey.Position).Count));
            return mesh;
        }
    }
}
