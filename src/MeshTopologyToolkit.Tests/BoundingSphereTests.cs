using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class BoundingSphereTests
{
    [Fact]
    public void SphereFromEmptySetOfPositions()
    {
        var boundingShere = BoundingSphere.RittersBoundingSphere(new Vector3[0]);
        Assert.Equal(new BoundingSphere(), boundingShere);
    }

    [Fact]
    public void SphereFromSinglePosition()
    {
        var boundingShere = BoundingSphere.RittersBoundingSphere(new Vector3[] { Vector3.UnitY});
        Assert.Equal(new BoundingSphere(Vector3.UnitY, 0.0f), boundingShere);
    }

    [Fact]
    public void SphereFromTwoPositions()
    {
        var boundingShere = BoundingSphere.RittersBoundingSphere(new Vector3[] { Vector3.UnitY , 3.0f*Vector3.UnitY });
        Assert.Equal(new BoundingSphere(Vector3.UnitY*2.0f, 1.0f), boundingShere);
    }

    [Fact]
    public void AddRandomPositions_EachPointContained()
    {
        var rnd = new Random(12345);
        var points = new List<Vector3>();
        for (int i = 0; i < 1000; ++i)
        {
            points.Add(new Vector3(
                    rnd.NextSingle(),
                    rnd.NextSingle(),
                    rnd.NextSingle()));
        }
        var boundingShere = BoundingSphere.RittersBoundingSphere(points);

        foreach (var point in points)
        {
            Assert.True(boundingShere.Contains(point));
        }
    }
}

