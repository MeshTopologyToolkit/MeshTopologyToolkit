using System;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    internal struct IndexRange : IEquatable<IndexRange>
    {
        public int StartIndex;
        public int Length;
        public IReadOnlyList<int> Values;

        public IndexRange(IReadOnlyList<int> values, int startIndex, int length)
        {
            Values = values;
            StartIndex = startIndex;
            Length = length;
        }

        public override bool Equals(object? obj)
        {
            return obj is IndexRange span && Equals(span);
        }

        public bool Equals(IndexRange other)
        {
            if (Length != other.Length)
                return false;
            for (int i = 0; i < Length; ++i)
            {
                if (Values[StartIndex + i] != other.Values[other.StartIndex + i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < Length; ++i)
            {
                hash = HashCode.Combine(hash, Values[StartIndex + i]);
            }
            return hash;
        }

        public static bool operator ==(IndexRange left, IndexRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IndexRange left, IndexRange right)
        {
            return !(left == right);
        }
    }
}
