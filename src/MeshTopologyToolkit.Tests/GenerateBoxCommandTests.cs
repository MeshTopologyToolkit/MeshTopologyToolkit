using MeshTopologyToolkit.TrimGenerator;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class GenerateBoxCommandTests
{
    [Fact]
    public void ExactMatchToRecepie()
    {
        var command = new GenerateBoxCommand();
        var args = new TrimGenerationArguments(new int[] { 8, 16, 32, 64, 128, 256, 448 }, width: 1024, bevelInPixels: 8, widthInUnits: 5, uvOffsetInPixels: 0.5f);
        for (int i = 0; i < args.TrimRecepies.Count; ++i)
        {
            var expectedRecepie = args.TrimRecepies[i];
            var mesh = new BoxBuilder(10f);
            mesh.AddSideToBox(args, expectedRecepie.SizeInUnits, Matrix4x4.Identity);

            var bbox = new BoundingBox3(mesh.Positions);
            var size = bbox.Max - bbox.Min;
            Assert.True((size - new Vector3(expectedRecepie.SizeInUnits.X, expectedRecepie.SizeInUnits.Y, 0.0f)).Length() < 1e-6f);

            var uvBox = new BoundingBox2(mesh.TexCoords);
            var uvSize = uvBox.Max - uvBox.Min;
            Assert.True((uvSize - new Vector2(expectedRecepie.TexCoordSize.X, expectedRecepie.TexCoordSize.Y)).Length() < 1e-6f);
        }
    }

    [Fact]
    public void RotatedRecepie()
    {
        var command = new GenerateBoxCommand();
        var args = new TrimGenerationArguments(new int[] { 8, 16, 32, 64, 128, 256, 448 }, width: 1024, bevelInPixels: 8, widthInUnits: 5, uvOffsetInPixels: 0.5f);
        for (int i = 0; i < args.TrimRecepies.Count; ++i)
        {
            var expectedRecepie = args.TrimRecepies[i];
            var mesh = new BoxBuilder(10f);
            mesh.AddSideToBox(args, new Vector2(expectedRecepie.SizeInUnits.Y, expectedRecepie.SizeInUnits.X), Matrix4x4.Identity);

            Assert.Equal(4, mesh.Positions.Count);

            var bbox = new BoundingBox3(mesh.Positions);
            var size = bbox.Max - bbox.Min;
            Assert.True((size - new Vector3(expectedRecepie.SizeInUnits.Y, expectedRecepie.SizeInUnits.X, 0.0f)).Length() < 1e-6f);

            var uvBox = new BoundingBox2(mesh.TexCoords);
            var uvSize = uvBox.Max - uvBox.Min;
            Assert.True((uvSize - new Vector2(expectedRecepie.TexCoordSize.X, expectedRecepie.TexCoordSize.Y)).Length() < 1e-6f);
        }
    }

    [Fact]
    public void HalfSizeRecepie()
    {
        var command = new GenerateBoxCommand();
        var args = new TrimGenerationArguments(new int[] { 8, 16, 32, 64, 128, 256, 448 }, width: 1024, bevelInPixels: 8, widthInUnits: 5, uvOffsetInPixels: 0.5f);
        for (int i = 0; i < args.TrimRecepies.Count; ++i)
        {
            var expectedRecepie = args.TrimRecepies[i];
            var expectedSize = expectedRecepie.SizeInUnits * new Vector2(0.5f, 1.0f);
            var mesh = new BoxBuilder(10f);
            mesh.AddSideToBox(args, expectedSize, Matrix4x4.Identity);

            Assert.Equal(8, mesh.Positions.Count);

            var bbox = new BoundingBox3(mesh.Positions);
            var size = bbox.Max - bbox.Min;
            Assert.True((size - new Vector3(expectedSize.X, expectedSize.Y, 0.0f)).Length() < 1e-6f);

            {
                var uvBox = new BoundingBox2(mesh.TexCoords);
                var uvSize = uvBox.Max - uvBox.Min;
                Assert.True((uvSize - new Vector2(expectedRecepie.TexCoordSize.X, expectedRecepie.TexCoordSize.Y)).Length() < 1e-6f);
            }
            {
                var uvBox1 = new BoundingBox2(mesh.TexCoords.Take(4));
                var uvSize1 = (uvBox1.Max - uvBox1.Min);
                var uvSizeInPixels1 = uvSize1 / args.PixelsToUV;
                Assert.Equal(expectedRecepie.BevelSizeInPixels * 2, (int)uvSizeInPixels1.X);

                var uvBox2 = new BoundingBox2(mesh.TexCoords.Skip(4));
                var uvSize2 = (uvBox2.Max - uvBox2.Min);
                var uvSizeInPixels2 = uvSize2 / args.PixelsToUV;
                //Assert.True((uvSize1 - new Vector2(expectedRecepie.TexCoordSize.X, expectedRecepie.TexCoordSize.Y)).Length() < 1e-6f);
            }
        }
    }
}