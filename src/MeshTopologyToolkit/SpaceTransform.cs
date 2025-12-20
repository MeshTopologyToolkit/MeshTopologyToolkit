using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public struct SpaceTransform : IEquatable<SpaceTransform>
    {
        public static readonly SpaceTransform Identity = new SpaceTransform(Matrix4x4.Identity);

        /// <summary>
        /// Transform matrix for space.
        /// </summary>
        public Matrix4x4 Transform { get; }

        /// <summary>
        /// Flip second texture coordinate.
        /// </summary>
        public bool FlipV { get; }

        /// <summary>
        /// Flip face indices.
        /// </summary>
        public bool FlipFaceIndices { get; }

        public SpaceTransform(Matrix4x4 transform, bool flipV = false, bool flipFaceIndices = false)
        {
            Transform = transform;
            FlipV = flipV;
            FlipFaceIndices = flipFaceIndices;
        }

        public override bool Equals(object? obj)
        {
            return obj is SpaceTransform transform && Equals(transform);
        }

        public bool Equals(SpaceTransform other)
        {
            return Transform.Equals(other.Transform) &&
                   FlipV == other.FlipV &&
                   FlipFaceIndices == other.FlipFaceIndices;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Transform, FlipV, FlipFaceIndices);
        }

        internal SpaceTransform Invert()
        {
            Matrix4x4.Invert(Transform, out var inverted);
            return new SpaceTransform(inverted, FlipV, FlipFaceIndices);
        }

        public static bool operator ==(SpaceTransform left, SpaceTransform right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SpaceTransform left, SpaceTransform right)
        {
            return !(left == right);
        }
    }
}
