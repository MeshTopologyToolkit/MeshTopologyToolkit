using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MeshTopologyToolkit.Tests")]

namespace MeshTopologyToolkit
{
    /// <summary>
    /// A simple R-tree like spatial index for axis aligned 3D bounding boxes (AABBs).
    /// - Stores an internal list of inserted bounding boxes.
    /// - Provides Insert(BoundingBox3) -> index into internal list.
    /// - Provides Query/Enumerate of indices whose boxes overlap a given query box.
    /// - Provides a bulk constructor that builds the tree from an existing collection.
    /// 
    /// Notes:
    /// - This implementation uses a simple node split strategy with configurable max entries.
    /// - It assumes a <c>BoundingBox3</c> type with <c>Min</c> and <c>Max</c> Vector3 properties
    ///   (common AABB representation). Intersection/merge/volume computations are implemented
    ///   using those vectors.
    /// </summary>
    public class RTree3
    {
        const int DefaultMaxEntries = 8; // typical small fanout, tuneable
        readonly int _maxEntries;
        readonly int _minEntries;

        readonly List<BoundingBox3> _boxes = new List<BoundingBox3>();

        internal Node? _root;

        public int MaxEntries => _maxEntries;

        /// <summary>
        /// Creates an empty RTree3 with default node capacity.
        /// </summary>
        public RTree3() : this(DefaultMaxEntries) { }

        /// <summary>
        /// Creates an empty RTree3 with a specified maximum entries per node.
        /// </summary>
        /// <param name="maxEntries">Maximum number of entries per node (leaf or internal). Must be >= 4.</param>
        public RTree3(int maxEntries)
        {
            if (maxEntries < 4) throw new ArgumentOutOfRangeException(nameof(maxEntries));
            _maxEntries = maxEntries;
            _minEntries = Math.Max(2, maxEntries / 2);
        }

        /// <summary>
        /// Get bounding box at given index.
        /// </summary>
        /// <param name="index">Index of the bounding box.</param>
        /// <returns>Bounding box.</returns>
        public BoundingBox3 this[int index] => _boxes[index];

        /// <summary>
        /// Builds the tree from an existing collection of bounding boxes.
        /// The boxes are copied into the tree's internal list; returned indices refer to that list.
        /// </summary>
        /// <param name="boxes">Collection of bounding boxes to index.</param>
        public RTree3(IEnumerable<BoundingBox3> boxes) : this(DefaultMaxEntries)
        {
            if (boxes == null) throw new ArgumentNullException(nameof(boxes));

            // Copy boxes into internal list first so indices are stable during bulk building.
            foreach (var b in boxes)
            {
                _boxes.Add(b);
            }

            // Bulk-insert by index into tree (avoids duplicate appends).
            for (int i = 0; i < _boxes.Count; ++i)
            {
                InsertIndex(i, _boxes[i]);
            }
        }

        /// <summary>
        /// Inserts a bounding box into the tree and returns its index inside the tree's internal list.
        /// </summary>
        /// <param name="aabb">Axis-aligned bounding box to insert.</param>
        /// <returns>Index of the inserted box in the tree's internal list.</returns>
        public int Insert(BoundingBox3 aabb)
        {
            int index = _boxes.Count;
            _boxes.Add(aabb);
            InsertIndex(index, aabb);
            return index;
        }

        /// <summary>
        /// Enumerates indices of all bounding boxes that overlap the provided query box.
        /// </summary>
        /// <param name="query">Query bounding box.</param>
        /// <returns>IEnumerable of indices into the tree's internal box list.</returns>
        public IEnumerable<int> Query(BoundingBox3 query)
        {
            if (_root == null) yield break;

            var stack = new Stack<Node>();
            stack.Push(_root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (!Intersects(node.Bounds, query)) continue;

                if (node.IsLeaf)
                {
                    // check leaf entries individually
                    foreach (var idx in node.Entries)
                    {
                        if (Intersects(_boxes[idx], query))
                            yield return idx;
                    }
                }
                else
                {
                    foreach (var child in node.Children)
                    {
                        if (Intersects(child.Bounds, query))
                            stack.Push(child);
                    }
                }
            }
        }

        private void InsertIndex(int index, BoundingBox3 box)
        {
            if (_root == null)
            {
                _root = new Node(box);
                _root.Entries.Add(index);
                return;
            }

            var leaf = ChooseLeaf(_root, box);
            // expand leaf bounds
            leaf.Bounds = Merge(leaf.Bounds, box);
            leaf.Entries.Add(index);

            if (leaf.Entries.Count > _maxEntries)
            {
                var (n1, n2) = SplitLeaf(leaf);
                if (leaf == _root)
                {
                    // create new root
                    var newRoot = new Node(Merge(n1.Bounds, n2.Bounds));
                    newRoot.AddChild(n1);
                    newRoot.AddChild(n2);
                    _root = newRoot;
                }
                else
                {
                    // replace leaf in parent with n1 and n2, then handle possible parent overflow up the tree
                    var parent = leaf.Parent!;
                    parent.RemoveChild(leaf);
                    parent.AddChild(n1);
                    parent.AddChild(n2);

                    // adjust parent bounds upward
                    RecalculateBoundsUpwards(parent);

                    // split parents if necessary
                    Node? current = parent;
                    while (current != null && current.Children.Count > _maxEntries)
                    {
                        var (c1, c2) = SplitInternal(current);
                        if (current == _root)
                        {
                            var newRoot = new Node(Merge(c1.Bounds, c2.Bounds));
                            newRoot.AddChild(c1);
                            newRoot.AddChild(c2);
                            _root = newRoot;
                            break;
                        }
                        else
                        {
                            var p = current.Parent!;
                            p.RemoveChild(current);
                            p.AddChild(c1);
                            p.AddChild(c2);
                            RecalculateBoundsUpwards(p);
                            current = p;
                        }
                    }
                }
            }
            else
            {
                // just adjust bounds upwards
                RecalculateBoundsUpwards(leaf.Parent);
            }
        }

        Node ChooseLeaf(Node node, BoundingBox3 box)
        {
            if (node.IsLeaf) return node;

            // choose child that needs smallest volume enlargement to include the box
            Node best = null!;
            double bestInc = double.PositiveInfinity;
            double bestVol = double.PositiveInfinity;

            foreach (var child in node.Children)
            {
                var oldVol = Volume(child.Bounds);
                var merged = Merge(child.Bounds, box);
                var newVol = Volume(merged);
                var inc = newVol - oldVol;

                if (inc < bestInc || (inc == bestInc && oldVol < bestVol))
                {
                    best = child;
                    bestInc = inc;
                    bestVol = oldVol;
                }
            }

            return ChooseLeaf(best, box);
        }

        (Node, Node) SplitLeaf(Node leaf)
        {
            // gather entries and their bounds
            var entries = new List<(int idx, BoundingBox3 box)>(leaf.Entries.Count);
            foreach (var idx in leaf.Entries)
                entries.Add((idx, _boxes[idx]));

            // pick seeds
            var (s1, s2) = PickSeeds(entries, (a, b) => Merge(a.box, b.box));
            var n1 = new Node(s1.box);
            var n2 = new Node(s2.box);
            n1.Entries.Add(s1.idx);
            n2.Entries.Add(s2.idx);

            // distribute remaining
            foreach (var e in entries)
            {
                if (e.idx == s1.idx || e.idx == s2.idx) continue;

                // enforce min entries rule
                if (n1.Entries.Count + 1 + (entries.Count - (n1.Entries.Count + n2.Entries.Count)) < _minEntries)
                {
                    n1.Entries.Add(e.idx);
                    n1.Bounds = Merge(n1.Bounds, e.box);
                    continue;
                }
                if (n2.Entries.Count + 1 + (entries.Count - (n1.Entries.Count + n2.Entries.Count)) < _minEntries)
                {
                    n2.Entries.Add(e.idx);
                    n2.Bounds = Merge(n2.Bounds, e.box);
                    continue;
                }

                var inc1 = Volume(Merge(n1.Bounds, e.box)) - Volume(n1.Bounds);
                var inc2 = Volume(Merge(n2.Bounds, e.box)) - Volume(n2.Bounds);

                if (inc1 < inc2)
                {
                    n1.Entries.Add(e.idx);
                    n1.Bounds = Merge(n1.Bounds, e.box);
                }
                else if (inc2 < inc1)
                {
                    n2.Entries.Add(e.idx);
                    n2.Bounds = Merge(n2.Bounds, e.box);
                }
                else
                {
                    // tie: choose smaller volume, then fewer entries
                    if (Volume(n1.Bounds) < Volume(n2.Bounds) || (Volume(n1.Bounds) == Volume(n2.Bounds) && n1.Entries.Count <= n2.Entries.Count))
                    {
                        n1.Entries.Add(e.idx);
                        n1.Bounds = Merge(n1.Bounds, e.box);
                    }
                    else
                    {
                        n2.Entries.Add(e.idx);
                        n2.Bounds = Merge(n2.Bounds, e.box);
                    }
                }
            }

            //// attach parents
            //var parent = leaf.Parent;
            //parent.AddChild(n1);
            //parent.AddChild(n2);

            return (n1, n2);
        }

        (Node, Node) SplitInternal(Node node)
        {
            // split children into two nodes
            var children = new List<Node>(node.Children);

            var (s1, s2) = PickSeeds(children, (a, b) => Merge(a.Bounds, b.Bounds));
            var n1 = new Node(s1.Bounds);
            var n2 = new Node(s2.Bounds);
            node.RemoveChild(s1);
            node.RemoveChild(s2);
            n1.AddChild(s1);
            n2.AddChild(s2);

            foreach (var child in children)
            {
                if (child == s1 || child == s2) continue;

                node.RemoveChild(child);

                // enforce min entries
                if (n1.Children.Count + 1 + (children.Count - (n1.Children.Count + n2.Children.Count)) < _minEntries)
                {
                    n1.AddChild(child);
                    continue;
                }
                if (n2.Children.Count + 1 + (children.Count - (n1.Children.Count + n2.Children.Count)) < _minEntries)
                {
                    n2.AddChild(child);
                    continue;
                }

                var inc1 = Volume(Merge(n1.Bounds, child.Bounds)) - Volume(n1.Bounds);
                var inc2 = Volume(Merge(n2.Bounds, child.Bounds)) - Volume(n2.Bounds);

                if (inc1 < inc2)
                {
                    n1.AddChild(child);
                }
                else if (inc2 < inc1)
                {
                    n2.AddChild(child); ;
                }
                else
                {
                    if (Volume(n1.Bounds) < Volume(n2.Bounds) || (Volume(n1.Bounds) == Volume(n2.Bounds) && n1.Children.Count <= n2.Children.Count))
                    {
                        n1.AddChild(child);
                    }
                    else
                    {
                        n2.AddChild(child);
                    }
                }
            }

            return (n1, n2);
        }

        // Generic seed picker for either entries or child nodes.
        // The selector returns a bounding box for the item.
        static (T left, T right) PickSeeds<T>(IList<T> items, Func<T, T, BoundingBox3> mergeFunc)
        {
            // pick pair with largest dead space when paired (heuristic used by classic R-tree)
            double bestWaste = double.NegativeInfinity;
            int i1 = 0, i2 = 1;
            for (int i = 0; i < items.Count; ++i)
            {
                for (int j = i + 1; j < items.Count; ++j)
                {
                    var a = items[i];
                    var b = items[j];
                    var mab = mergeFunc(a, b);
                    var waste = Volume(mab) - Volume(GetBoxFor(items, i, mergeFunc)) - Volume(GetBoxFor(items, j, mergeFunc));
                    if (waste > bestWaste)
                    {
                        bestWaste = waste;
                        i1 = i;
                        i2 = j;
                    }
                }
            }
            return (items[i1], items[i2]);

            // helper to get box for item at index (calls mergeFunc with same item to get its box).
            static BoundingBox3 GetBoxFor(IList<T> list, int idx, Func<T, T, BoundingBox3> merge)
            {
                // We assume merge(a,a) == box of a (caller provides merge such that this holds).
                return merge(list[idx], list[idx]);
            }
        }

        void RecalculateBoundsUpwards(Node? node)
        {
            while (node != null)
            {
                if (node.IsLeaf)
                {
                    // compute bounds from entries
                    if (node.Entries != null && node.Entries.Count > 0)
                    {
                        var b = _boxes[node.Entries[0]];
                        for (int i = 1; i < node.Entries.Count; ++i)
                            b = Merge(b, _boxes[node.Entries[i]]);
                        node.Bounds = b;
                    }
                }
                else
                {
                    if (node.Children != null && node.Children.Count > 0)
                    {
                        var b = node.Children[0].Bounds;
                        for (int i = 1; i < node.Children.Count; ++i)
                            b = Merge(b, node.Children[i].Bounds);
                        node.Bounds = b;
                    }
                }
                node = node.Parent;
            }
        }

        static BoundingBox3 Merge(BoundingBox3 a, BoundingBox3 b)
        {
            return a.Union(b);
        }

        static double Volume(BoundingBox3 b)
        {
            var d = b.Max - b.Min;
            // guard against negative/degenerate extents
            var x = Math.Max(0.0, d.X);
            var y = Math.Max(0.0, d.Y);
            var z = Math.Max(0.0, d.Z);
            return x * y * z;
        }

        static bool Intersects(BoundingBox3 a, BoundingBox3 b)
        {
            // standard AABB overlap test
            return !(a.Max.X < b.Min.X || a.Min.X > b.Max.X
                  || a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y
                  || a.Max.Z < b.Min.Z || a.Min.Z > b.Max.Z);
        }

        // internal node type
        internal class Node
        {
            public BoundingBox3 Bounds = BoundingBox3.Empty;

            public Node? Parent { get; private set; }

            // leaf data
            public List<int> Entries { get; }

            private List<Node> _children;

            // internal node data
            public IReadOnlyList<Node> Children => _children;

            public bool IsLeaf => _children.Count == 0;

            public Node(BoundingBox3 bounds)
            {
                Bounds = bounds;
                Entries = new List<int>();
                _children = new List<Node>();
            }

            public void AddChild(Node child)
            {
                if (child.Parent == this)
                    throw new InvalidOperationException("Node is already a child of this parent.");
                if (child.Parent != null)
                    throw new InvalidOperationException("Node already has a parent.");
                _children.Add(child);
                child.Parent = this;
                Bounds = Merge(Bounds, child.Bounds);
            }

            internal void RemoveChild(Node current)
            {
                if (current.Parent != this)
                    throw new InvalidOperationException("Node is not a child of this parent.");
                _children.Remove(current);
                current.Parent = null;
            }
        }
    }
}
