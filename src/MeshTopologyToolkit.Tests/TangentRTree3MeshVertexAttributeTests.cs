using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class TangentRTree3MeshVertexAttributeTests
{
    [Fact]
    public void WeldsCloseTangents()
    {
        var attr = new TangentRTree3MeshVertexAttribute(0.1f);
        int index1 = attr.Add(new Vector4(1, 0, 0, 1));
        int index2 = attr.Add(new Vector4(1, 0, 0, -1));
        int index3 = attr.Add(new Vector4(0.99f, 0, 0, 1));
        int index4 = attr.Add(new Vector4(1.01f, 0, 0, -1));
        Assert.Equal(index1, index3); // Should weld
        Assert.Equal(index2, index4); // Should weld
        Assert.NotEqual(index1, index2); // Should not weld
        Assert.NotEqual(index3, index4); // Should not weld
    }
}
