using System;
using System.Collections.Generic;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Reads the <c>Takes</c> section of legacy (6.x) FBX files and converts it to the object model of FBX 7.x:
    /// animation stacks, layers, curve nodes and curves connected to the models.
    /// The key format is ported from ufbx (<c>ufbxi_read_take_anim_channel</c>): keys are a heterogeneous list of numbers and bare characters.
    /// </summary>
    internal static class FbxLegacyAnimation
    {
        private const int FlagConstant = 0x2;
        private const int FlagLinear = 0x4;
        private const int FlagCubic = 0x8;

        public static void Read(FbxObjectGraph graph, List<FbxObject> newObjects)
        {
            var takes = graph.Document.Child("Takes");
            if (takes == null)
                return;

            long nextId = -1000;
            foreach (var take in takes.ChildrenNamed("Take"))
            {
                var stack = new FbxObject { Id = nextId--, Class = "AnimationStack", Name = take.GetString(0) ?? "Take", Node = take };
                var layer = new FbxObject { Id = nextId--, Class = "AnimationLayer", Name = "BaseLayer", Node = take };
                Link(layer, stack, null);
                newObjects.Add(stack);
                newObjects.Add(layer);

                foreach (var modelNode in take.ChildrenNamed("Model"))
                {
                    var modelKey = modelNode.GetString(0) ?? "";
                    var model = graph.FindByKey(modelKey) ?? graph.FindByKey("Model::" + modelKey);
                    if (model == null || model == graph.Root)
                        continue;

                    foreach (var transform in modelNode.ChildrenNamed("Channel"))
                    {
                        if (transform.GetString(0) != "Transform")
                            continue;
                        foreach (var propertyChannel in transform.ChildrenNamed("Channel"))
                        {
                            string? property = propertyChannel.GetString(0) switch
                            {
                                "T" => "Lcl Translation",
                                "R" => "Lcl Rotation",
                                "S" => "Lcl Scaling",
                                _ => null,
                            };
                            if (property == null)
                                continue;

                            var curveNode = new FbxObject { Id = nextId--, Class = "AnimationCurveNode", Name = property, Node = propertyChannel };
                            var props = new FbxProps();
                            var curves = new List<(string component, FbxObject curve)>();
                            foreach (var component in propertyChannel.ChildrenNamed("Channel"))
                            {
                                var axis = component.GetString(0);
                                if (axis != "X" && axis != "Y" && axis != "Z")
                                    continue;
                                var parsed = ReadChannel(component, graph.Document.EffectiveVersion, out var defaultValue);
                                props.SetDouble("d|" + axis, defaultValue);
                                if (parsed != null)
                                    curves.Add(("d|" + axis, new FbxObject { Id = nextId--, Class = "AnimationCurve", Name = axis!, Node = component, Curve = parsed }));
                            }
                            if (curves.Count == 0)
                                continue;

                            curveNode.Props = props;
                            newObjects.Add(curveNode);
                            Link(curveNode, layer, null);
                            Link(curveNode, model, property);
                            foreach (var (component, curve) in curves)
                            {
                                newObjects.Add(curve);
                                Link(curve, curveNode, component);
                            }
                        }
                    }
                }
            }
        }

        private static void Link(FbxObject child, FbxObject parent, string? property)
        {
            var link = new FbxConnection(child, parent, property);
            child.ParentLinks.Add(link);
            parent.ChildLinks.Add(link);
        }

        private static List<double> Flatten(FbxNode node)
        {
            var data = new List<double>();
            foreach (var p in node.Properties)
            {
                if (p.IsArray)
                {
                    foreach (var v in new FbxNode("tmp") { Properties = { p } }.GetDoubles())
                        data.Add(v);
                }
                else if (p.Type == FbxPropertyType.String)
                {
                    var s = (string)p.Value;
                    // Bare characters are stored as doubles in binary files.
                    data.Add(s.Length > 0 ? s[0] : 0.0);
                }
                else if (p.IsNumber)
                {
                    data.Add(p.AsDouble());
                }
            }
            return data;
        }

        private static char ToChar(double d) => (char)(int)d;

        public static FbxCurve? ReadChannel(FbxNode channel, int version, out double defaultValue)
        {
            defaultValue = channel.Child("Default")?.GetDouble(0) ?? 0.0;
            var keyNode = channel.Child("Key");
            if (keyNode == null)
                return null;

            int keyVersion = channel.Child("KeyVer")?.GetInt(0) ?? 0;
            if (keyVersion <= 0)
                keyVersion = version < 5000 ? 4003 : version < 6000 ? 4004 : 4005;
            int count = channel.Child("KeyCount")?.GetInt(0) ?? 0;
            if (count <= 0)
                return null;

            var data = Flatten(keyNode);
            int pos = 0;
            int Remaining() => data.Count - pos;
            if (Remaining() < 2)
                return null;

            var times = new double[count];
            var values = new float[count];
            var flags = new int[count];
            var leftSlopes = new float[count];
            var rightSlopes = new float[count];

            double nextTime = data[0] / FbxCurve.TicksPerSecond;
            double nextValue = data[1];
            double previousTime = 0.0;
            float slopeLeft = 0.0f;

            for (int i = 0; i < count; ++i)
            {
                if (Remaining() < 3)
                    return null;
                double time = nextTime;
                double value = nextValue;
                times[i] = time;
                values[i] = (float)value;
                char mode = ToChar(data[pos + 2]);
                pos += 3;

                float slopeRight = 0.0f;
                float nextSlopeLeft = 0.0f;
                bool autoSlope = false;

                if (mode == 'U')
                {
                    flags[i] = FlagCubic;
                    if (Remaining() < 1)
                        return null;
                    char slopeMode = ToChar(data[pos++]);
                    int weights = 1;
                    switch (slopeMode)
                    {
                        case 's':
                        case 'b':
                            if (Remaining() < 2)
                                return null;
                            slopeRight = (float)data[pos];
                            nextSlopeLeft = (float)data[pos + 1];
                            pos += 2;
                            if (keyVersion == 4003)
                                weights = 0;
                            break;
                        case 'a':
                            autoSlope = true;
                            if (keyVersion <= 4004)
                                weights = 0;
                            break;
                        case 'p':
                        case 'q':
                            autoSlope = true;
                            if (Remaining() < 2)
                                return null;
                            pos += 2;
                            weights = keyVersion <= 4004 ? 1 : 2;
                            break;
                        case 't':
                            autoSlope = true;
                            if (Remaining() < 3)
                                return null;
                            pos += 3;
                            weights = 0;
                            break;
                        case 'd':
                            autoSlope = true;
                            if (Remaining() < 1)
                                return null;
                            pos += 1;
                            break;
                        default:
                            return null;
                    }

                    for (; weights > 0; --weights)
                    {
                        if (Remaining() < 1)
                            return null;
                        char weightMode = ToChar(data[pos++]);
                        int extra;
                        switch (weightMode)
                        {
                            case 'n':
                            case 'c': extra = 0; break;
                            case 'a': extra = 2; break;
                            case 'l':
                            case 'r': extra = 1; break;
                            default: return null;
                        }
                        if (Remaining() < extra)
                            return null;
                        pos += extra;
                    }
                }
                else if (mode == 'L')
                {
                    flags[i] = FlagLinear;
                }
                else if (mode == 'C')
                {
                    flags[i] = FlagConstant;
                    if (keyVersion >= 4004)
                    {
                        if (Remaining() < 1)
                            return null;
                        pos += 1;
                    }
                }
                else
                {
                    return null;
                }

                if (i + 1 < count)
                {
                    if (Remaining() < 2)
                        return null;
                    nextTime = data[pos] / FbxCurve.TicksPerSecond;
                    nextValue = data[pos + 1];
                }

                if (autoSlope)
                {
                    // Progressive clamped automatic tangent: flat at extremes, otherwise the slope between the neighbours.
                    if (i > 0 && i + 1 < count)
                    {
                        double before = values[i - 1];
                        bool extremum = (value - before) * (nextValue - value) <= 0.0;
                        double span = nextTime - previousTime;
                        slopeLeft = slopeRight = extremum || span <= 0.0 ? 0.0f : (float)((nextValue - before) / span);
                    }
                    else
                    {
                        slopeLeft = slopeRight = 0.0f;
                    }
                }

                leftSlopes[i] = slopeLeft;
                rightSlopes[i] = slopeRight;
                slopeLeft = nextSlopeLeft;
                previousTime = time;
            }

            var nextLeftSlopes = new float[count];
            for (int i = 0; i + 1 < count; ++i)
                nextLeftSlopes[i] = leftSlopes[i + 1];
            return new FbxCurve(times, values, flags, rightSlopes, nextLeftSlopes);
        }
    }
}
