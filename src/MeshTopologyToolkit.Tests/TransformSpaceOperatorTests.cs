using MeshTopologyToolkit.Operators;
using System.Numerics;
using Xunit.Abstractions;

namespace MeshTopologyToolkit.Tests;
public class TransformSpaceOperatorTests
{
    private readonly ITestOutputHelper _testOutput;

    public TransformSpaceOperatorTests(ITestOutputHelper testOutput)
    {
        this._testOutput = testOutput;
    }

    [Fact]
    public void IdentityTransform()
    {
        var mesh = TestUtils.BuildPointMesh(new Vector3(2.0f, 3.0f, 4.0f), Vector3.UnitY);

        var spaceTransform = SpaceTransform.Identity;

        var container = TestUtils.BuildChildParentScene(SpaceTransform.Identity,
            new TRSTransform(new Vector3(1, 2, 3), Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f), new Vector3(1.1f, 2.2f, 3.3f)),
            new TRSTransform(new Vector3(1.1f, 2.2f, 3.3f), Quaternion.CreateFromYawPitchRoll(0.11f, -0.22f, 0.15f), new Vector3(0.4f, 0.5f, 0.6f)),
            mesh);

        var originalMerged = new SpaceTransformOperator(spaceTransform).Transform(new MergeOperator().Transform(container));
        var transformedMerged = new MergeOperator().Transform(new SpaceTransformOperator(spaceTransform).Transform(container));

        CompareMergedMeshes(originalMerged, transformedMerged);
    }

    [Fact]
    public void RotateSingleNode()
    {
        var mesh = TestUtils.BuildPointMesh(new Vector3(2.0f, 3.0f, 4.0f), Vector3.UnitY);

        var spaceTransform = new SpaceTransform(SpaceTransform._YXZ);

        var container = TestUtils.BuildSingleNodeScene(SpaceTransform.Identity,
            new TRSTransform(
                //new Vector3(1, 2, 3), 
                //Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f), 
                //new Vector3(1.1f, 2.2f, 3.3f)
                ),
            mesh);

        _testOutput.WriteLine("Original scene: --------------");
        TestUtils.PrintScene(_testOutput, container.Scenes[0]);

        var mergedThenTransformed = new SpaceTransformOperator(spaceTransform).Transform(new MergeOperator().Transform(container));

        _testOutput.WriteLine("Merge then Transform Space: --------------");
        TestUtils.PrintScene(_testOutput, mergedThenTransformed.Scenes[0]);

        var transformedThenMerged = new MergeOperator().Transform(new SpaceTransformOperator(spaceTransform).Transform(container));
        _testOutput.WriteLine("Transform Space then Merge: --------------");
        TestUtils.PrintScene(_testOutput, transformedThenMerged.Scenes[0]);

        CompareMergedMeshes(new MergeOperator().Transform(mergedThenTransformed), transformedThenMerged);
    }

    //[Fact]
    public void Rotate()
    {
        var mesh = TestUtils.BuildPointMesh(new Vector3(2.0f, 3.0f, 4.0f), Vector3.UnitY);

        var spaceTransform = new SpaceTransform(SpaceTransform._YXZ);

        var container = TestUtils.BuildChildParentScene(spaceTransform,
            new TRSTransform(new Vector3(1, 2, 3), Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.3f), new Vector3(1.1f, 2.2f, 3.3f)),
            new TRSTransform(new Vector3(1.1f, 2.2f, 3.3f), Quaternion.CreateFromYawPitchRoll(0.11f, -0.22f, 0.15f), new Vector3(0.4f, 0.5f, 0.6f)),
            mesh);

        var transformed = new SpaceTransformOperator(container.FileToGltfTransform!).Transform(container);

        var originalMerged = new SpaceTransformOperator(spaceTransform).Transform(new MergeOperator().Transform(container));
        var transformedMerged = new MergeOperator().Transform(transformed);

        CompareMergedMeshes(originalMerged, transformedMerged);
    }

    private void CompareMergedMeshes(FileContainer originalMerged, FileContainer transformedMerged)
    {
        var expected = originalMerged.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0];
        var actual = transformedMerged.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0];
        Assert.Equal(expected, actual, Vector3EqualityComparer.Default);
    }

    [Fact]
    public void TansformPosition()
    {
        var mesh = new UnifiedIndexedMesh();
        IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
        mesh.AddAttribute(MeshAttributeKey.Position, positions);
        var indices = mesh.Indices;
        indices.Add(positions.Add(new Vector3(1, 2, 3)));
        mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.Points, 0, 1));
        var content = new FileContainer();
        content.AddSingleMeshScene(mesh);
        content.Scenes[0].Children[0].Transform = new TRSTransform(new Vector3(1,2,3));

        var spaceTransform = new SpaceTransform(Matrix4x4.CreateRotationY(MathF.PI / 2.0f) * Matrix4x4.CreateScale(-1, 1, 1), scale: 1.0f);
        var changeSpace = new SpaceTransformOperator(spaceTransform);
        var transformedContent = changeSpace.Transform(content);

        Assert.Equal(new Vector3(-3, 2, -1), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(-3, 2, -1), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);

        spaceTransform = spaceTransform.Invert();
        changeSpace = new SpaceTransformOperator(spaceTransform);
        transformedContent = changeSpace.Transform(transformedContent);

        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);
    }

    [Fact]
    public void TansformWithScale()
    {
        var rotation = Matrix4x4.CreateFromYawPitchRoll(0.5f,0.0f,0.0f);

        var mesh = new UnifiedIndexedMesh();
        IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
        mesh.AddAttribute(MeshAttributeKey.Position, positions);
        var indices = mesh.Indices;
        indices.Add(positions.Add(new Vector3(1, 2, 3)));
        mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.Points, 0, 1));
        var content = new FileContainer();
        content.AddSingleMeshScene(mesh);
        content.Scenes[0].Children[0].Transform = new TRSTransform(new Vector3(1, 2, 3));

        var spaceTransform = new SpaceTransform(Matrix4x4.CreateRotationY(MathF.PI / 2.0f) * Matrix4x4.CreateScale(-1, 1, 1), 1.1f);
        var changeSpace = new SpaceTransformOperator(spaceTransform);
        var transformedContent = changeSpace.Transform(content);

        Assert.Equal(new Vector3(-3.3f, 2.2f, -1.1f), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(-3.3f, 2.2f, -1.1f), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);

        spaceTransform = spaceTransform.Invert();
        changeSpace = new SpaceTransformOperator(spaceTransform);
        transformedContent = changeSpace.Transform(transformedContent);

        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Scenes[0].Children[0].Transform.ToMatrix().Translation, Vector3EqualityComparer.Default);
        Assert.Equal(new Vector3(1, 2, 3), transformedContent.Meshes[0].GetAttribute<Vector3>(MeshAttributeKey.Position)[0], Vector3EqualityComparer.Default);
    }
}
