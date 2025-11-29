using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class BoxBuilder
    {
        private UnifiedIndexedMesh _mesh;

        public BoxBuilder()
        {
            _mesh = new UnifiedIndexedMesh();
            Positions = new ListMeshVertexAttribute<Vector3>();
            Normals = new ListMeshVertexAttribute<Vector3>();
            TexCoords = new ListMeshVertexAttribute<Vector2>();

            _mesh.AddAttribute(MeshAttributeKey.Position, Positions);
            _mesh.AddAttribute(MeshAttributeKey.Normal, Normals);
            _mesh.AddAttribute(MeshAttributeKey.TexCoord, TexCoords);
        }

        public ListMeshVertexAttribute<Vector3> Positions { get; internal set; }
        public ListMeshVertexAttribute<Vector3> Normals { get; internal set; }
        public ListMeshVertexAttribute<Vector2> TexCoords { get; internal set; }
        public IList<int> Indices => _mesh.Indices;

        public UnifiedIndexedMesh Build()
        {
            _mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, _mesh.Indices.Count));
            _mesh.EnsureTangents();

            return _mesh;
        }
    }
}