using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace MeshTopologyToolkit
{
    public struct StridedIndexRange : IEquatable<StridedIndexRange>, IReadOnlyList<int>
    {
        public int StartIndex { get; }
        public IReadOnlyList<int> Values { get; }

        public StridedIndexRange(IReadOnlyList<int> values, int startIndex, int numAttributes)
        {
            Values = values;
            StartIndex = startIndex;
            Count = numAttributes;
        }

        public int this[int index] => Values[StartIndex+index];

        public int Count { get; }

        public override bool Equals(object? obj)
        {
            return obj is StridedIndexRange span && Equals(span);
        }

        public bool Equals(StridedIndexRange other)
        {
            if (Count != other.Count)
                return false;
            for (int i = 0; i < Count; ++i)
            {
                if (Values[StartIndex + i] != other.Values[other.StartIndex + i])
                    return false;
            }
            return true;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = StartIndex; i<StartIndex+Count; ++i)
            {
                yield return Values[i];
            }
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < Count; ++i)
            {
                hash = HashCode.Combine(hash, Values[StartIndex + i]);
            }
            return hash;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool operator ==(StridedIndexRange left, StridedIndexRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StridedIndexRange left, StridedIndexRange right)
        {
            return !(left == right);
        }
    }
}
