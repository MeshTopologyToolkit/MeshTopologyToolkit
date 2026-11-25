using MeshTopologyToolkit.Fbx;
using System.Globalization;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;

public class FbxSkinAnimationTests
{
    private readonly ITestOutputHelper _testOutput;

    public FbxSkinAnimationTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    private static List<Vector3> ReadObjPositions(string resource)
    {
        var entry = StreamFileSystemEntry.FromEmbeddedResource(typeof(FbxSkinAnimationTests).Assembly, "MeshTopologyToolkit.Tests." + resource);
        using var reader = new StreamReader(entry.OpenRead()!);
        var result = new List<Vector3>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!line.StartsWith("v "))
                continue;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            result.Add(new Vector3(
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture)));
        }
        return result;
    }

    /// <summary>Evaluates the local transforms of all nodes at the given time, taking the tracks of the animation into account.</summary>
    internal static Dictionary<Node, Matrix4x4> EvaluateWorld(FileContainer content, Animation? animation, float time)
    {
        var tracks = new Dictionary<Node, AnimationTrack>();
        if (animation != null)
        {
            foreach (var track in animation.Tracks)
                if (track.Target != null)
                    tracks[track.Target] = track;
        }

        var world = new Dictionary<Node, Matrix4x4>();
        void Visit(Node node, Matrix4x4 parentWorld)
        {
            var local = node.Transform.ToMatrix();
            if (tracks.TryGetValue(node, out var track))
            {
                var trs = node.Transform.ToTRS();
                var t = track.Translation != null ? track.Translation.Evaluate(time) : trs.Translation;
                var r = track.Rotation != null ? track.Rotation.Evaluate(time) : trs.Rotation;
                var s = track.Scale != null ? track.Scale.Evaluate(time) : trs.Scale;
                local = new TRSTransform(t, r, s).ToMatrix();
            }
            var w = local * parentWorld;
            world[node] = w;
            foreach (var child in node.Children)
                Visit(child, w);
        }
        foreach (var scene in content.Scenes)
            Visit(scene, Matrix4x4.Identity);
        return world;
    }

    /// <summary>World space positions of the first mesh of the file, skinned when the mesh has a skin.</summary>
    internal static List<Vector3> SkinnedPositions(FileContainer content, Animation? animation, float time)
    {
        var world = EvaluateWorld(content, animation, time);
        var result = new List<Vector3>();
        foreach (var (node, nodeWorld) in world)
        {
            var reference = node.Mesh;
            if (reference?.Mesh == null)
                continue;
            var mesh = reference.Mesh;
            var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
            var skin = reference.Skin;
            var positionIndices = mesh.GetAttributeIndices(MeshAttributeKey.Position);
            if (skin != null
                && mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Joints, out var joints) && mesh.TryGetAttributeIndices(MeshAttributeKey.Joints, out var jointIndices)
                && mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Weights, out var weights) && mesh.TryGetAttributeIndices(MeshAttributeKey.Weights, out var weightIndices))
            {
                var jointMatrices = skin.Joints.Select((j, i) => skin.InverseBindMatrices[i] * world[j]).ToList();
                // One result per corner, every attribute has its own index list.
                for (int c = 0; c < positionIndices.Count; ++c)
                {
                    var position = positions[positionIndices[c]];
                    var p = Vector3.Zero;
                    var j = joints[jointIndices[c]];
                    var w = weights[weightIndices[c]];
                    p += Vector3.Transform(position, jointMatrices[(int)j.X]) * w.X;
                    p += Vector3.Transform(position, jointMatrices[(int)j.Y]) * w.Y;
                    p += Vector3.Transform(position, jointMatrices[(int)j.Z]) * w.Z;
                    p += Vector3.Transform(position, jointMatrices[(int)j.W]) * w.W;
                    result.Add(p);
                }
            }
            else
            {
                for (int c = 0; c < positionIndices.Count; ++c)
                    result.Add(Vector3.Transform(positions[positionIndices[c]], nodeWorld));
            }
        }
        return result;
    }

    private static string BoundsText(IReadOnlyList<Vector3> points)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var p in points)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        return $"{min} .. {max}";
    }

    /// <summary>Largest distance from a reference vertex to the closest evaluated vertex.</summary>
    internal static float MaxDeviation(IReadOnlyList<Vector3> reference, IReadOnlyList<Vector3> actual)
    {
        float max = 0.0f;
        foreach (var r in reference)
        {
            float best = float.MaxValue;
            foreach (var a in actual)
                best = Math.Min(best, Vector3.Distance(r, a));
            max = Math.Max(max, best);
        }
        return max;
    }

    [Theory]
    [InlineData("maya_advanced_skinned_pivot_7700_binary")]
    [InlineData("maya_advanced_skinned_pivot_7700_ascii")]
    public void SkinnedPoseWithPivotsMatchesReference(string fbxName)
    {
        var content = FbxFileFormatTests.ReadSample(new FbxFileFormat(), "samples.fbx." + fbxName + ".fbx");
        var reference = ReadObjPositions("samples.fbx.maya_advanced_skinned_pivot.obj");
        var actual = SkinnedPositions(content, null, 0.0f);
        float deviation = MaxDeviation(reference, actual);
        _testOutput.WriteLine($"{fbxName}: {reference.Count} reference vertices, {actual.Count} evaluated, max deviation {deviation}");
        _testOutput.WriteLine($"  reference bounds {BoundsText(reference)}");
        _testOutput.WriteLine($"  actual bounds    {BoundsText(actual)}");
        Assert.True(deviation < 0.05f, $"Max deviation {deviation}");
    }

    [Theory]
    [InlineData("max_transformed_skin_7500_binary", "max_transformed_skin", 5)]
    [InlineData("max_transformed_skin_7500_binary", "max_transformed_skin", 15)]
    [InlineData("max_transformed_skin_7500_ascii", "max_transformed_skin", 15)]
    [InlineData("max_transformed_skin_6100_binary", "max_transformed_skin", 5)]
    [InlineData("max_transformed_skin_6100_ascii", "max_transformed_skin", 15)]
    [InlineData("max2009_cube_anim_6100_ascii", "max2009_cube_anim", 15)]
    [InlineData("max2009_cube_anim_6100_binary", "max2009_cube_anim", 45)]
    public void AnimatedPoseMatchesReference(string fbxName, string objPrefix, int frame)
    {
        var content = FbxFileFormatTests.ReadSample(new FbxFileFormat(), "samples.fbx." + fbxName + ".fbx");
        var reference = ReadObjPositions($"samples.fbx.{objPrefix}_{frame}.obj");
        var animation = Assert.Single(content.Animations);

        var actual = SkinnedPositions(content, animation, frame / 30.0f);
        float deviation = MaxDeviation(reference, actual);
        _testOutput.WriteLine($"{fbxName} frame {frame}: {reference.Count} reference vertices, {actual.Count} evaluated, max deviation {deviation}");
        if (deviation >= 0.05f)
        {
            _testOutput.WriteLine($"  reference bounds {BoundsText(reference)}");
            _testOutput.WriteLine($"  actual bounds    {BoundsText(actual)}");
            var bind = SkinnedPositions(content, null, 0.0f);
            _testOutput.WriteLine($"  bind pose bounds {BoundsText(bind)}");
            foreach (var (node, world) in EvaluateWorld(content, animation, frame / 30.0f))
                _testOutput.WriteLine($"  node {node.Name}: T={world.Translation}");
        }
        Assert.True(deviation < 0.05f, $"Max deviation {deviation}");
    }
}
