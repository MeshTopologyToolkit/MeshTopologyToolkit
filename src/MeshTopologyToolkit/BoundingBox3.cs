using System.Numerics;

namespace MeshTopologyToolkit
{
    public struct BoundingBox3
    {
        public static readonly BoundingBox3 Zero = new BoundingBox3(Vector3.Zero, Vector3.Zero);
        public static readonly BoundingBox3 Empty = new BoundingBox3(new Vector3(float.MaxValue), new Vector3(float.MinValue));

        private Vector3 _min;
        private Vector3 _max;

        public BoundingBox3(Vector3 min, Vector3 max)
        {
            _min = min;
            _max = max;
        }

        public BoundingBox3(Vector3 center, float halfExtent)
        {
            _min = center - new Vector3(halfExtent);
            _max = center + new Vector3(halfExtent);
        }

        public bool IsEmpty => _min.X > _max.X || _min.Y > _max.Y || _min.Z > _max.Z;

        public Vector3 Min => _min;

        public Vector3 Max => _max;

        public BoundingBox3 Union(BoundingBox3 other)
        {
            return new BoundingBox3(Vector3.Min(_min, other._min), Vector3.Max(_max, other._max));
        }

        public BoundingBox3 Union(Vector3 other)
        {
            return new BoundingBox3(Vector3.Min(_min, other), Vector3.Max(_max, other));
        }

        public bool Intersects(BoundingBox3 other)
        {
            return (_min.X <= other._max.X && _max.X >= other._min.X) &&
                   (_min.Y <= other._max.Y && _max.Y >= other._min.Y) &&
                   (_min.Z <= other._max.Z && _max.Z >= other._min.Z);
        }

        public bool Contains(Vector3 point)
        {
            return (point.X >= _min.X && point.X <= _max.X) &&
                   (point.Y >= _min.Y && point.Y <= _max.Y) &&
                   (point.Z >= _min.Z && point.Z <= _max.Z);
        }

        public override string ToString()
        {
            return $"({_min}, {_max})";
        }
    }
}
