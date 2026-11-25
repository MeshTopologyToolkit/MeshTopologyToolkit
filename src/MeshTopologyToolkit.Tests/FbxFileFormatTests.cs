using MeshTopologyToolkit.Fbx;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Operators;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class FbxFileFormatTests
{
    private static readonly System.Reflection.Assembly TestAssembly = typeof(FbxFileFormatTests).Assembly;

    private readonly ITestOutputHelper _testOutput;

    public FbxFileFormatTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    internal static FileContainer ReadSample(IFileFormat format, string resource)
    {
        Assert.True(format.TryRead(StreamFileSystemEntry.FromEmbeddedResource(TestAssembly, "MeshTopologyToolkit.Tests." + resource), out var content), $"Can't read {resource}");
        return content;
    }

    internal static IEnumerable<(Node node, Matrix4x4 world)> VisitWorld(Node root, Matrix4x4 parentWorld)
    {
        foreach (var child in root.Children)
        {
            var world = child.Transform.ToMatrix() * parentWorld;
            yield return (child, world);
            foreach (var nested in VisitWorld(child, world))
                yield return nested;
        }
    }

    /// <summary>All triangles of the scene in the world space as canonical strings. Vertices are rounded, the winding is kept.</summary>
    internal static List<string> WorldTriangles(FileContainer content, float tolerance = 1e-3f)
    {
        var result = new List<string>();
        foreach (var scene in content.Scenes)
        {
            foreach (var (node, world) in VisitWorld(scene, Matrix4x4.Identity))
            {
                if (node.Mesh?.Mesh == null)
                    continue;
                var mesh = node.Mesh.Mesh;
                var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
                var indices = mesh.GetAttributeIndices(MeshAttributeKey.Position);
                foreach (var drawCall in mesh.DrawCalls)
                {
                    foreach (var face in drawCall.GetFaces(indices))
                    {
                        var keys = new[] { face.A, face.B, face.C }.Select(i => Key(Vector3.Transform(positions[i], world), tolerance)).ToArray();
                        int min = 0;
                        for (int i = 1; i < 3; ++i)
                            if (string.CompareOrdinal(keys[i], keys[min]) < 0)
                                min = i;
                        result.Add($"{keys[min]} {keys[(min + 1) % 3]} {keys[(min + 2) % 3]}");
                    }
                }
            }
        }
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    private static string Key(Vector3 v, float tolerance)
    {
        int Round(float f) => (int)MathF.Round(f / tolerance);
        return $"({Round(v.X)},{Round(v.Y)},{Round(v.Z)})";
    }

    [Theory]
    [InlineData("samples.primitives.Primitives")]
    [InlineData("samples.primitives.TangentFaces")]
    [InlineData("samples.primitives.CilinderWithBoxUVProjection")]
    [InlineData("samples.primitives.UncappedCilinderWithRotatedUVProjection")]
    public void ReadsSameGeometryAsGltf(string resource)
    {
        var fbx = ReadSample(new FbxFileFormat(), resource + ".fbx").ToGltfSpace();
        var glb = ReadSample(new GltfFileFormat(), resource + ".glb").ToGltfSpace();

        var fbxTriangles = WorldTriangles(fbx, 2e-3f);
        var glbTriangles = WorldTriangles(glb, 2e-3f);
        _testOutput.WriteLine($"fbx: {fbxTriangles.Count} triangles, glb: {glbTriangles.Count} triangles");
        Assert.NotEmpty(fbxTriangles);
        Assert.Equal(glbTriangles.Count, fbxTriangles.Count);
        Assert.Equal(glbTriangles, fbxTriangles);
    }

    [Fact]
    public void YUpAndZUpFilesAreEqualInGltfSpace()
    {
        var yUp = ReadSample(new FbxFileFormat(), "samples.corner.TwoCorners_2014_YUp.fbx").ToGltfSpace();
        var zUp = ReadSample(new FbxFileFormat(), "samples.corner.TwoCorners_2014_ZUp.fbx").ToGltfSpace();

        foreach (var (name, c) in new[] { ("YUp", yUp), ("ZUp", zUp) })
        {
            _testOutput.WriteLine(name);
            foreach (var (node, world) in VisitWorld(c.Scenes[0], Matrix4x4.Identity))
            {
                var t = node.Transform.ToTRS();
                _testOutput.WriteLine($"  {node.Name}: T={t.Translation} R={t.Rotation} S={t.Scale} mesh={(node.Mesh != null)}");
            }
        }
        var a = WorldTriangles(yUp, 2e-3f);
        var b = WorldTriangles(zUp, 2e-3f);
        Assert.NotEmpty(a);
        Assert.Equal(a, b);
    }

    [Theory]
    [InlineData("max_transformed_skin_7500_binary")]
    [InlineData("max_transformed_skin_7500_ascii")]
    [InlineData("max_transformed_skin_6100_binary")]
    [InlineData("max_transformed_skin_6100_ascii")]
    [InlineData("max2009_cube_anim_6100_ascii")]
    [InlineData("max2009_cube_anim_6100_binary")]
    [InlineData("blender_279_default_7400_binary")]
    [InlineData("blender_279_default_6100_ascii")]
    [InlineData("blender_279_sausage_7400_binary")]
    [InlineData("maya_advanced_skinned_pivot_7700_binary")]
    [InlineData("maya_advanced_skinned_pivot_7700_ascii")]
    [InlineData("blender_279_uv_sets_7400_binary")]
    public void ReadsUfbxSamples(string name)
    {
        var content = ReadSample(new FbxFileFormat(), "samples.fbx." + name + ".fbx");
        var scene = Assert.Single(content.Scenes);
        _testOutput.WriteLine($"{name}: nodes={scene.VisitAllChildren().Count()} meshes={content.Meshes.Count} materials={content.Materials.Count} textures={content.Textures.Count} skins={content.Skins.Count} animations={content.Animations.Count}");
        foreach (var mesh in content.Meshes)
            _testOutput.WriteLine($"  mesh {mesh.Name}: attributes=[{string.Join(", ", mesh.GetAttributeKeys())}] drawCalls={mesh.DrawCalls.Count} triangles={mesh.DrawCalls.Sum(_ => _.NumIndices / 3)}");
        foreach (var skin in content.Skins)
            _testOutput.WriteLine($"  skin {skin.Name}: joints={skin.Joints.Count}");
        foreach (var animation in content.Animations)
            _testOutput.WriteLine($"  animation {animation.Name}: tracks={animation.Tracks.Count} duration={animation.Duration}");
        Assert.NotEmpty(content.Meshes);
    }
}
