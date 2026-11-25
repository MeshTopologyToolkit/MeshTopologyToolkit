using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit.Fbx
{
    internal sealed partial class FbxSceneReader
    {
        private const string TranslationProperty = "Lcl Translation";
        private const string RotationProperty = "Lcl Rotation";
        private const string ScalingProperty = "Lcl Scaling";

        /// <summary>Samples of the animation of one property in one animation layer.</summary>
        private sealed class LayerChannel
        {
            public double Weight = 1.0;
            public bool Additive;
            public readonly FbxCurve?[] Curves = new FbxCurve?[3];
            public Vector3 Defaults;
        }

        private static readonly string[] AnimatedProperties = { TranslationProperty, RotationProperty, ScalingProperty };

        private void ReadAnimations()
        {
            foreach (var stack in _graph.OfClass("AnimationStack"))
            {
                // Model -> property -> channels in layer order.
                var animated = new List<(FbxObject model, Dictionary<string, List<LayerChannel>> channels)>();
                var lookup = new Dictionary<FbxObject, Dictionary<string, List<LayerChannel>>>();

                foreach (var layerLink in stack.ChildLinks)
                {
                    var layer = layerLink.Child;
                    if (layer.Class != "AnimationLayer")
                        continue;
                    double weight = layer.Props.GetDouble("Weight", 100.0) / 100.0;
                    bool additive = layer.Props.GetInt("BlendMode", 0) == 1;

                    foreach (var nodeLink in layer.ChildLinks)
                    {
                        var curveNode = nodeLink.Child;
                        if (curveNode.Class != "AnimationCurveNode")
                            continue;

                        var channel = new LayerChannel { Weight = weight, Additive = additive };
                        channel.Defaults = new Vector3(
                            (float)curveNode.Props.GetDouble("d|X"),
                            (float)curveNode.Props.GetDouble("d|Y"),
                            (float)curveNode.Props.GetDouble("d|Z"));
                        foreach (var curveLink in curveNode.ChildLinks)
                        {
                            if (curveLink.Child.Class != "AnimationCurve" || curveLink.Property == null)
                                continue;
                            int component = curveLink.Property == "d|X" ? 0 : curveLink.Property == "d|Y" ? 1 : curveLink.Property == "d|Z" ? 2 : -1;
                            if (component < 0)
                                continue;
                            channel.Curves[component] = curveLink.Child.Curve ?? FbxCurve.Read(curveLink.Child.Node, !_graph.Document.IsBinary && _graph.Document.EffectiveVersion >= 7200);
                        }
                        if (channel.Curves.All(c => c == null))
                            continue;

                        foreach (var targetLink in curveNode.ParentLinks)
                        {
                            if (targetLink.Property == null || targetLink.Parent.Class != "Model" || Array.IndexOf(AnimatedProperties, targetLink.Property) < 0)
                                continue;
                            if (!lookup.TryGetValue(targetLink.Parent, out var channels))
                            {
                                lookup[targetLink.Parent] = channels = new Dictionary<string, List<LayerChannel>>();
                                animated.Add((targetLink.Parent, channels));
                            }
                            if (!channels.TryGetValue(targetLink.Property, out var list))
                                channels[targetLink.Property] = list = new List<LayerChannel>();
                            list.Add(channel);
                        }
                    }
                }

                var animation = new Animation(stack.Name);
                foreach (var (model, channels) in animated)
                {
                    if (!_nodes.TryGetValue(model, out var node))
                        continue;
                    var track = BuildTrack(model, node, channels);
                    if (track != null)
                        animation.Tracks.Add(track);
                }
                if (animation.Tracks.Count > 0)
                    _content.Add(animation);
            }
        }

        private static Vector3 EvaluateProperty(List<LayerChannel>? channels, Vector3 baseValue, double time, bool multiplicative)
        {
            if (channels == null)
                return baseValue;
            var value = baseValue;
            foreach (var channel in channels)
            {
                var layerValue = new Vector3(
                    channel.Curves[0]?.Evaluate(time) ?? channel.Defaults.X,
                    channel.Curves[1]?.Evaluate(time) ?? channel.Defaults.Y,
                    channel.Curves[2]?.Evaluate(time) ?? channel.Defaults.Z);
                float w = (float)channel.Weight;
                if (channel.Additive)
                {
                    if (multiplicative)
                        value *= Vector3.Lerp(Vector3.One, layerValue, w);
                    else
                        value += layerValue * w;
                }
                else
                {
                    value = Vector3.Lerp(value, layerValue, w);
                }
            }
            return value;
        }

        private AnimationTrack? BuildTrack(FbxObject model, Node node, Dictionary<string, List<LayerChannel>> channels)
        {
            channels.TryGetValue(TranslationProperty, out var translationChannels);
            channels.TryGetValue(RotationProperty, out var rotationChannels);
            channels.TryGetValue(ScalingProperty, out var scalingChannels);

            var curves = channels.Values.SelectMany(c => c).SelectMany(c => c.Curves).Where(c => c != null).Select(c => c!).ToList();
            if (curves.Count == 0)
                return null;

            var baseTransform = FbxNodeTransform.Read(model.Props);
            bool allConstant = curves.All(c => c.IsAllConstant);
            bool needsAll = baseTransform.HasPivotsOrOffsets;

            // Times at which the track is sampled.
            var times = new SortedSet<double>();
            foreach (var curve in curves)
                for (int i = 0; i < curve.KeyCount; ++i)
                    times.Add(curve.GetTime(i));

            if (!allConstant)
            {
                var keyTimes = times.ToList();
                for (int i = 0; i + 1 < keyTimes.Count; ++i)
                {
                    double t0 = keyTimes[i], t1 = keyTimes[i + 1];
                    double dt = t1 - t0;
                    if (dt <= 1e-6)
                        continue;

                    int subdivisions = 1;
                    if (curves.Any(c => c.IsCubicAt(t0)))
                        subdivisions = Math.Max(subdivisions, (int)Math.Ceiling(dt * 30.0));

                    // The rotation is interpolated as a quaternion, so large Euler changes need extra samples to follow the same path.
                    if (rotationChannels != null)
                    {
                        var r0 = EvaluateProperty(rotationChannels, baseTransform.Rotation, t0, false);
                        var r1 = EvaluateProperty(rotationChannels, baseTransform.Rotation, t1, false);
                        var delta = Vector3.Abs(r1 - r0);
                        float maxDelta = Math.Max(delta.X, Math.Max(delta.Y, delta.Z));
                        subdivisions = Math.Max(subdivisions, (int)Math.Ceiling(maxDelta / 45.0f));
                    }

                    subdivisions = Math.Min(subdivisions, 128);
                    for (int s = 1; s < subdivisions; ++s)
                        times.Add(t0 + dt * s / subdivisions);

                    // Constant segments hold the value until the next key, which linear interpolation reproduces with an extra key just before it.
                    if (dt > 2e-4 && curves.Any(c => c.IsConstantAt(t0)))
                        times.Add(t1 - 1e-4);
                }
            }

            var track = new AnimationTrack(node, node.Name);
            var interpolation = allConstant ? AnimationInterpolation.Step : AnimationInterpolation.Linear;
            bool animateT = translationChannels != null || needsAll;
            bool animateR = rotationChannels != null || needsAll;
            bool animateS = scalingChannels != null || needsAll;
            var translationCurve = animateT ? AnimationCurve.CreateVector3(interpolation) : null;
            var rotationCurve = animateR ? AnimationCurve.CreateQuaternion(interpolation) : null;
            var scaleCurve = animateS ? AnimationCurve.CreateVector3(interpolation) : null;

            bool hasPrevious = false;
            Quaternion previous = Quaternion.Identity;
            foreach (var time in times)
            {
                var sample = baseTransform;
                sample.Translation = EvaluateProperty(translationChannels, baseTransform.Translation, time, false);
                sample.Rotation = EvaluateProperty(rotationChannels, baseTransform.Rotation, time, false);
                sample.Scale = EvaluateProperty(scalingChannels, baseTransform.Scale, time, true);

                Vector3 translation = sample.Translation;
                Vector3 scale = sample.Scale;
                Quaternion rotation;
                if (needsAll)
                {
                    if (!Matrix4x4.Decompose(sample.ToMatrix(), out scale, out rotation, out translation))
                    {
                        rotation = Quaternion.CreateFromRotationMatrix(sample.GetRotationMatrix());
                        translation = sample.Translation;
                        scale = sample.Scale;
                    }
                }
                else
                {
                    rotation = Quaternion.CreateFromRotationMatrix(sample.GetRotationMatrix());
                }
                rotation = Quaternion.Normalize(rotation);
                if (hasPrevious && Quaternion.Dot(rotation, previous) < 0.0f)
                    rotation = new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W);
                previous = rotation;
                hasPrevious = true;

                float t = (float)time;
                translationCurve?.Add(t, translation);
                rotationCurve?.Add(t, rotation);
                scaleCurve?.Add(t, scale);
            }

            track.Translation = translationCurve;
            track.Rotation = rotationCurve;
            track.Scale = scaleCurve;
            return track;
        }
    }
}
