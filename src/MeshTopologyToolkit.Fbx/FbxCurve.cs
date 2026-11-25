using System;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// A single FBX animation curve (one component of a vector property). Evaluates constant, linear and cubic (Hermite) segments.
    /// </summary>
    internal sealed class FbxCurve
    {
        /// <summary>FBX time is measured in ticks, this many per second.</summary>
        public const double TicksPerSecond = 46186158000.0;

        private const int FlagConstant = 0x2;
        private const int FlagLinear = 0x4;
        private const int FlagCubic = 0x8;

        private readonly double[] _times;
        private readonly float[] _values;
        private readonly int[] _flags;
        private readonly float[] _rightSlope;
        private readonly float[] _nextLeftSlope;

        public FbxCurve(double[] times, float[] values, int[] flags, float[] rightSlope, float[] nextLeftSlope)
        {
            _times = times;
            _values = values;
            _flags = flags;
            _rightSlope = rightSlope;
            _nextLeftSlope = nextLeftSlope;
        }

        public int KeyCount => _times.Length;

        public double GetTime(int index) => _times[index];

        public double StartTime => _times.Length > 0 ? _times[0] : 0.0;

        public double EndTime => _times.Length > 0 ? _times[_times.Length - 1] : 0.0;

        /// <param name="attributesAsBits">
        /// ASCII files of version 7200 and newer store <c>KeyAttrDataFloat</c> as the integer bit patterns of the floats.
        /// </param>
        public static FbxCurve? Read(FbxNode node, bool attributesAsBits = false)
        {
            var ticks = node.Child("KeyTime")?.GetLongs();
            var values = node.Child("KeyValueFloat")?.GetDoubles();
            if (ticks == null || values == null || ticks.Length == 0 || values.Length < ticks.Length)
                return null;

            int count = ticks.Length;
            var times = new double[count];
            var floats = new float[count];
            for (int i = 0; i < count; ++i)
            {
                times[i] = ticks[i] / TicksPerSecond;
                floats[i] = (float)values[i];
            }

            // Attributes are shared by groups of consecutive keys: KeyAttrRefCount tells how many keys use each group.
            var attrFlags = node.Child("KeyAttrFlags")?.GetInts() ?? Array.Empty<int>();
            var attrData = node.Child("KeyAttrDataFloat")?.GetDoubles() ?? Array.Empty<double>();
            if (attributesAsBits)
            {
                attrData = (double[])attrData.Clone();
                for (int i = 0; i < attrData.Length; ++i)
                    attrData[i] = BitConverter.Int32BitsToSingle(unchecked((int)(long)attrData[i]));
            }
            var refCounts = node.Child("KeyAttrRefCount")?.GetInts() ?? Array.Empty<int>();

            var flags = new int[count];
            var right = new float[count];
            var nextLeft = new float[count];
            int key = 0;
            for (int group = 0; group < attrFlags.Length && key < count; ++group)
            {
                int refs = group < refCounts.Length ? refCounts[group] : 1;
                for (int r = 0; r < refs && key < count; ++r, ++key)
                {
                    flags[key] = attrFlags[group];
                    if (group * 4 + 1 < attrData.Length)
                    {
                        right[key] = (float)attrData[group * 4];
                        nextLeft[key] = (float)attrData[group * 4 + 1];
                    }
                }
            }
            for (; key < count; ++key)
                flags[key] = FlagLinear;

            return new FbxCurve(times, floats, flags, right, nextLeft);
        }

        /// <summary>True if the segment starting at the key before <paramref name="time"/> is not linear (needs extra samples).</summary>
        public bool IsCubicAt(double time)
        {
            if (_times.Length < 2 || time < _times[0] || time >= _times[_times.Length - 1])
                return false;
            int i = FindSegment(time);
            return (_flags[i] & FlagConstant) == 0 && (_flags[i] & FlagCubic) != 0;
        }

        public bool IsConstantAt(double time)
        {
            if (_times.Length < 2 || time < _times[0] || time >= _times[_times.Length - 1])
                return false;
            return (_flags[FindSegment(time)] & FlagConstant) != 0;
        }

        public bool IsAllConstant
        {
            get
            {
                for (int i = 0; i + 1 < _flags.Length; ++i)
                    if ((_flags[i] & FlagConstant) == 0)
                        return false;
                return true;
            }
        }

        private int FindSegment(double time)
        {
            int lo = 0, hi = _times.Length - 2;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (_times[mid] <= time)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo;
        }

        public float Evaluate(double time)
        {
            int n = _times.Length;
            if (time <= _times[0] || n == 1)
                return _values[0];
            if (time >= _times[n - 1])
                return _values[n - 1];

            int i = FindSegment(time);
            double t0 = _times[i], t1 = _times[i + 1];
            double dt = t1 - t0;
            if (dt <= 0.0)
                return _values[i + 1];
            int flags = _flags[i];
            if ((flags & FlagConstant) != 0)
                return _values[i];

            double u = (time - t0) / dt;
            if ((flags & FlagCubic) != 0)
            {
                double u2 = u * u, u3 = u2 * u;
                double h00 = 2 * u3 - 3 * u2 + 1;
                double h10 = u3 - 2 * u2 + u;
                double h01 = -2 * u3 + 3 * u2;
                double h11 = u3 - u2;
                return (float)(h00 * _values[i] + h10 * dt * _rightSlope[i] + h01 * _values[i + 1] + h11 * dt * _nextLeftSlope[i]);
            }
            return (float)(_values[i] + (_values[i + 1] - _values[i]) * u);
        }
    }
}
