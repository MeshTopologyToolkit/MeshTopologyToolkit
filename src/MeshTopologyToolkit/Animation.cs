using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshTopologyToolkit
{
    public class Animation
    {
        public Animation(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; set; }

        public IList<AnimationTrack> Tracks { get; } = new List<AnimationTrack>();

        public float StartTime => Tracks.Count == 0 ? 0.0f : Tracks.Min(_ => _.StartTime);

        public float EndTime => Tracks.Count == 0 ? 0.0f : Tracks.Max(_ => _.EndTime);

        public float Duration => Math.Max(0.0f, EndTime - StartTime);

        /// <summary>
        /// Resolves <see cref="AnimationTrack.Target"/> of unbound tracks by node name, searching <paramref name="root"/> and its descendants.
        /// </summary>
        /// <returns>The number of tracks that remain unbound.</returns>
        public int Bind(Node root)
        {
            var nodes = new Dictionary<string, Node>();
            void Add(Node n)
            {
                if (n.Name != null && !nodes.ContainsKey(n.Name))
                    nodes.Add(n.Name, n);
            }
            Add(root);
            foreach (var n in root.VisitAllChildren())
                Add(n);

            int unbound = 0;
            foreach (var track in Tracks)
            {
                if (track.Target != null)
                    continue;
                if (track.TargetName != null && nodes.TryGetValue(track.TargetName, out var node))
                    track.Target = node;
                else
                    ++unbound;
            }
            return unbound;
        }

        /// <summary>
        /// Gets sorted unique times at which all the curves of the <paramref name="track"/> have to be sampled to reproduce them
        /// by linearly interpolating samples. Keys of every curve are included, step curves get an extra key just before each key
        /// and cubic spline curves are sampled at <paramref name="cubicSampleRate"/> samples per second.
        /// </summary>
        public static List<float> GetSampleTimes(AnimationTrack track, float cubicSampleRate = 30.0f, float stepGap = 1e-4f, float mergeGap = 2e-5f)
        {
            var all = new SortedSet<float>();
            AddTimes(track.Translation, all, cubicSampleRate, stepGap);
            AddTimes(track.Rotation, all, cubicSampleRate, stepGap);
            AddTimes(track.Scale, all, cubicSampleRate, stepGap);
            var result = new List<float>(all.Count);
            foreach (var time in all)
            {
                if (result.Count == 0 || time - result[result.Count - 1] > mergeGap)
                    result.Add(time);
            }
            return result;
        }

        private static void AddTimes<T>(AnimationCurve<T>? curve, SortedSet<float> times, float cubicSampleRate, float stepGap) where T : struct
        {
            if (curve == null || curve.Keys.Count == 0)
                return;
            foreach (var key in curve.Keys)
                times.Add(key.Time);
            switch (curve.Interpolation)
            {
                case AnimationInterpolation.Step:
                    for (int i = 1; i < curve.Keys.Count; ++i)
                    {
                        float before = curve.Keys[i].Time - stepGap;
                        if (before > curve.Keys[i - 1].Time + stepGap)
                            times.Add(before);
                    }
                    break;
                case AnimationInterpolation.CubicSpline:
                    if (curve.Keys.Count > 1 && cubicSampleRate > 0.0f)
                    {
                        float step = 1.0f / cubicSampleRate;
                        for (float t = curve.StartTime + step; t < curve.EndTime - step * 0.01f; t += step)
                            times.Add(t);
                    }
                    break;
            }
        }

        public override string ToString() => Name != null ? $"Animation \"{Name}\"" : "Animation";
    }
}
