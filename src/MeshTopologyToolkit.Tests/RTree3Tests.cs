using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class RTree3Tests
{
    [Fact]
    public void SplitRoot()
    {
        float boxHalfSize = 0.1f;
        var tree = new RTree3();
        for (int i = 0; i < tree.MaxEntries+2; ++i)
        {
            tree.Insert(new BoundingBox3(Vector3.UnitX * i, boxHalfSize));
        }

        var root = tree._root!;
        Assert.Equal(2, root.Children.Count);
        Assert.Equal(root, root.Children[0].Parent);
        Assert.Equal(root, root.Children[1].Parent);
    }

    [Fact]
    public void AddRandomPositions_TreeIsBalanced()
    {
        float weldRadius = 0.1f;
        var tree = new RTree3();
        var rnd = new Random(12345);
        for (int i=0; i<1000; ++i)
        {
            tree.Insert(new BoundingBox3(
                new Vector3(
                    rnd.NextSingle(),
                    rnd.NextSingle(),
                    rnd.NextSingle()),
                weldRadius));
        }

        IEnumerable<Tuple<int, RTree3.Node>> GetLeavesWithDepth(RTree3.Node node, int depth)
        {
            if (node.IsLeaf)
                yield return Tuple.Create(depth, node);
            foreach (var child in node.Children)
            {
                foreach (var res in GetLeavesWithDepth(child, depth + 1))
                    yield return res;
            }
        }

        var leaves = GetLeavesWithDepth(tree._root!, 0).OrderBy(_=>_.Item1).ToList();

        Assert.DoesNotContain(leaves, _ => _.Item1 != 3);
    }

    [Fact]
    public void AddRandomPositions_EachElementDiscoverable()
    {
        float weldRadius = 0.1f;
        var tree = new RTree3();
        var rnd = new Random(12345);
        for (int i = 0; i < 1000; ++i)
        {
            tree.Insert(new BoundingBox3(
                new Vector3(
                    rnd.NextSingle(),
                    rnd.NextSingle(),
                    rnd.NextSingle()),
                weldRadius));
        }

        IEnumerable<RTree3.Node> GetLeaves(RTree3.Node node)
        {
            if (node.IsLeaf)
                yield return node;
            foreach (var child in node.Children)
            {
                foreach (var res in GetLeaves(child))
                    yield return res;
            }
        }

        foreach (var node in GetLeaves(tree._root!))
        {
            foreach (var index in node.Entries)
            {
                var box = tree.Query(tree[index]).Contains(index);
            }
        }
    }
}