using MeshTopologyToolkit.Fbx;
using MeshTopologyToolkit.Gltf;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class FbxWriteTests
{
    private static readonly System.Reflection.Assembly TestAssembly = typeof(FbxWriteTests).Assembly;

    private readonly ITestOutputHelper _testOutput;

    public FbxWriteTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    private static byte[] ReadResource(string resource)
    {
        using var stream = StreamFileSystemEntry.FromEmbeddedResource(TestAssembly, "MeshTopologyToolkit.Tests." + resource).OpenRead()!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static void AssertSame(FbxNode expected, FbxNode actual, string path)
    {
        Assert.Equal(expected.Name, actual.Name);
        path += "/" + expected.Name;
        Assert.True(expected.Properties.Count == actual.Properties.Count, $"{path}: property count {expected.Properties.Count} != {actual.Properties.Count}");
        for (int i = 0; i < expected.Properties.Count; ++i)
        {
            var e = expected.Properties[i];
            var a = actual.Properties[i];
            Assert.True(e.Type == a.Type, $"{path}[{i}]: type {e.Type} != {a.Type}");
            if (e.Value is Array ea)
                Assert.True(ea.Cast<object>().SequenceEqual(((Array)a.Value).Cast<object>()), $"{path}[{i}]: array differs");
            else
                Assert.True(Equals(e.Value, a.Value), $"{path}[{i}]: {e.Value} != {a.Value}");
        }
        Assert.True(expected.Children.Count == actual.Children.Count, $"{path}: child count {expected.Children.Count} != {actual.Children.Count}");
        for (int i = 0; i < expected.Children.Count; ++i)
            AssertSame(expected.Children[i], actual.Children[i], path);
    }

    [Theory]
    [InlineData("samples.primitives.TangentFaces.fbx")]
    [InlineData("samples.primitives.Primitives.fbx")]
    [InlineData("samples.fbx.max_transformed_skin_7500_binary.fbx")]
    [InlineData("samples.fbx.blender_279_sausage_7400_binary.fbx")]
    [InlineData("samples.fbx.max_transformed_skin_7500_ascii.fbx")]
    public void NodeTreeSurvivesWriteAndRead(string resource)
    {
        var original = FbxFileFormat.Parse(ReadResource(resource));
        var bytes = FbxBinaryWriter.Write(original);
        var copy = FbxFileFormat.Parse(bytes);

        Assert.Equal(7400, copy.Version);
        Assert.Equal(original.Roots.Count, copy.Roots.Count);
        for (int i = 0; i < original.Roots.Count; ++i)
            AssertSame(original.Roots[i], copy.Roots[i], "");
    }

    private static FileContainer WriteAndRead(FileContainer content, out byte[] bytes)
    {
        // Content is converted from glTF space to the space of the written file and back.
        var files = new Dictionary<string, byte[]>();
        var format = new FileFormatCollection(true, new FbxFileFormat());
        Assert.True(format.TryWrite(new InMemoryFileSystemEntry("out.fbx", files), content));
        bytes = files["out.fbx"];
        Assert.True(format.TryRead(new InMemoryFileSystemEntry("out.fbx", files), out var result));
        return result;
    }

    [Theory]
    [InlineData("samples.primitives.TangentFaces.fbx")]
    [InlineData("samples.primitives.Primitives.fbx")]
    [InlineData("samples.primitives.CilinderWithBoxUVProjection.fbx")]
    [InlineData("samples.fbx.maya_advanced_skinned_pivot_7700_binary.fbx")]
    public void SceneSurvivesWriteAndRead(string resource)
    {
        var original = FbxFileFormatTests.ReadSample(new FileFormatCollection(true, new FbxFileFormat()), resource);
        var copy = WriteAndRead(original, out _);

        var a = FbxFileFormatTests.WorldTriangles(original, 2e-3f);
        var b = FbxFileFormatTests.WorldTriangles(copy, 2e-3f);
        Assert.NotEmpty(a);
        Assert.Equal(a, b);

        Assert.Equal(original.Meshes.Count, copy.Meshes.Count);
        Assert.Equal(original.Materials.Count, copy.Materials.Count);
        for (int i = 0; i < original.Meshes.Count; ++i)
        {
            var keysA = original.Meshes[i].GetAttributeKeys().OrderBy(k => k.ToString()).Select(k => k.ToString()).ToList();
            var keysB = copy.Meshes[i].GetAttributeKeys().OrderBy(k => k.ToString()).Select(k => k.ToString()).ToList();
            Assert.Equal(keysA, keysB);
        }
        Assert.Equal(original.Skins.Count, copy.Skins.Count);
    }

    [Fact]
    public void MaterialsAndEmbeddedTexturesSurviveWriteAndRead()
    {
        var original = FbxFileFormatTests.ReadSample(new FbxFileFormat(), "samples.fbx.blender_279_default_7400_binary.fbx");
        var material = Assert.Single(original.Materials);
        material.SetVector4(MaterialParam.BaseColor, new Vector4(0.25f, 0.5f, 0.75f, 0.5f));
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4, 5 };
        material.SetTexture(MaterialParam.BaseColor, new Texture("checker.png", pngBytes));

        var copy = WriteAndRead(original, out _);
        var copied = Assert.Single(copy.Materials);
        var color = copied.Vector4Params[MaterialParam.BaseColor];
        Assert.Equal(0.25f, color.X, 4);
        Assert.Equal(0.5f, color.Y, 4);
        Assert.Equal(0.75f, color.Z, 4);
        Assert.Equal(0.5f, color.W, 4);
        Assert.Equal(AlphaMode.Blend, copied.AlphaMode);
        var texture = Assert.Contains(MaterialParam.BaseColor, (IDictionary<string, Texture>)copied.TextureParams.ToDictionary(kv => kv.Key, kv => kv.Value));
        Assert.Equal(pngBytes, texture.FileSystemEntry!.ReadAllBytes());
        Assert.Equal("checker.png", texture.FileSystemEntry.Name);
    }

    [Theory]
    [InlineData("samples.kronos.SimpleSkin.gltf")]
    public void GltfSkinAndAnimationSurviveFbxRoundTrip(string resource)
    {
        var format = new FileFormatCollection(true, new GltfFileFormat(), new FbxFileFormat());
        Assert.True(format.TryRead(StreamFileSystemEntry.FromEmbeddedResource(TestAssembly, "MeshTopologyToolkit.Tests." + resource), out var original));
        var animation = Assert.Single(original.Animations);

        var files = new Dictionary<string, byte[]>();
        Assert.True(format.TryWrite(new InMemoryFileSystemEntry("roundtrip.fbx", files), original));
        Assert.True(format.TryRead(new InMemoryFileSystemEntry("roundtrip.fbx", files), out var copy));
        var copiedAnimation = Assert.Single(copy.Animations);
        _testOutput.WriteLine($"duration {animation.Duration} -> {copiedAnimation.Duration}, fbx size {files["roundtrip.fbx"].Length}");

        var skin = Assert.Single(copy.Skins);
        Assert.Equal(Assert.Single(original.Skins).Joints.Count, skin.Joints.Count);

        float duration = animation.EndTime;
        for (int step = 0; step <= 8; ++step)
        {
            float time = duration * step / 8.0f;
            var expected = FbxSkinAnimationTests.SkinnedPositions(original, animation, time);
            var actual = FbxSkinAnimationTests.SkinnedPositions(copy, copiedAnimation, time);
            float deviation = FbxSkinAnimationTests.MaxDeviation(expected, actual);
            Assert.True(deviation < 1e-3f, $"time {time}: deviation {deviation}");
        }
    }
}
