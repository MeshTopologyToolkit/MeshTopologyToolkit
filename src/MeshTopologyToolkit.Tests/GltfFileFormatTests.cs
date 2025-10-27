﻿using MeshTopologyToolkit.Gltf;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class GltfFileFormatTests
{
    private readonly ITestOutputHelper _testOutput;

    public GltfFileFormatTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
    }

    [Fact]
    public void TwoCorners()
    {
        var fileFormat = new GltfFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(this.GetType().Assembly, "MeshTopologyToolkit.Tests.samples.corner.TwoCorners.glb"), out var content));

        Assert.NotNull(content);
        Assert.Equal(2, content.Meshes.Count);
    }

    public static IEnumerable<object[]> EnumerateGltfFiles
    {
        get
        {
            var res = new List<object[]>();
            try
            {
                var rootFolder = @"C:\github\glTF-Sample-Models\2.0";
                if (Directory.Exists(rootFolder))
                {
                    foreach (var fileName in Directory.GetFiles(rootFolder, "*.gltf", SearchOption.AllDirectories))
                    {
                        if (!fileName.Contains("-Draco"))
                            res.Add(new[] { fileName });
                    }

                    foreach (var fileName in Directory.GetFiles(rootFolder, "*.glb", SearchOption.AllDirectories))
                    {
                        if (!fileName.Contains("-Draco"))
                            res.Add(new[] { fileName });
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return res;
        }

    }

    [Theory]
    [InlineData("samples.corner.TwoCorners.glb")]
    [InlineData("samples.kronos.SimpleInstancing.glb")]
    [InlineData("samples.kronos.SimpleMeshes.gltf")]
    [InlineData("samples.kronos.SimpleMorph.gltf")]
    [InlineData("samples.kronos.SimpleSkin.gltf")]
    [InlineData("samples.kronos.VC.glb")]
    [InlineData("samples.primitives.Primitives.glb")]
    public void ReadAndWriteSamples(string fileName)
    {
        var resourceName = this.GetType().Namespace + "." + fileName;

        var fileFormat = new GltfFileFormat();

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName), out var content));
        Assert.NotNull(content);

        fileFormat.TryWrite(new FileSystemEntry(Path.GetFileNameWithoutExtension(fileName) + ".glb"), content);
    }

    [Fact]
    public void GenerateShapeCode()
    {
        var resourceName = this.GetType().Namespace + ".samples.primitives.Primitives.glb";

        var fileFormat = new GltfFileFormat();

        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(resourceName), out var content));
        Assert.NotNull(content);

        foreach (var mesh in content.Meshes)
        {
            _testOutput.WriteLine($"public static IMesh Build{mesh.Name}(float size)");
            _testOutput.WriteLine("{");
            _testOutput.WriteLine($"    var mesh = new {nameof(SeparatedIndexedMesh)}();");
            _testOutput.WriteLine($"    var radius = size * 0.5f;");

            void printElement(MeshAttributeKey key, float scale = 1, bool scaled = false)
            {
                var positions = mesh.GetAttribute(key);
                _testOutput.WriteLine("    {");

                if (positions is IMeshVertexAttribute<Vector3> vec3Attr)
                {
                    _testOutput.WriteLine($"        var values = new ListMeshVertexAttribute<Vector3>();");
                    foreach (var pos in vec3Attr)
                    {
                        var adjPos = pos * scale;
                        _testOutput.WriteLine($"        values.Add(new Vector3({adjPos.X}f, {adjPos.Y}f, {adjPos.Z}f){(scaled ? " * radius":"")});");
                    }
                }
                else if (positions is IMeshVertexAttribute<Vector4> vec4Attr)
                {
                    _testOutput.WriteLine($"        var values = new ListMeshVertexAttribute<Vector4>();");
                    foreach (var pos in vec4Attr)
                    {
                        var adjPos = pos * scale;
                        _testOutput.WriteLine($"        values.Add(new Vector4({adjPos.X}f, {adjPos.Y}f, {adjPos.Z}f, {adjPos.W}f){(scaled ? " * radius" : "")});");
                    }
                }
                else if (positions is IMeshVertexAttribute<Vector2> vec2Attr)
                {
                    _testOutput.WriteLine($"        var values = new ListMeshVertexAttribute<Vector2>();");
                    foreach (var pos in vec2Attr)
                    {
                        var adjPos = pos * scale;
                        _testOutput.WriteLine($"        values.Add(new Vector2({adjPos.X}f, {adjPos.Y}f){(scaled ? " * radius" : "")});");
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }

                var indices = string.Join(", ", mesh.GetAttributeIndices(key));
                _testOutput.WriteLine($"        var indices = new int[] {{{indices}}};");

                var camelKey = key.ToString();
                camelKey = camelKey.Substring(0, 1) + camelKey.Substring(1).ToLowerInvariant();
                _testOutput.WriteLine($"        mesh.AddAttribute(MeshAttributeKey.{camelKey}, values, indices);");

                _testOutput.WriteLine("    }");
            }

            printElement(MeshAttributeKey.Position, 10f, true);
            _testOutput.WriteLine("    if ((mask & MeshAttributeMask.Normal) == MeshAttributeMask.Normal)");
            printElement(MeshAttributeKey.Normal);

            if (mesh.HasAttribute(MeshAttributeKey.Tangent))
            {
                _testOutput.WriteLine($"    if ((mask & MeshAttributeMask.{MeshAttributeMask.Tangent}) == MeshAttributeMask.{MeshAttributeMask.Tangent})");
                printElement(MeshAttributeKey.Tangent);
            }

            _testOutput.WriteLine($"    if ((mask & MeshAttributeMask.{MeshAttributeMask.TexCoord}) == MeshAttributeMask.{MeshAttributeMask.TexCoord})");
            printElement(MeshAttributeKey.TexCoord);

            {
                _testOutput.WriteLine($"    if ((mask & MeshAttributeMask.{MeshAttributeMask.Color}) == MeshAttributeMask.{MeshAttributeMask.Color})");
                _testOutput.WriteLine("    {");

                _testOutput.WriteLine($"        var values = new ConstMeshVertexAttribute<Vector4>(Vector4.One, 1);");
                var indices = string.Join(", ", mesh.GetAttributeIndices(MeshAttributeKey.Position).Select(_ => 0));

                _testOutput.WriteLine($"        var indices = new int[] {{{indices}}};");
                _testOutput.WriteLine($"        mesh.AddAttribute(MeshAttributeKey.Color, values, indices);");
                _testOutput.WriteLine("    }");
            }

            _testOutput.WriteLine($"    mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.TriangleList, 0, mesh.GetAttributeIndices(MeshAttributeKey.Position).Count));");

            _testOutput.WriteLine($"    return mesh;");
            _testOutput.WriteLine("}");
        }
    }

    //[Theory]
    //[MemberData(nameof(EnumerateGltfFiles))]
    //public void TestOnSampleModels(string fileName)
    //{
    //    var fileFormat = new GltfFileFormat();

    //    Assert.True(fileFormat.TryRead(new FileSystemEntry(fileName), out var content));

    //    //fileFormat.TryWrite(new FileSystemEntry(Path.GetFileNameWithoutExtension(Path.GetFileName(fileName)) + ".glb"), content);
    //}

    [Fact]
    public void BuildGltfFileFromScratch()
    {
        // Create a new unified mesh object that will store vertex data and indices.
        var mesh = new UnifiedIndexedMesh();

        // Create a vertex attribute to hold 3D positions for the mesh’s vertices.
        // DictionaryMeshVertexAttribute<Vector3> is a simple container that only stores unique vertex positions.
        IMeshVertexAttribute<Vector3> positions = new DictionaryMeshVertexAttribute<Vector3>();

        // Register the position attribute in the mesh using the predefined key for "Position".
        mesh.AddAttribute(MeshAttributeKey.Position, positions);

        // Access the mesh’s index list. This defines how vertices are connected into triangles.
        var indices = mesh.Indices;

        // Add three vertices to the position attribute, each representing a point in 3D space.
        // The Add() method returns the index of the newly added vertex,
        // which we then add to the mesh’s index buffer.
        indices.Add(positions.Add(new Vector3(0, 0, 0)));
        indices.Add(positions.Add(new Vector3(1, 0, 0)));
        indices.Add(positions.Add(new Vector3(0, 1, 0)));

        // Add a draw call defining how to interpret the indices above.
        // Here, MeshTopology.TriangleList means every three indices form a triangle.
        // The parameters (0, 3) mean: start at index 0, use 3 indices → one triangle.
        mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.TriangleList, 0, indices.Count));

        // Create a new file container to hold scene assets (e.g. meshes, textures, materials).
        var content = new FileContainer();

        // Store the mesh we just built in the container.
        content.Meshes.Add(mesh);

        // Create a simple scene object named "My Scene".
        var scene = new Scene() { Name = "My Scene" };

        // Create a node in the scene. The node holds a transform and references the mesh.
        // Here, TRSTransform applies a translation of +10 units along the X axis.
        var node = new Node()
        {
            Transform = new TRSTransform(new Vector3(10, 0, 0)),
            Mesh = new MeshReference(mesh)
        };
        // Add node the scene.
        scene.AddChild(node);
        // Add scene to the file content.
        content.Scenes.Add(scene);

        // Create an STL file format writer.
        var fileFormat = new GltfFileFormat();

        // Attempt to export the content (which includes the mesh and scene) as an STL file.
        // The STL format stores only the mesh geometry, not scene hierarchy or transforms.
        // Because of that all meshes in scene going to be merged into a single triangle soup.
        fileFormat.TryWrite(new FileSystemEntry("triangle.glb"), content);
    }
}
