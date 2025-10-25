using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class RTree3MeshVertexAttributeTests
{
    [Fact]
    public void WeldsCloseVertices()
    {
        var attr = new RTree3MeshVertexAttribute(0.1f);
        int index1 = attr.Add(new Vector3(0, 0, 0));
        int index2 = attr.Add(new Vector3(0.05f, 0, 0));
        int index3 = attr.Add(new Vector3(0.2f, 0, 0));
        Assert.Equal(index1, index2); // Should weld
        Assert.NotEqual(index1, index3); // Should not weld
    }
}
