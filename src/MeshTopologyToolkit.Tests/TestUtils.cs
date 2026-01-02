using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public static class TestUtils
{

    public static UnifiedIndexedMesh BuildPointMesh(Vector3 point)
    {
        return BuildPointMesh(point, Vector3.UnitZ);
    }

    public static UnifiedIndexedMesh BuildPointMesh(Vector3 point, Vector3 normal)
    {
        var mesh = new UnifiedIndexedMesh();
        IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>() { point };
        mesh.AddAttribute(MeshAttributeKey.Position, positions);
        IMeshVertexAttribute<Vector3> normals = new ListMeshVertexAttribute<Vector3>() { normal };
        mesh.AddAttribute(MeshAttributeKey.Normal, normals);
        var indices = mesh.Indices;
        indices.Add(0);
        mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.Points, 0, 1));
        return mesh;
    }

    public static FileContainer BuildSingleNodeScene(SpaceTransform contentTransform, ITransform transform, IMesh mesh)
    {
        var content = new FileContainer();
        content.FileToGltfTransform = contentTransform;
        var scene = new Scene();
        content.Add(scene);
        var node = new Node() { Transform = transform };
        scene.AddChild(node);
        node.Mesh = new MeshReference(mesh);
        content.Add(mesh);
        return content;
    }

    public static FileContainer BuildChildParentScene(SpaceTransform contentTransform, ITransform parentTransform, ITransform childTransform, IMesh mesh)
    {
        var content = new FileContainer();
        content.FileToGltfTransform = contentTransform;
        var scene = new Scene();
        content.Add(scene);
        var parentNode = new Node() { Transform = parentTransform };
        scene.AddChild(parentNode);
        var childNode = new Node() { Transform = childTransform };
        parentNode.AddChild(childNode);
        childNode.Mesh = new MeshReference(mesh);
        content.Add(mesh);
        return content;
    }

    internal static void PrintScene(ITestOutputHelper testOutput, Scene scene)
    {
        testOutput.WriteLine($"Scene {scene.Name}:");
        PrintNodes(testOutput, scene.Children, "  ");
    }

    private static void PrintNodes(ITestOutputHelper testOutput, IReadOnlyList<Node> children, string indent = "")
    {
        foreach (var child in children)
        {
            testOutput.WriteLine($"{indent}Node: {child.Name}");
            if (child.Transform.IsIdentity)
            {
                testOutput.WriteLine($"{indent}  Transform: Identity");
            }
            else if (child.Transform is TRSTransform trs)
            {
                testOutput.WriteLine($"{indent}  Translation: {trs.Translation}");
                testOutput.WriteLine($"{indent}  Rotation: {trs.Rotation}");
                testOutput.WriteLine($"{indent}  Scale: {trs.Scale}");
            }
            else if (child.Transform is MatrixTransform mat)
            {
                var m = mat.Transform;
                testOutput.WriteLine($"{indent}  X: {m.M11}, {m.M12}, {m.M13}");
                testOutput.WriteLine($"{indent}  Y: {m.M21}, {m.M22}, {m.M23}");
                testOutput.WriteLine($"{indent}  Z: {m.M31}, {m.M32}, {m.M33}");
                testOutput.WriteLine($"{indent}  Translation: {m.Translation}");
            }
            if (child.Mesh?.Mesh != null)
            {
                var m = child.Mesh.Mesh;
                var positions = m.GetAttribute<Vector3>(MeshAttributeKey.Position);
                testOutput.WriteLine($"{indent}  Mesh:");
                testOutput.WriteLine($"{indent}    Positions: {positions.Count}");
                if (positions.Count > 0)
                    testOutput.WriteLine($"{indent}               {positions[0]} ...");
            }
            PrintNodes(testOutput, child.Children, indent + "  ");
        }
    }
}
