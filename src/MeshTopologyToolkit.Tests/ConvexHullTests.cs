using MeshTopologyToolkit.Gltf;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class ConvexHullTests
{
    private static Vector3[] CubeCorners() => new[]
    {
        new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0),
        new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1),
    };

    [Fact]
    public void Tetrahedron()
    {
        var hull = new ConvexHull();
        hull.Build(new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) });
        Assert.Equal(4, hull.Vertices.Count);
        Assert.Equal(4, hull.Faces.Count);
        Assert.True(hull.Check());
    }

    [Fact]
    public void CubeWithInteriorPoints()
    {
        var points = CubeCorners().ToList();
        var rnd = new Random(1);
        for (int i = 0; i < 50; ++i)
        {
            points.Add(new Vector3(0.1f + 0.8f * rnd.NextSingle(), 0.1f + 0.8f * rnd.NextSingle(), 0.1f + 0.8f * rnd.NextSingle()));
        }

        var hull = new ConvexHull();
        hull.Build(points);
        Assert.Equal(8, hull.Vertices.Count);
        Assert.Equal(6, hull.Faces.Count);
        Assert.All(hull.Faces, f => Assert.Equal(4, f.Length));
        Assert.All(hull.VertexPointIndices, i => Assert.InRange(i, 0, 7));
        Assert.True(hull.Check());

        hull.Triangulate();
        Assert.Equal(12, hull.Faces.Count);
        Assert.All(hull.Faces, f => Assert.Equal(3, f.Length));
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(100, false)]
    [InlineData(1000, false)]
    [InlineData(100, true)]
    [InlineData(1000, true)]
    public void RandomPoints(int count, bool onSphere)
    {
        var rnd = new Random(count);
        var points = new List<Vector3>();
        while (points.Count < count)
        {
            var p = new Vector3(rnd.NextSingle() * 2 - 1, rnd.NextSingle() * 2 - 1, rnd.NextSingle() * 2 - 1);
            var l = p.Length();
            if (l > 1 || l < 1e-3f)
                continue;
            points.Add(onSphere ? p / l : p);
        }

        var hull = new ConvexHull();
        hull.Build(points);
        Assert.True(hull.Check());

        var mesh = hull.ToMesh();
        Assert.True(mesh.Indices.Count >= 12);
        AssertOutwardAndContainsAll(mesh, points);

        hull.Triangulate();
        Assert.All(hull.Faces, f => Assert.Equal(3, f.Length));
    }

    [Fact]
    public void ToMeshFlatNormals()
    {
        var hull = new ConvexHull();
        hull.Build(CubeCorners());
        var mesh = hull.ToMesh(flatNormals: true);

        Assert.Equal(36, mesh.Indices.Count);
        Assert.True(mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var normals));
        Assert.True(mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions));
        Assert.Equal(positions.Count, normals.Count);
        var center = new Vector3(0.5f);
        for (int i = 0; i < positions.Count; ++i)
        {
            Assert.Equal(1.0f, normals[i].Length(), 1e-4f);
            Assert.True(Vector3.Dot(normals[i], positions[i] - center) > 0);
        }
    }

    [Fact]
    public void ShapesBuildConvexHull()
    {
        var mesh = Shapes.BuildConvexHull(CubeCorners());
        Assert.True(mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out _));
        Assert.Equal(12, mesh.GetAttributeIndices(MeshAttributeKey.Position).Count / 3);

        var content = new FileContainer();
        var scene = new Scene();
        content.Add(scene);
        content.Add(mesh);
        scene.AddChild(new Node { Mesh = new MeshReference(mesh) });
        new GltfFileFormat().TryWrite(new FileSystemEntry("ConvexHull.glb"), content);
    }

    [Fact]
    public void MeshExtensionBuildConvexHull()
    {
        // a sphere mesh has many vertices; its hull must stay inside the same bounds and be convex
        var sphere = Shapes.BuildSphere(8);
        var hullMesh = sphere.BuildConvexHull();

        var spherePositions = sphere.GetAttribute<Vector3>(MeshAttributeKey.Position);
        Assert.True(hullMesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var hullPositions));
        Assert.True(hullPositions.Count <= spherePositions.Count);
        AssertOutwardAndContainsAll((UnifiedIndexedMesh)hullMesh, Enumerable.Range(0, spherePositions.Count).Select(i => spherePositions[i]).ToList());

        var box = Shapes.BuildBox().BuildConvexHull(flatNormals: true);
        Assert.Equal(12, box.GetAttributeIndices(MeshAttributeKey.Position).Count / 3);
        Assert.True(box.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out _));
    }

    [Fact]
    public void TooFewPoints()
    {
        var hull = new ConvexHull();
        Assert.Throws<ArgumentException>(() => hull.Build(new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0) }));
    }

    [Fact]
    public void CoincidentPoints()
    {
        var hull = new ConvexHull();
        Assert.Throws<ArgumentException>(() => hull.Build(new[] { Vector3.One, Vector3.One, Vector3.One, Vector3.One, Vector3.One }));
    }

    [Fact]
    public void ColinearPoints()
    {
        var hull = new ConvexHull();
        var points = Enumerable.Range(0, 10).Select(i => new Vector3(i, i, i)).ToArray();
        Assert.Throws<ArgumentException>(() => hull.Build(points));
    }

    [Fact]
    public void CoplanarPoints()
    {
        var hull = new ConvexHull();
        var points = new List<Vector3>();
        for (int x = 0; x < 4; ++x)
            for (int y = 0; y < 4; ++y)
                points.Add(new Vector3(x, y, 0));
        Assert.Throws<ArgumentException>(() => hull.Build(points));
    }

    [Fact]
    public void RebuildReusesInstance()
    {
        var hull = new ConvexHull();
        hull.Build(CubeCorners());
        Assert.Equal(8, hull.Vertices.Count);
        hull.Build(new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) });
        Assert.Equal(4, hull.Vertices.Count);
        Assert.Equal(4, hull.Faces.Count);
    }

    private static void AssertOutwardAndContainsAll(UnifiedIndexedMesh mesh, IReadOnlyList<Vector3> points)
    {
        Assert.True(mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions));
        var centroid = Vector3.Zero;
        for (int i = 0; i < positions.Count; ++i)
            centroid += positions[i];
        centroid /= positions.Count;

        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            var a = positions[mesh.Indices[i]];
            var b = positions[mesh.Indices[i + 1]];
            var c = positions[mesh.Indices[i + 2]];
            var n = Vector3.Cross(b - a, c - a);
            if (n.LengthSquared() < 1e-12f)
                continue;
            n = Vector3.Normalize(n);
            Assert.True(Vector3.Dot(n, a - centroid) > 0, "triangle faces inwards");
            foreach (var p in points)
            {
                Assert.True(Vector3.Dot(n, p - a) < 1e-4f, "point outside of hull");
            }
        }
    }
}
