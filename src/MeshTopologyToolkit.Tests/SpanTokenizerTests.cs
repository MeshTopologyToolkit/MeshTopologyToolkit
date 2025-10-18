namespace MeshTopologyToolkit.Tests;

public class SpanTokenizerTests
{
    [Fact]
    public void EmptyString_IsEndOfStream()
    {
        Assert.True(new SpanTokenizer("").IsEndOfStream);
    }

    [Fact]
    public void TryReadFloat_ToTheEnd()
    {
        Assert.True(new SpanTokenizer("1.2").TryReadFloat(out var val));
        Assert.Equal(1.2f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_ToTheWhitespace()
    {
        Assert.True(new SpanTokenizer("1.2 1").TryReadFloat(out var val));
        Assert.Equal(1.2f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_ToTheComma()
    {
        Assert.True(new SpanTokenizer("1.2,1").TryReadFloat(out var val));
        Assert.Equal(1.2f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_LeadingMinus()
    {
        Assert.True(new SpanTokenizer("-1.2").TryReadFloat(out var val));
        Assert.Equal(-1.2f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_LeadingPlus()
    {
        Assert.True(new SpanTokenizer("+1.2").TryReadFloat(out var val));
        Assert.Equal(1.2f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_ExpFormat()
    {
        Assert.True(new SpanTokenizer("1.2e+1").TryReadFloat(out var val));
        Assert.Equal(12f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_ShortExpFormat()
    {
        Assert.True(new SpanTokenizer("1e1").TryReadFloat(out var val));
        Assert.Equal(10f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_NegativeExpFormat()
    {
        Assert.True(new SpanTokenizer("1e-1").TryReadFloat(out var val));
        Assert.Equal(0.1f, val, 1e-6f);
    }

    [Fact]
    public void TryReadFloat_UppercaseExpFormat()
    {
        Assert.True(new SpanTokenizer("1.2E+1").TryReadFloat(out var val));
        Assert.Equal(12f, val, 1e-6f);
    }
}