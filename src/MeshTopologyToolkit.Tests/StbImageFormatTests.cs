namespace MeshTopologyToolkit.Tests;

public class StbImageFormatTests
{
    private static Color32[] BuildPixels(int width, int height)
    {
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
                pixels[x + y * width] = new Color32((byte)(x * 16), (byte)(y * 16), (byte)((x + y) * 8), 255);
        return pixels;
    }

    private static ImageContainer WriteAndRead(string fileName, Color32[] pixels, int width, int height)
    {
        var files = new Dictionary<string, byte[]>();
        var format = new StbImageFormat();
        Assert.True(format.TryWrite(new InMemoryFileSystemEntry(fileName, files), new ImageContainer(new LDRImageMipMap(width, height, 1, pixels))));
        Assert.True(format.TryRead(new InMemoryFileSystemEntry(fileName, files), out var image));
        return image;
    }

    [Theory]
    [InlineData("image.png")]
    [InlineData("image.bmp")]
    [InlineData("image.tga")]
    public void LosslessFormatsRoundTrip(string fileName)
    {
        var pixels = BuildPixels(16, 8);
        var mip = WriteAndRead(fileName, pixels, 16, 8)[0];
        Assert.Equal(16, mip.Width);
        Assert.Equal(8, mip.Height);
        var actual = mip.GetPixels();
        for (int i = 0; i < pixels.Length; ++i)
        {
            Assert.Equal(pixels[i].R, actual[i].R);
            Assert.Equal(pixels[i].G, actual[i].G);
            Assert.Equal(pixels[i].B, actual[i].B);
        }
    }

    [Fact]
    public void JpegRoundTripIsClose()
    {
        var pixels = BuildPixels(16, 16);
        var mip = WriteAndRead("image.jpg", pixels, 16, 16)[0];
        Assert.Equal(16, mip.Width);
        Assert.Equal(16, mip.Height);
        var actual = mip.GetPixels();
        for (int i = 0; i < pixels.Length; ++i)
            Assert.InRange(Math.Abs(pixels[i].R - actual[i].R), 0, 40);
    }

    [Theory]
    [InlineData("image.webp")]
    [InlineData("image.tiff")]
    [InlineData("image.gif")]
    public void UnsupportedFormatsAreNotWritten(string fileName)
    {
        var files = new Dictionary<string, byte[]>();
        var container = new ImageContainer(new LDRImageMipMap(2, 2, 1, BuildPixels(2, 2)));
        Assert.False(new StbImageFormat().TryWrite(new InMemoryFileSystemEntry(fileName, files), container));
    }
}
