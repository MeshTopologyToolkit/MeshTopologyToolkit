namespace MeshTopologyToolkit.Tests;

public class UncompressedDDSImageFormatTests
{
    [Fact]
    public void TryWrite_AndTryRead_RoundTripsLegacyDds()
    {
        var format = new UncompressedDDSImageFormat(false);
        var source = new ImageContainer(new LDRImageMipMap(2, 1, 1, new[]
        {
            new Color32(1, 2, 3, 4),
            new Color32(10, 20, 30, 40),
        }));

        using var stream = new MemoryStream();
        Assert.True(format.TryWrite(new StreamFileSystemEntry(() => stream, "legacy.dds"), source));

        Assert.True(format.TryRead(new StreamFileSystemEntry(() => new MemoryStream(stream.ToArray()), "legacy.dds"), out var image));

        Assert.Single(image);
        Assert.Equal(2, image[0].Width);
        Assert.Equal(1, image[0].Height);
        Assert.Equal(1, image[0].Depth);
        Assert.Equal(new Color32(1, 2, 3, 4), image[0].GetPixels()[0]);
        Assert.Equal(new Color32(10, 20, 30, 40), image[0].GetPixels()[1]);
    }

    [Fact]
    public void TryWrite_AndTryRead_RoundTripsDx10Dds()
    {
        var format = new UncompressedDDSImageFormat(true);
        var source = new ImageContainer(new LDRImageMipMap(2, 1, 1, new[]
        {
            new Color32(11, 12, 13, 14),
            new Color32(21, 22, 23, 24),
        }));

        using var stream = new MemoryStream();
        Assert.True(format.TryWrite(new StreamFileSystemEntry(() => stream, "dx10.dds"), source));

        Assert.True(format.TryRead(new StreamFileSystemEntry(() => new MemoryStream(stream.ToArray()), "dx10.dds"), out var image));

        Assert.Single(image);
        Assert.Equal(2, image[0].Width);
        Assert.Equal(1, image[0].Height);
        Assert.Equal(new Color32(11, 12, 13, 14), image[0].GetPixels()[0]);
        Assert.Equal(new Color32(21, 22, 23, 24), image[0].GetPixels()[1]);
    }
}
