using System;
using System.Collections.Generic;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// A node of the raw FBX tree: a name, a list of properties and a list of children.
    /// Both ASCII and binary FBX files map to this structure.
    /// </summary>
    public sealed class FbxNode
    {
        public FbxNode(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public List<FbxProperty> Properties { get; } = new List<FbxProperty>();

        public List<FbxNode> Children { get; } = new List<FbxNode>();

        public FbxNode AddChild(string name, params FbxProperty[] properties)
        {
            var child = new FbxNode(name);
            child.Properties.AddRange(properties);
            Children.Add(child);
            return child;
        }

        public FbxNode? Child(string name)
        {
            foreach (var c in Children)
                if (c.Name == name)
                    return c;
            return null;
        }

        public IEnumerable<FbxNode> ChildrenNamed(string name)
        {
            foreach (var c in Children)
                if (c.Name == name)
                    yield return c;
        }

        public string? GetString(int index)
            => index < Properties.Count ? Properties[index].AsString() : null;

        public long GetLong(int index, long defaultValue = 0)
            => index < Properties.Count ? Properties[index].AsLong(defaultValue) : defaultValue;

        public int GetInt(int index, int defaultValue = 0)
            => index < Properties.Count ? Properties[index].AsInt(defaultValue) : defaultValue;

        public double GetDouble(int index, double defaultValue = 0.0)
            => index < Properties.Count ? Properties[index].AsDouble(defaultValue) : defaultValue;

        /// <summary>Gets all numeric values of the node as doubles, whether they are stored as one array or as separate properties.</summary>
        public double[] GetDoubles()
        {
            if (Properties.Count == 1 && Properties[0].IsArray)
            {
                switch (Properties[0].Value)
                {
                    case double[] d: return d;
                    case float[] f: { var r = new double[f.Length]; for (int i = 0; i < r.Length; ++i) r[i] = f[i]; return r; }
                    case int[] n: { var r = new double[n.Length]; for (int i = 0; i < r.Length; ++i) r[i] = n[i]; return r; }
                    case long[] l: { var r = new double[l.Length]; for (int i = 0; i < r.Length; ++i) r[i] = l[i]; return r; }
                    case bool[] b: { var r = new double[b.Length]; for (int i = 0; i < r.Length; ++i) r[i] = b[i] ? 1 : 0; return r; }
                }
            }
            var list = new List<double>(Properties.Count);
            foreach (var p in Properties)
            {
                if (p.IsNumber)
                    list.Add(p.AsDouble());
            }
            return list.ToArray();
        }

        public int[] GetInts()
        {
            if (Properties.Count == 1 && Properties[0].IsArray)
            {
                switch (Properties[0].Value)
                {
                    case int[] n: return n;
                    case long[] l: { var r = new int[l.Length]; for (int i = 0; i < r.Length; ++i) r[i] = (int)l[i]; return r; }
                    case double[] d: { var r = new int[d.Length]; for (int i = 0; i < r.Length; ++i) r[i] = (int)d[i]; return r; }
                    case float[] f: { var r = new int[f.Length]; for (int i = 0; i < r.Length; ++i) r[i] = (int)f[i]; return r; }
                    case bool[] b: { var r = new int[b.Length]; for (int i = 0; i < r.Length; ++i) r[i] = b[i] ? 1 : 0; return r; }
                }
            }
            var list = new List<int>(Properties.Count);
            foreach (var p in Properties)
            {
                if (p.IsNumber)
                    list.Add(p.AsInt());
            }
            return list.ToArray();
        }

        public long[] GetLongs()
        {
            if (Properties.Count == 1 && Properties[0].Value is long[] l)
                return l;
            if (Properties.Count == 1 && Properties[0].IsArray)
            {
                var d = GetDoubles();
                var r = new long[d.Length];
                for (int i = 0; i < r.Length; ++i) r[i] = (long)d[i];
                return r;
            }
            var list = new List<long>(Properties.Count);
            foreach (var p in Properties)
            {
                if (p.IsNumber)
                    list.Add(p.AsLong());
            }
            return list.ToArray();
        }

        public override string ToString() => $"{Name} ({Properties.Count} props, {Children.Count} children)";
    }
}
