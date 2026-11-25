using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Operators;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class GltfSkinAnimationTests
{
    private const string SimpleSkinResource = "MeshTopologyToolkit.Tests.samples.kronos.SimpleSkin.gltf";

    private static FileContainer ReadSimpleSkin()
    {
        var fileFormat = new GltfFileFormat();
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(typeof(GltfSkinAnimationTests).Assembly, SimpleSkinResource), out var content));
        return content;
    }

    private static FileContainer RoundTrip(FileContainer content, string name = "roundtrip.glb")
    {
        var fileFormat = new GltfFileFormat();
        MemoryStream? memoryStream = null;
        Assert.True(fileFormat.TryWrite(new StreamFileSystemEntry(() => memoryStream = new MemoryStream(), name), content));
        Assert.NotNull(memoryStream);
        Assert.True(fileFormat.TryRead(new InMemoryFileSystemEntry(name, memoryStream!.ToArray()), out var restored));
        return restored;
    }

    [Fact]
    public void ReadSkinAndAnimation()
    {
        var content = ReadSimpleSkin();

        var skin = Assert.Single(content.Skins);
        Assert.Equal(2, skin.Joints.Count);
        Assert.Equal(2, skin.InverseBindMatrices.Count);

        // The skinned node refers to the skin and the mesh has skinning attributes.
        var skinnedNode = content.Scenes.SelectMany(_ => _.VisitAllChildren()).Single(_ => _.Mesh?.Skin != null);
        Assert.Same(skin, skinnedNode.Mesh!.Skin);
        Assert.True(skinnedNode.Mesh.Mesh.HasAttribute(MeshAttributeKey.Joints));
        Assert.True(skinnedNode.Mesh.Mesh.HasAttribute(MeshAttributeKey.Weights));

        var animation = Assert.Single(content.Animations);
        Assert.NotEmpty(animation.Tracks);
        Assert.All(animation.Tracks, track => Assert.NotNull(track.Target));
        Assert.True(animation.Duration > 0.0f);
        var rotation = animation.Tracks.Select(_ => _.Rotation).First(_ => _ != null)!;
        Assert.True(rotation.Keys.Count > 1);
    }

    [Fact]
    public void SkinAndAnimationRoundTrip()
    {
        var content = ReadSimpleSkin();
        var restored = RoundTrip(content);

        var skin = Assert.Single(restored.Skins);
        Assert.Equal(content.Skins[0].Joints.Count, skin.Joints.Count);
        for (int i = 0; i < skin.Joints.Count; ++i)
        {
            Assert.Equal(content.Skins[0].InverseBindMatrices[i], skin.InverseBindMatrices[i]);
        }

        var skinnedNode = restored.Scenes.SelectMany(_ => _.VisitAllChildren()).Single(_ => _.Mesh?.Skin != null);
        Assert.True(skinnedNode.Mesh!.Mesh.HasAttribute(MeshAttributeKey.Joints));
        Assert.True(skinnedNode.Mesh.Mesh.HasAttribute(MeshAttributeKey.Weights));

        var original = Assert.Single(content.Animations);
        var animation = Assert.Single(restored.Animations);
        Assert.Equal(original.Tracks.Count, animation.Tracks.Count);
        Assert.Equal(original.Duration, animation.Duration, 1e-5f);
        foreach (var originalTrack in original.Tracks)
        {
            // Joints can be unnamed, so find the same joint by its index in the skin.
            var jointIndex = content.Skins[0].Joints.IndexOf(originalTrack.Target!);
            Assert.True(jointIndex >= 0);
            var track = animation.Tracks.Single(_ => _.Target == skin.Joints[jointIndex]);
            Assert.Equal(originalTrack.Rotation?.Keys.Count, track.Rotation?.Keys.Count);
            if (originalTrack.Rotation == null)
                continue;
            for (float t = 0.0f; t <= original.EndTime; t += 0.25f)
            {
                var expected = originalTrack.Rotation.Evaluate(t);
                var actual = track.Rotation!.Evaluate(t);
                Assert.True(MathF.Abs(Quaternion.Dot(expected, actual)) > 0.9999f, $"Rotation differs at {t}: {expected} != {actual}");
            }
        }
    }

    [Fact]
    public void JointsAreNotWrittenWithoutSkin()
    {
        var content = ReadSimpleSkin();
        foreach (var node in content.Scenes.SelectMany(_ => _.VisitAllChildren()))
        {
            if (node.Mesh != null)
                node.Mesh.Skin = null;
        }

        var restored = RoundTrip(content);

        Assert.Empty(restored.Skins);
        Assert.All(restored.Meshes, mesh => Assert.False(mesh.HasAttribute(MeshAttributeKey.Joints)));
    }

    [Fact]
    public void MaterialAlphaAndDoubleSidedRoundTrip()
    {
        var content = new FileContainer();
        var material = new Material("Glass") { AlphaMode = AlphaMode.Mask, AlphaCutoff = 0.25f, DoubleSided = true };
        material.SetVector4(MaterialParam.BaseColor, new Vector4(1, 0, 0, 0.5f));
        var blend = new Material("Blend") { AlphaMode = AlphaMode.Blend };
        content.AddSingleMeshScene(new MeshReference(Shapes.BuildBox(), material));
        content.AddSingleMeshScene(new MeshReference(Shapes.BuildBox(), blend));

        var restored = RoundTrip(content);

        var restoredMaterial = restored.Materials.Single(_ => _.Name == "Glass");
        Assert.Equal(AlphaMode.Mask, restoredMaterial.AlphaMode);
        Assert.Equal(0.25f, restoredMaterial.AlphaCutoff, 1e-6f);
        Assert.True(restoredMaterial.DoubleSided);
        var restoredBlend = restored.Materials.Single(_ => _.Name == "Blend");
        Assert.Equal(AlphaMode.Blend, restoredBlend.AlphaMode);
        Assert.False(restoredBlend.DoubleSided);
    }

    [Fact]
    public void StepAndCubicCurvesRoundTrip()
    {
        var content = new FileContainer();
        var mesh = Shapes.BuildBox();
        content.AddSingleMeshScene(mesh);
        var node = content.Scenes[0].Children[0];
        node.Name = "Animated";

        var animation = new Animation("Test");
        var track = new AnimationTrack(node)
        {
            Translation = AnimationCurve.CreateVector3(AnimationInterpolation.Step)
                .Add(0.0f, Vector3.Zero).Add(1.0f, Vector3.UnitX).Add(2.0f, Vector3.UnitY),
            Scale = AnimationCurve.CreateVector3(AnimationInterpolation.CubicSpline)
                .Add(0.0f, Vector3.One, Vector3.Zero, Vector3.UnitX)
                .Add(1.0f, new Vector3(2, 2, 2), Vector3.UnitX, Vector3.Zero),
        };
        animation.Tracks.Add(track);
        content.Add(animation);

        var restored = RoundTrip(content);

        var restoredTrack = Assert.Single(Assert.Single(restored.Animations).Tracks);
        Assert.Equal(AnimationInterpolation.Step, restoredTrack.Translation!.Interpolation);
        Assert.Equal(new Vector3(1, 0, 0), restoredTrack.Translation.Evaluate(1.5f));
        Assert.Equal(AnimationInterpolation.CubicSpline, restoredTrack.Scale!.Interpolation);
        // Same values (and tangents) must evaluate to the same curve.
        for (float t = 0.0f; t <= 1.0f; t += 0.1f)
        {
            Assert.Equal(track.Scale.Evaluate(t), restoredTrack.Scale.Evaluate(t), Vector3EqualityComparer.Default);
        }
    }

    [Fact]
    public void SpaceTransformKeepsSkinningConsistent()
    {
        // The world matrix of a joint multiplied by its inverse bind matrix must stay the same
        // relation after a change of space (it is the matrix that moves a bind pose vertex).
        var content = ReadSimpleSkin();
        var transform = new SpaceTransform(SpaceTransform._YXZ, 2.0f);
        var transformed = new SpaceTransformOperator(transform).Transform(content);

        var originalSkin = content.Skins[0];
        var skin = transformed.Skins[0];
        Assert.Equal(originalSkin.Joints.Count, skin.Joints.Count);

        var transformer = transform.Transformer;
        for (int i = 0; i < skin.Joints.Count; ++i)
        {
            var originalSkinMatrix = originalSkin.InverseBindMatrices[i];
            var skinMatrix = skin.InverseBindMatrices[i];
            // A bind pose vertex is moved to the joint space and back by world * inverseBind. Both worlds are in their own spaces.
            var originalWorld = originalSkin.Joints[i].GetWorldMatrix();
            var world = skin.Joints[i].GetWorldMatrix();
            var changedPoint = transformer.TransformPosition(new Vector3(0.3f, 0.7f, 1.1f));
            var viaOriginal = Vector3.Transform(Vector3.Transform(new Vector3(0.3f, 0.7f, 1.1f), originalSkinMatrix), originalWorld);
            var viaTransformed = Vector3.Transform(Vector3.Transform(changedPoint, skinMatrix), world);
            Assert.Equal(transformer.TransformPosition(viaOriginal), viaTransformed, Vector3EqualityComparer.Default);
        }

        // Animation keys: the animated joint pose is the same pose seen in the other space.
        var originalTrack = content.Animations[0].Tracks.First(_ => _.Rotation != null);
        var track = transformed.Animations[0].Tracks[content.Animations[0].Tracks.IndexOf(originalTrack)];
        var originalRotation = originalTrack.Rotation!.Evaluate(0.5f);
        var rotation = track.Rotation!.Evaluate(0.5f);
        var point = new Vector3(1, 2, 3);
        var rotatedOriginal = Vector3.Transform(point, originalRotation);
        var rotated = Vector3.Transform(transformer.TransformNormal(point), rotation);
        Assert.Equal(transformer.TransformNormal(rotatedOriginal), rotated, Vector3EqualityComparer.Default);
    }

}

internal static class NodeTestExtensions
{
    public static Matrix4x4 GetWorldMatrix(this Node node)
    {
        var m = node.Transform.ToMatrix();
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            m = m * parent.Transform.ToMatrix();
        }
        return m;
    }
}
