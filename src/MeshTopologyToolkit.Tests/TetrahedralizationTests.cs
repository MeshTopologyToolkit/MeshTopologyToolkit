using MeshTopologyToolkit.Gltf;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class TetrahedralizationTests
{
    [Fact]
    public void TetrahedronCircumSphere()
    {
        var vertices = new Vector3d[]
        {
            new Vector3d(0,0,0),
            new Vector3d(1,0,0),
            new Vector3d(0,1,0),
            new Vector3d(0,0,1)
        };
        var t = new Tetrahedralization.Tetrahedron(0, 1, 2, 3, vertices);

        foreach (var vertex in vertices)
        {
            var l2 = (vertex - t.CircumCenter).LengthSquared();
            Assert.Equal(t.CircumRadiusSq, l2, 1e-6);
        }

        Vector3d expectedCenter = new(0.5, 0.5, 0.5);
        Assert.True(expectedCenter.Equals(t.CircumCenter, 1e-6), $"CircumCenter {t.CircumCenter} doesn't match expected {expectedCenter}");
        Assert.Equal(0.75, t.CircumRadiusSq);
    }

    [Fact]
    public void NoPoints()
    {
        var t = new Tetrahedralization();
        var res = t.Generate(new Vector3[] { });
        Assert.Empty(res);
    }

    [Fact]
    public void NotEnoughPoints()
    {
        var t = new Tetrahedralization();
        var res = t.Generate(new Vector3[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0) });
        Assert.Empty(res);
    }

    [Fact]
    public void FourPoints()
    {
        var t = new Tetrahedralization();
        Vector3[] vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) };
        var res = t.Generate(vertices);
        Assert.Single(res);
    }


    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(20)]
    public void GenerateNoise(int numVertices)
    {
        int steps = numVertices + 1;
        int totalIndices = steps * steps * steps;

        Vector3 IndexToPosition(int posIndex)
        {
            var x = (posIndex % steps);
            posIndex /= steps;
            var y = (posIndex % steps);
            posIndex /= steps;
            var z = posIndex;
            return new Vector3(x, y, z) * (10.0f / numVertices);
        }

        Assert.Equal(new Vector3(10, 10, 10), IndexToPosition(totalIndices - 1));

        var rnd = new Random(0);
        var positions = new ListMeshVertexAttribute<Vector3>();
        for (int i = 0; i < numVertices; ++i)
        {
            var posIndex = rnd.Next(totalIndices);
            positions.Add(IndexToPosition(posIndex));
        }
        var t = new Tetrahedralization();
        var res = t.Generate(positions);
        Assert.NotEmpty(res);

        var content = new FileContainer();
        var scene = new Scene();
        content.Scenes.Add(scene);

        var tetrahedrons = new List<TetrahedronHelper>();
        foreach (var tet in res)
        {
            var tetrahedron = new TetrahedronHelper(tet, positions);
            tetrahedrons.Add(tetrahedron);
            content.Meshes.Add(tetrahedron.Mesh);
            scene.AddChild(new Node { Mesh = new MeshReference(tetrahedron.Mesh), Transform = new TRSTransform(tetrahedron.Position) });
        }

        for (int i = 0; i < totalIndices; ++i)
        {
            var pos = IndexToPosition(i);
            var count = tetrahedrons.Where(_ => _.DotCoordinate(pos) > 1e-6f).Count();
            Assert.True(count <= 1, $"Position {pos} is inside of {count} tetrahedrons");
        }

        var fileFormat = new GltfFileFormat();
        fileFormat.TryWrite(new FileSystemEntry($"Tetrahedralization{numVertices}.glb"), content);
    }

    public class TetrahedronHelper
    {
        private ListMeshVertexAttribute<Vector3> _vertices;
        private List<Plane> _planes;
        private Vector3 _position;
        private List<int> _indices;
        private UnifiedIndexedMesh _mesh;

        public UnifiedIndexedMesh Mesh => _mesh;

        public Vector3 Position => _position;

        public TetrahedronHelper(Tetrahedralization.Tetrahedron tet, ListMeshVertexAttribute<Vector3> positions)
        {
            _vertices = new ListMeshVertexAttribute<Vector3>();
            _vertices.Add(positions[tet.A]);
            _vertices.Add(positions[tet.B]);
            _vertices.Add(positions[tet.C]);
            _vertices.Add(positions[tet.D]);

            _planes = new List<Plane>();

            _position = (_vertices[0] + _vertices[1] + _vertices[2] + _vertices[3]) * 0.25f;
            for (int i = 0; i < 4; i++)
            {
                _vertices[i] = _vertices[i] - _position;
            }
            _indices = new List<int>();
            for (int i = 0; i < 4; ++i)
            {
                var face = Enumerable.Range(0, 4).Where(_ => _ != i).ToList();

                var n = Vector3.Cross(_vertices[face[1]] - _vertices[face[0]], _vertices[face[2]] - _vertices[face[0]]);
                var nl = n.Length();
                Assert.True(nl > 1e-6f, "Empty tetrahedrons should be eliminated by algorithm");
                n = Vector3.Normalize(n);
                if (Vector3.Dot(n, _vertices[face[0]]) < 0.0f)
                {
                    Plane plane = new(n, -Vector3.Dot(_vertices[face[0]], n));
                    Assert.True(Plane.DotCoordinate(plane, Vector3.Zero) > 0);
                    _planes.Add(plane);

                    _indices.Add(face[0]);
                    _indices.Add(face[1]);
                    _indices.Add(face[2]);
                }
                else
                {
                    n = -n;
                    Plane plane = new(n, -Vector3.Dot(_vertices[face[0]], n));
                    Assert.True(Plane.DotCoordinate(plane, Vector3.Zero) > 0);
                    _planes.Add(plane);
                    _indices.Add(face[0]);
                    _indices.Add(face[2]);
                    _indices.Add(face[1]);
                }
            }
            _mesh = new UnifiedIndexedMesh(_indices, (MeshAttributeKey.Position, _vertices)).WithTriangleList();

        }

        public float DotCoordinate(Vector3 pos)
        {
            return _planes.Select(p => Plane.DotCoordinate(p, pos + _position)).Min();
        }
    }

}
