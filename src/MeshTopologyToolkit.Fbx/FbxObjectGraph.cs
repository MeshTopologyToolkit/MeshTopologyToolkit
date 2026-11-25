using System;
using System.Collections.Generic;

namespace MeshTopologyToolkit.Fbx
{
    internal sealed class FbxObject
    {
        public long Id;
        public string Name = "";
        public string Class = "";
        public string SubClass = "";
        public FbxNode Node = null!;
        public FbxProps Props = FbxProps.Empty;

        /// <summary>Preparsed curve, set for synthetic animation curves created from legacy files.</summary>
        public FbxCurve? Curve;

        /// <summary>Links where this object is the source (child).</summary>
        public readonly List<FbxConnection> ParentLinks = new List<FbxConnection>();

        /// <summary>Links where this object is the destination (parent).</summary>
        public readonly List<FbxConnection> ChildLinks = new List<FbxConnection>();

        public bool Is(string cls, string? subClass = null) => Class == cls && (subClass == null || SubClass == subClass);

        public override string ToString() => $"{Class}::{Name} ({SubClass}) #{Id}";
    }

    internal sealed class FbxConnection
    {
        public FbxConnection(FbxObject child, FbxObject parent, string? property)
        {
            Child = child;
            Parent = parent;
            Property = property;
        }

        public FbxObject Child { get; }
        public FbxObject Parent { get; }

        /// <summary>Name of the property of the parent the child is connected to (an OP connection), or null (an OO connection).</summary>
        public string? Property { get; }
    }

    /// <summary>
    /// Objects and connections of an FBX document. Legacy (6.x) documents are normalized to the same representation:
    /// objects are identified by their "Class::Name" string and get synthetic ids.
    /// </summary>
    internal sealed class FbxObjectGraph
    {
        public FbxObjectGraph(FbxDocument doc)
        {
            Document = doc;
            Root = new FbxObject { Id = 0, Class = "Root", Name = "RootNode", Node = new FbxNode("Root") };
            ById[0] = Root;
            IsLegacy = doc.EffectiveVersion < 7000;

            var templates = ReadTemplates(doc);
            var objects = doc.Child("Objects");
            if (objects != null)
            {
                long nextId = 1;
                foreach (var node in objects.Children)
                {
                    var obj = new FbxObject { Class = node.Name, Node = node };
                    if (IsLegacy)
                    {
                        var key = node.GetString(0) ?? "";
                        SplitName(key, out var name, out var cls);
                        obj.Name = name;
                        obj.SubClass = node.GetString(1) ?? "";
                        obj.Id = nextId++;
                        // Connections refer to the objects by the full name as written in the file, for example "SubDeformer::Cluster Bone".
                        ByKey[key] = obj;
                        ByKey[node.Name + "::" + name] = obj;
                    }
                    else
                    {
                        obj.Id = node.GetLong(0);
                        SplitName(node.GetString(1) ?? "", out var name, out _);
                        obj.Name = name;
                        obj.SubClass = node.GetString(2) ?? "";
                    }

                    var props = templates.TryGetValue(node.Name, out var template) ? template.Clone() : new FbxProps();
                    props.Add(node);
                    obj.Props = props;
                    ById[obj.Id] = obj;
                    Objects.Add(obj);
                }
            }

            ReadConnections(doc);

            if (IsLegacy)
            {
                var animationObjects = new List<FbxObject>();
                FbxLegacyAnimation.Read(this, animationObjects);
                foreach (var o in animationObjects)
                {
                    ById[o.Id] = o;
                    Objects.Add(o);
                }
            }
        }

        public FbxDocument Document { get; }

        public bool IsLegacy { get; }

        public FbxObject Root { get; }

        public List<FbxObject> Objects { get; } = new List<FbxObject>();

        public Dictionary<long, FbxObject> ById { get; } = new Dictionary<long, FbxObject>();

        /// <summary>Objects by "Class::Name", used by legacy files.</summary>
        public Dictionary<string, FbxObject> ByKey { get; } = new Dictionary<string, FbxObject>(StringComparer.Ordinal);

        public IEnumerable<FbxObject> OfClass(string cls)
        {
            foreach (var o in Objects)
                if (o.Class == cls)
                    yield return o;
        }

        /// <summary>Splits "Name\0\x01Class" (binary) or "Class::Name" (ASCII) into its parts.</summary>
        public static void SplitName(string raw, out string name, out string cls)
        {
            int idx = raw.IndexOf("\0\u0001", StringComparison.Ordinal);
            if (idx >= 0)
            {
                name = raw.Substring(0, idx);
                cls = raw.Substring(idx + 2);
                return;
            }
            idx = raw.IndexOf("::", StringComparison.Ordinal);
            if (idx >= 0)
            {
                cls = raw.Substring(0, idx);
                name = raw.Substring(idx + 2);
                return;
            }
            name = raw;
            cls = "";
        }

        private static Dictionary<string, FbxProps> ReadTemplates(FbxDocument doc)
        {
            var result = new Dictionary<string, FbxProps>(StringComparer.Ordinal);
            var definitions = doc.Child("Definitions");
            if (definitions == null)
                return result;
            foreach (var type in definitions.ChildrenNamed("ObjectType"))
            {
                var className = type.GetString(0);
                var template = type.Child("PropertyTemplate");
                if (className == null || template == null)
                    continue;
                var props = new FbxProps();
                props.Add(template);
                result[className] = props;
            }
            return result;
        }

        private void ReadConnections(FbxDocument doc)
        {
            foreach (var connections in doc.ChildrenNamed("Connections"))
            {
                foreach (var c in connections.Children)
                {
                    FbxObject? child, parent;
                    string? property = null;
                    if (c.Name == "C")
                    {
                        // C: "OO", child, parent[, property]
                        child = FindObject(c, 1);
                        parent = FindObject(c, 2);
                        if (string.Equals(c.GetString(0), "OP", StringComparison.Ordinal))
                            property = c.GetString(3);
                    }
                    else if (c.Name == "Connect")
                    {
                        child = FindObject(c, 1);
                        parent = FindObject(c, 2);
                        if (string.Equals(c.GetString(0), "OP", StringComparison.Ordinal))
                            property = c.GetString(3);
                    }
                    else
                    {
                        continue;
                    }

                    if (child == null || parent == null)
                        continue;
                    var link = new FbxConnection(child, parent, property);
                    child.ParentLinks.Add(link);
                    parent.ChildLinks.Add(link);
                }
            }
        }

        private FbxObject? FindObject(FbxNode connection, int index)
        {
            if (index >= connection.Properties.Count)
                return null;
            var p = connection.Properties[index];
            if (IsLegacy || p.Type == FbxPropertyType.String)
                return FindByKey(p.AsString() ?? "");
            return ById.TryGetValue(p.AsLong(), out var obj) ? obj : null;
        }

        /// <summary>Finds a legacy object by its name as written in the file: "Model::Cube" or "Cube\0\x01Model".</summary>
        public FbxObject? FindByKey(string key)
        {
            if (ByKey.TryGetValue(key, out var found))
                return found;
            SplitName(key, out var name, out var cls);
            if (cls != "" && ByKey.TryGetValue(cls + "::" + name, out found))
                return found;
            if (cls == "Model" && (name == "Scene" || name == "RootNode"))
                return Root;
            return null;
        }
    }
}
