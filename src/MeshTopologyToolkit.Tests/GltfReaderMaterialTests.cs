using MeshTopologyToolkit.Gltf;
using SharpGLTF.Schema2;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class GltfReaderMaterialTests
{
    // A 1x1 PNG.
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    private static FileContainer Read(ModelRoot root)
    {
        using var stream = new MemoryStream();
        root.WriteGLB(stream);
        Assert.True(new GltfFileFormat().TryRead(new InMemoryFileSystemEntry("test.glb", stream.ToArray()), out var content));
        return content;
    }

    [Fact]
    public void TexturesOfEverySlotAreRead_WithTheirSamplerAndTextureCoordinateSet()
    {
        var root = ModelRoot.CreateModel();
        var image = root.UseImageWithContent(Png);
        image.Name = "pixel";
        var sampler = root.UseTextureSampler(TextureWrapMode.CLAMP_TO_EDGE, TextureWrapMode.MIRRORED_REPEAT, TextureMipMapFilter.DEFAULT, TextureInterpolationFilter.DEFAULT);
        var texture = root.UseTexture(image, sampler);

        var material = root.CreateMaterial("Full");
        material.InitializePBRMetallicRoughness();
        material.FindChannel("BaseColor")!.Value.SetTexture(0, texture);
        material.FindChannel("MetallicRoughness")!.Value.SetTexture(1, texture);
        material.FindChannel("Normal")!.Value.SetTexture(0, texture);
        material.FindChannel("Occlusion")!.Value.SetTexture(0, texture);
        material.FindChannel("Emissive")!.Value.SetTexture(0, texture);

        var read = Read(root).Materials.Single(m => m.Name == "Full");

        foreach (var slot in new[] { MaterialParam.BaseColor, MaterialParam.MetallicRoughness, MaterialParam.Normal, MaterialParam.Occlusion, MaterialParam.Emissive })
        {
            Assert.True(read.TextureParams.TryGetValue(slot, out var read1), slot);
            Assert.Equal(TextureWrap.Clamp, read1!.WrapU);
            Assert.Equal(TextureWrap.Mirror, read1.WrapV);
            Assert.EndsWith(".png", read1.FileSystemEntry!.Name);
        }
        Assert.Equal(1, read.TexCoordParams[MaterialParam.MetallicRoughness]);
        Assert.False(read.TexCoordParams.ContainsKey(MaterialParam.BaseColor));
    }

    [Fact]
    public void AMaterialWithExtensionsOfTheSpecification_IsReadWithoutThem()
    {
        var root = ModelRoot.CreateModel();
        var builder = new SharpGLTF.Materials.MaterialBuilder("Coated")
            .WithChannelParam(SharpGLTF.Materials.KnownChannel.BaseColor, SharpGLTF.Materials.KnownProperty.RGBA, new Vector4(1, 0, 0, 1))
            .WithChannelParam(SharpGLTF.Materials.KnownChannel.ClearCoat, SharpGLTF.Materials.KnownProperty.ClearCoatFactor, 0.5f)
            .WithChannelParam(SharpGLTF.Materials.KnownChannel.Transmission, SharpGLTF.Materials.KnownProperty.TransmissionFactor, 0.5f);
        root.CreateMaterial(builder);

        var read = Read(root).Materials.Single(m => m.Name == "Coated");

        Assert.True(read.Vector4Params.ContainsKey(MaterialParam.BaseColor));
    }

    [Fact]
    public void APrimitiveThatHasAnAttributeTheEarlierOnesLack_GivesThemTheNeutralValue()
    {
        var root = ModelRoot.CreateModel();
        var mesh = root.CreateMesh("Mixed");
        var positions = new[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY };
        mesh.CreatePrimitive()
            .WithVertexAccessor("POSITION", positions)
            .WithIndicesAccessor(PrimitiveType.TRIANGLES, new[] { 0, 1, 2 });
        mesh.CreatePrimitive()
            .WithVertexAccessor("POSITION", positions)
            .WithVertexAccessor("COLOR_0", new[] { new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1), new Vector4(0, 0, 1, 1) })
            .WithIndicesAccessor(PrimitiveType.TRIANGLES, new[] { 0, 1, 2 });
        root.UseScene("scene").CreateNode("node").WithMesh(mesh);

        var read = Read(root);

        var unified = read.Meshes.Single().AsUnified();
        Assert.True(unified.TryGetAttribute<Vector4>(MeshAttributeKey.Color, out var colors));
        var drawCalls = unified.DrawCalls.ToList();
        Assert.Equal(2, drawCalls.Count);
        // The first primitive had no colours: white, not transparent black.
        var first = unified.Indices[drawCalls[0].StartIndex];
        Assert.Equal(Vector4.One, colors[first]);
        var second = unified.Indices[drawCalls[1].StartIndex];
        Assert.Equal(new Vector4(1, 0, 0, 1), colors[second]);
    }
}
