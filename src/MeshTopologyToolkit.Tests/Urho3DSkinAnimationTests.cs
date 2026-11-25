using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Urho3D;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class Urho3DSkinAnimationTests
{
    private static readonly System.Reflection.Assembly TestAssembly = typeof(Urho3DSkinAnimationTests).Assembly;

    private static FileContainer ReadMdl(string fileName, Urho3DModelVersion version = Urho3DModelVersion.Rbfx)
    {
        var fileFormat = new Urho3DFileFormat(version);
        Assert.True(fileFormat.TryRead(StreamFileSystemEntry.FromEmbeddedResource(TestAssembly, "MeshTopologyToolkit.Tests.samples.urho3d." + fileName), out var content));
        return content;
    }

    private static Matrix4x4 GetWorldMatrix(Node node)
    {
        var m = node.Transform.ToMatrix();
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
            m = m * parent.Transform.ToMatrix();
        return m;
    }

    [Theory]
    // The offset matrix of a bone is the inverse of its bind pose in model space, so a bone moved by its initial pose stays in place.
    // Urho3D stores the initial pose and the bind pose separately: all bones of Jack.mdl are in their bind pose, but Mutant.mdl is
    // exported with posed limbs, so only a part of its bones match (20 of 31 at the time of writing). A misparsed skeleton would match nothing.
    [InlineData("Jack.mdl", 1.0f)]
    [InlineData("Mutant.mdl", 0.5f)]
    public void ReadSkeleton(string fileName, float minMatchingFraction)
    {
        var content = ReadMdl(fileName);

        var skin = Assert.Single(content.Skins);
        Assert.True(skin.Joints.Count > 1);
        Assert.Equal(skin.Joints.Count, skin.InverseBindMatrices.Count);
        Assert.Single(content.Scenes);
        Assert.All(skin.Joints, joint => Assert.False(string.IsNullOrEmpty(joint.Name)));

        // Order matters: a vertex goes to bone space first and to model space next.
        int matchingBones = 0;
        for (int i = 0; i < skin.Joints.Count; ++i)
        {
            var skinMatrix = skin.InverseBindMatrices[i] * GetWorldMatrix(skin.Joints[i]);
            if (IsIdentity(skinMatrix))
                ++matchingBones;
        }
        Assert.True(matchingBones >= skin.Joints.Count * minMatchingFraction, $"Only {matchingBones} of {skin.Joints.Count} bones are in their bind pose.");

        var node = content.Scenes[0].Children.Single(_ => _.Mesh != null);
        Assert.Same(skin, node.Mesh!.Skin);
        var mesh = node.Mesh.Mesh;
        Assert.True(mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Joints, out var joints));
        Assert.True(mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Weights, out var weights));
        var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
        int stayingVertices = 0;
        for (int v = 0; v < joints.Count; ++v)
        {
            Assert.InRange(joints[v].X, 0, skin.Joints.Count - 1);
            Assert.InRange(joints[v].W, 0, skin.Joints.Count - 1);
            Assert.InRange(weights[v].X + weights[v].Y + weights[v].Z + weights[v].W, 0.99f, 1.01f);

            // Skinning the bind pose does not move the vertex.
            var bone = (int)(joints[v].X + 0.5f);
            var skinned = Vector3.Transform(Vector3.Transform(positions[v], skin.InverseBindMatrices[bone]), GetWorldMatrix(skin.Joints[bone]));
            if (Vector3.Distance(skinned, positions[v]) < 0.1f)
                ++stayingVertices;
        }
        Assert.True(stayingVertices >= joints.Count * minMatchingFraction, $"Only {stayingVertices} of {joints.Count} vertices stay in place in the bind pose.");
    }

    private static bool IsIdentity(Matrix4x4 m)
    {
        // A bone of Mutant.mdl can be 5 cm away from where its bind pose puts it, a misparsed matrix is off by far more.
        return MaxDifference(m, Matrix4x4.Identity) < 0.1f;
    }

    private static float MaxDifference(Matrix4x4 a, Matrix4x4 b)
    {
        float max = 0.0f;
        void Check(float x, float y) => max = MathF.Max(max, MathF.Abs(x - y));
        Check(a.M11, b.M11); Check(a.M12, b.M12); Check(a.M13, b.M13);
        Check(a.M21, b.M21); Check(a.M22, b.M22); Check(a.M23, b.M23);
        Check(a.M31, b.M31); Check(a.M32, b.M32); Check(a.M33, b.M33);
        Check(a.M41, b.M41); Check(a.M42, b.M42); Check(a.M43, b.M43);
        return max;
    }

    [Theory]
    [InlineData("Jack.mdl", Urho3DModelVersion.Original)]
    [InlineData("Mutant.mdl", Urho3DModelVersion.VertexDeclarations)]
    [InlineData("Mutant.mdl", Urho3DModelVersion.Rbfx)]
    public void SkinnedModelRoundTrip(string fileName, Urho3DModelVersion version)
    {
        var content = ReadMdl(fileName);
        var fileFormat = new Urho3DFileFormat(version);

        MemoryStream? memoryStream = null;
        Assert.True(fileFormat.TryWrite(new StreamFileSystemEntry(() => memoryStream = new MemoryStream(), fileName), content));
        Assert.True(fileFormat.TryRead(new InMemoryFileSystemEntry(fileName, memoryStream!.ToArray()), out var restored));

        var skin = Assert.Single(content.Skins);
        var restoredSkin = Assert.Single(restored.Skins);
        Assert.Equal(skin.Joints.Count, restoredSkin.Joints.Count);
        for (int i = 0; i < skin.Joints.Count; ++i)
        {
            Assert.Equal(skin.Joints[i].Name, restoredSkin.Joints[i].Name);
            Assert.Equal(skin.Joints[i].Parent?.Name, restoredSkin.Joints[i].Parent?.Name);
            Assert.True(Matrix4x4.Invert(restoredSkin.InverseBindMatrices[i], out var inverse));
            Assert.True(MaxDifference(skin.InverseBindMatrices[i] * inverse, Matrix4x4.Identity) < 1e-3f, $"Offset matrix {i} changed");
        }

        var mesh = content.Meshes[0];
        var restoredMesh = restored.Meshes[0];
        Assert.True(restoredMesh.HasAttribute(MeshAttributeKey.Joints));
        Assert.True(restoredMesh.HasAttribute(MeshAttributeKey.Weights));
        var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
        var restoredPositions = restoredMesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
        if (positions.Count == restoredPositions.Count)
        {
            var joints = mesh.GetAttribute<Vector4>(MeshAttributeKey.Joints);
            var restoredJoints = restoredMesh.GetAttribute<Vector4>(MeshAttributeKey.Joints);
            var weights = mesh.GetAttribute<Vector4>(MeshAttributeKey.Weights);
            var restoredWeights = restoredMesh.GetAttribute<Vector4>(MeshAttributeKey.Weights);
            for (int v = 0; v < positions.Count; ++v)
            {
                Assert.Equal(joints[v], restoredJoints[v]);
                Assert.Equal(weights[v], restoredWeights[v], new Vector4EqualityComparer(1e-6f));
            }
        }
    }

    [Fact]
    public void SkinnedModelToGltfAndBack()
    {
        var content = ReadMdl("Mutant.mdl").ToGltfSpace();

        var gltf = new GltfFileFormat();
        MemoryStream? memoryStream = null;
        Assert.True(gltf.TryWrite(new StreamFileSystemEntry(() => memoryStream = new MemoryStream(), "mutant.glb"), content));
        Assert.True(gltf.TryRead(new InMemoryFileSystemEntry("mutant.glb", memoryStream!.ToArray()), out var restored));

        var skin = Assert.Single(restored.Skins);
        Assert.Equal(content.Skins[0].Joints.Count, skin.Joints.Count);
        Assert.Equal(content.Skins[0].Joints[0].Name, skin.Joints[0].Name);
    }

    [Fact]
    public void AnimationRoundTrip()
    {
        var animation = new Animation("Wave");
        var track = new AnimationTrack(null, "Hand")
        {
            Translation = AnimationCurve.CreateVector3().Add(0.5f, Vector3.Zero).Add(1.5f, new Vector3(1, 2, 3)),
            Rotation = AnimationCurve.CreateQuaternion().Add(0.5f, Quaternion.Identity).Add(1.5f, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 1.0f)),
        };
        animation.Tracks.Add(track);
        var content = new FileContainer();
        content.Add(animation);

        var format = new Urho3DAnimationFormat();
        MemoryStream? memoryStream = null;
        Assert.True(format.TryWrite(new StreamFileSystemEntry(() => memoryStream = new MemoryStream(), "Wave.ani"), content));
        Assert.True(format.TryRead(new InMemoryFileSystemEntry("Wave.ani", memoryStream!.ToArray()), out var restored));

        var restoredAnimation = Assert.Single(restored.Animations);
        Assert.Equal("Wave", restoredAnimation.Name);
        // Urho3D animations start at 0.
        Assert.Equal(0.0f, restoredAnimation.StartTime, 1e-6f);
        Assert.Equal(1.0f, restoredAnimation.Duration, 1e-6f);
        var restoredTrack = Assert.Single(restoredAnimation.Tracks);
        Assert.Equal("Hand", restoredTrack.TargetName);
        Assert.Null(restoredTrack.Scale);
        Assert.Equal(new Vector3(0.5f, 1, 1.5f), restoredTrack.Translation!.Evaluate(0.5f), Vector3EqualityComparer.Default);
        var expectedRotation = track.Rotation.Evaluate(1.0f);
        Assert.True(MathF.Abs(Quaternion.Dot(expectedRotation, restoredTrack.Rotation!.Evaluate(0.5f))) > 0.9999f);
    }

    [Fact]
    public void SaveGltfAnimationAsAni()
    {
        var gltf = new GltfFileFormat();
        Assert.True(gltf.TryRead(StreamFileSystemEntry.FromEmbeddedResource(TestAssembly, "MeshTopologyToolkit.Tests.samples.kronos.SimpleSkin.gltf"), out var content));
        // SimpleSkin has unnamed joints. Urho3D tracks are bound by name.
        var joints = content.Skins[0].Joints;
        for (int i = 0; i < joints.Count; ++i)
            joints[i].Name = "Joint" + i;
        var animation = content.Animations[0];

        var format = new Urho3DAnimationFormat();
        MemoryStream? memoryStream = null;
        Assert.True(format.TryWrite(new StreamFileSystemEntry(() => memoryStream = new MemoryStream(), "Skin.ani"), animation));
        Assert.True(format.TryRead(new InMemoryFileSystemEntry("Skin.ani", memoryStream!.ToArray()), out var restored));
        var restoredAnimation = Assert.Single(restored.Animations);

        Assert.Equal(animation.Duration, restoredAnimation.Duration, 1e-4f);
        Assert.All(restoredAnimation.Tracks, track => Assert.Null(track.Target));

        // Bind to the scene of the model.
        var root = content.Scenes[0];
        Assert.Equal(0, restoredAnimation.Bind(root));
        Assert.All(restoredAnimation.Tracks, track => Assert.NotNull(track.Target));

        // The animation was converted to Urho3D space: converting it back gives the original rotation.
        var restoredInGltf = restored.ToGltfSpace();
        var original = animation.Tracks.First(_ => _.Rotation != null);
        var track = restoredInGltf.Animations[0].Tracks.First(_ => _.TargetName == original.Target!.Name);
        for (float t = 0.0f; t <= animation.Duration; t += 0.25f)
        {
            var expected = original.Rotation!.Evaluate(t + animation.StartTime);
            var actual = track.Rotation!.Evaluate(t);
            Assert.True(MathF.Abs(Quaternion.Dot(expected, actual)) > 0.999f, $"Rotation differs at {t}: {expected} != {actual}");
        }
    }
}
