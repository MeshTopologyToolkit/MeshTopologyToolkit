using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Property table of an FBX object (<c>Properties70</c> for FBX 7.x, <c>Properties60</c> for legacy files).
    /// Property templates from the Definitions section serve as defaults.
    /// </summary>
    internal sealed class FbxProps
    {
        private readonly struct Entry
        {
            public Entry(FbxNode node, int valueIndex)
            {
                Node = node;
                ValueIndex = valueIndex;
            }

            public FbxNode Node { get; }
            public int ValueIndex { get; }
        }

        private readonly Dictionary<string, Entry> _values = new Dictionary<string, Entry>(StringComparer.Ordinal);

        public static readonly FbxProps Empty = new FbxProps();

        /// <summary>Adds all properties of the <c>Properties70</c>/<c>Properties60</c> child of <paramref name="owner"/>, overriding existing ones.</summary>
        public void Add(FbxNode? owner)
        {
            if (owner == null)
                return;
            foreach (var table in owner.Children)
            {
                if (table.Name == "Properties70")
                {
                    foreach (var p in table.Children)
                    {
                        if (p.Name == "P" && p.Properties.Count >= 1)
                        {
                            var name = p.GetString(0);
                            if (name != null)
                                _values[name] = new Entry(p, 4);
                        }
                    }
                }
                else if (table.Name == "Properties60" || table.Name == "Properties50" || table.Name == "Properties")
                {
                    foreach (var p in table.Children)
                    {
                        if (p.Name == "Property" && p.Properties.Count >= 1)
                        {
                            var name = p.GetString(0);
                            if (name != null)
                                _values[name] = new Entry(p, 3);
                        }
                    }
                }
            }
        }

        /// <summary>Sets a number property, used for synthetic objects.</summary>
        public void SetDouble(string name, double value)
        {
            var node = new FbxNode("P");
            node.Properties.Add(FbxProperty.FromString(name));
            node.Properties.Add(FbxProperty.FromString("double"));
            node.Properties.Add(FbxProperty.FromString("Number"));
            node.Properties.Add(FbxProperty.FromString(""));
            node.Properties.Add(FbxProperty.FromDouble(value));
            _values[name] = new Entry(node, 4);
        }

        public FbxProps Clone()
        {
            var clone = new FbxProps();
            foreach (var kv in _values)
                clone._values[kv.Key] = kv.Value;
            return clone;
        }

        public bool Has(string name) => _values.ContainsKey(name);

        public IEnumerable<string> Names => _values.Keys;

        public double GetDouble(string name, double defaultValue = 0.0)
        {
            if (_values.TryGetValue(name, out var e) && e.ValueIndex < e.Node.Properties.Count)
                return e.Node.Properties[e.ValueIndex].AsDouble(defaultValue);
            return defaultValue;
        }

        public int GetInt(string name, int defaultValue = 0)
        {
            if (_values.TryGetValue(name, out var e) && e.ValueIndex < e.Node.Properties.Count)
                return e.Node.Properties[e.ValueIndex].AsInt(defaultValue);
            return defaultValue;
        }

        public bool GetBool(string name, bool defaultValue = false)
        {
            if (_values.TryGetValue(name, out var e) && e.ValueIndex < e.Node.Properties.Count)
                return e.Node.Properties[e.ValueIndex].AsBool(defaultValue);
            return defaultValue;
        }

        public string? GetString(string name)
        {
            if (_values.TryGetValue(name, out var e) && e.ValueIndex < e.Node.Properties.Count)
                return e.Node.Properties[e.ValueIndex].AsString();
            return null;
        }

        public Vector3 GetVector3(string name, Vector3 defaultValue)
        {
            if (_values.TryGetValue(name, out var e) && e.ValueIndex + 2 < e.Node.Properties.Count + 0)
            {
                var p = e.Node.Properties;
                int i = e.ValueIndex;
                return new Vector3((float)p[i].AsDouble(), (float)p[i + 1].AsDouble(), (float)p[i + 2].AsDouble());
            }
            return defaultValue;
        }

        /// <summary>Gets a color or vector with 3 or 4 components. Missing alpha is reported as 1.</summary>
        public bool TryGetVector4(string name, out Vector4 value)
        {
            value = Vector4.One;
            if (!_values.TryGetValue(name, out var e))
                return false;
            var p = e.Node.Properties;
            int i = e.ValueIndex;
            int count = p.Count - i;
            if (count < 3)
                return false;
            value = new Vector4((float)p[i].AsDouble(), (float)p[i + 1].AsDouble(), (float)p[i + 2].AsDouble(), count >= 4 ? (float)p[i + 3].AsDouble() : 1.0f);
            return true;
        }

        /// <summary>Finds the first property whose name ends with the given suffix, ignoring case. Used for vendor specific names like <c>3dsMax|main|baseColor</c>.</summary>
        public string? FindNameWithSuffix(string suffix)
        {
            foreach (var name in _values.Keys)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                    && (name.Length == suffix.Length || name[name.Length - suffix.Length - 1] == '|'))
                    return name;
            }
            return null;
        }
    }
}
