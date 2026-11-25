using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// A value over time. Keys are expected to be sorted by time.
    /// </summary>
    public class AnimationCurve<T> where T : struct
    {
        private readonly Func<T, T, float, T> _lerp;
        private readonly Func<T, T, T, T, float, float, T> _hermite;

        internal AnimationCurve(Func<T, T, float, T> lerp, Func<T, T, T, T, float, float, T> hermite)
        {
            _lerp = lerp;
            _hermite = hermite;
        }

        public AnimationInterpolation Interpolation { get; set; } = AnimationInterpolation.Linear;

        public List<AnimationKey<T>> Keys { get; } = new List<AnimationKey<T>>();

        public float StartTime => Keys.Count > 0 ? Keys[0].Time : 0.0f;

        public float EndTime => Keys.Count > 0 ? Keys[Keys.Count - 1].Time : 0.0f;

        public AnimationCurve<T> Add(float time, T value)
        {
            Keys.Add(new AnimationKey<T>(time, value));
            return this;
        }

        public AnimationCurve<T> Add(float time, T value, T inTangent, T outTangent)
        {
            Keys.Add(new AnimationKey<T>(time, value, inTangent, outTangent));
            return this;
        }

        /// <summary>
        /// Evaluates the curve the way a glTF file means it. Times outside of the key range are clamped.
        /// </summary>
        public T Evaluate(float time)
        {
            if (Keys.Count == 0)
                return default;
            if (Keys.Count == 1 || time <= Keys[0].Time)
                return Keys[0].Value;
            var last = Keys[Keys.Count - 1];
            if (time >= last.Time)
                return last.Value;

            // Binary search for the last key with Time <= time.
            int lo = 0, hi = Keys.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (Keys[mid].Time <= time)
                    lo = mid;
                else
                    hi = mid;
            }

            var a = Keys[lo];
            var b = Keys[hi];
            switch (Interpolation)
            {
                case AnimationInterpolation.Step:
                    return a.Value;
                case AnimationInterpolation.CubicSpline:
                    {
                        float dt = b.Time - a.Time;
                        float t = (time - a.Time) / dt;
                        return _hermite(a.Value, a.OutTangent, b.Value, b.InTangent, t, dt);
                    }
                default:
                    {
                        float t = (time - a.Time) / (b.Time - a.Time);
                        return _lerp(a.Value, b.Value, t);
                    }
            }
        }

        /// <summary>
        /// Creates a copy of the curve with every value and tangent mapped by the given functions.
        /// </summary>
        public AnimationCurve<T> Map(Func<T, T> mapValue, Func<T, T>? mapTangent = null)
        {
            mapTangent ??= mapValue;
            var result = new AnimationCurve<T>(_lerp, _hermite) { Interpolation = Interpolation };
            foreach (var key in Keys)
            {
                result.Keys.Add(new AnimationKey<T>(key.Time, mapValue(key.Value), mapTangent(key.InTangent), mapTangent(key.OutTangent)));
            }
            return result;
        }
    }

    public static class AnimationCurve
    {
        public static AnimationCurve<Vector3> CreateVector3(AnimationInterpolation interpolation = AnimationInterpolation.Linear)
        {
            return new AnimationCurve<Vector3>(
                Vector3.Lerp,
                (p0, m0, p1, m1, t, dt) => (Hermite(t, 0) * p0) + (Hermite(t, 1) * dt * m0) + (Hermite(t, 2) * p1) + (Hermite(t, 3) * dt * m1))
            { Interpolation = interpolation };
        }

        public static AnimationCurve<Quaternion> CreateQuaternion(AnimationInterpolation interpolation = AnimationInterpolation.Linear)
        {
            return new AnimationCurve<Quaternion>(
                (a, b, t) =>
                {
                    // Take the short way round.
                    if (Quaternion.Dot(a, b) < 0.0f)
                        b = Quaternion.Negate(b);
                    return Quaternion.Normalize(Quaternion.Slerp(a, b, t));
                },
                (p0, m0, p1, m1, t, dt) =>
                {
                    var r = (p0 * Hermite(t, 0)) + (m0 * (Hermite(t, 1) * dt)) + (p1 * Hermite(t, 2)) + (m1 * (Hermite(t, 3) * dt));
                    return Quaternion.Normalize(r);
                })
            { Interpolation = interpolation };
        }

        // Cubic Hermite basis functions: 0 = p0, 1 = m0, 2 = p1, 3 = m1.
        private static float Hermite(float t, int index)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            switch (index)
            {
                case 0: return 2 * t3 - 3 * t2 + 1;
                case 1: return t3 - 2 * t2 + t;
                case 2: return -2 * t3 + 3 * t2;
                default: return t3 - t2;
            }
        }
    }
}
