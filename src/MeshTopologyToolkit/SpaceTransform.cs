using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public struct SpaceTransform : IEquatable<SpaceTransform>
    {
        public static readonly SpaceTransform Identity = new SpaceTransform(Matrix4x4.Identity);

        /// <summary>
        /// Rotation matrix for space. Should not contain translation or scale!
        /// </summary>
        public Matrix4x4 Rotation { get; }

        /// <summary>
        /// Scale vector for the space.
        /// </summary>
        public Vector3 Scale { get; }

        /// <summary>
        /// Flip second texture coordinate.
        /// </summary>
        public bool FlipV { get; }

        /// <summary>
        /// Flip face indices.
        /// </summary>
        public bool FlipFaceIndices { get; }

        public SpaceTransform(Matrix4x4 rotation, bool flipV = false, bool flipFaceIndices = false)
            :this(rotation, Vector3.One, flipV, flipFaceIndices)
        {
        }

        public SpaceTransform(Matrix4x4 rotation, Vector3 scale, bool flipV = false, bool flipFaceIndices = false)
        {
            if (rotation.Translation != Vector3.Zero)
                throw new ArgumentException("Rotation matrix cannot contain translation.", nameof(rotation));

            if (scale.X <= 0.0f && scale.Y <= 0.0f && scale.Z <= 0.0f)
                throw new ArgumentException("Scale should be positive. If you need to flip axis direction use rotation matrix instead.", nameof(scale));

            Rotation = rotation;
            Scale = scale;
            FlipV = flipV;
            FlipFaceIndices = flipFaceIndices;
        }

        public override bool Equals(object? obj)
        {
            return obj is SpaceTransform transform && Equals(transform);
        }

        public bool Equals(SpaceTransform other)
        {
            return Rotation.Equals(other.Rotation) &&
                   Scale == other.Scale &&
                   FlipV == other.FlipV &&
                   FlipFaceIndices == other.FlipFaceIndices;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Rotation, Scale, FlipV, FlipFaceIndices);
        }

        internal SpaceTransform Invert()
        {
            Matrix4x4.Invert(Rotation, out var inverted);
            return new SpaceTransform(inverted, new Vector3(1.0f/Scale.X, 1.0f / Scale.Y, 1.0f / Scale.Z), FlipV, FlipFaceIndices);
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
