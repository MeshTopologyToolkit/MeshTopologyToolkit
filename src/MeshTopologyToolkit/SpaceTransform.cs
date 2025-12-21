using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// A class that defines transform from one coordinate space to another.
    /// </summary>
    public class SpaceTransform : IEquatable<SpaceTransform>
    {
        public static readonly SpaceTransform Identity = new SpaceTransform(Matrix4x4.Identity);

        public static readonly Matrix4x4 XYZ = CreateMatrix(Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        public static readonly Matrix4x4 _XYZ = CreateMatrix(-Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ);
        public static readonly Matrix4x4 XZY = CreateMatrix(Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY);
        public static readonly Matrix4x4 _XZY = CreateMatrix(-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY);
        public static readonly Matrix4x4 X_ZY = CreateMatrix(Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY);

        /// <summary>
        /// Rotation matrix for space. Should not contain translation or scale!
        /// </summary>
        public Matrix4x4 Rotation { get; }

        /// <summary>
        /// Uniform scale factor for the transformation.
        /// </summary>
        public float Scale { get; }

        /// <summary>
        /// Flip second texture coordinate.
        /// </summary>
        public bool FlipV { get; }

        /// <summary>
        /// Flip face indices.
        /// </summary>
        public bool FlipFaceIndices { get; }

        public SpaceTransform(Matrix4x4 rotation, float scale = 1.0f, bool flipV = false, bool flipFaceIndices = false)
        {
            if (rotation.Translation != Vector3.Zero)
                throw new ArgumentException("Rotation matrix cannot contain translation.", nameof(rotation));

            var rotX = new Vector3(rotation.M11, rotation.M12, rotation.M13);
            if (!ValidateRotationAxis(rotX))
                throw new ArgumentException($"Rotation matrix axis X is not defined correctly: {rotX}. It should be a unit vector.", nameof(rotation));
            var rotY = new Vector3(rotation.M21, rotation.M22, rotation.M23);
            if (!ValidateRotationAxis(rotY))
                throw new ArgumentException($"Rotation matrix axis Y is not defined correctly: {rotY}. It should be a unit vector.", nameof(rotation));
            var rotZ = new Vector3(rotation.M31, rotation.M32, rotation.M33);
            if (!ValidateRotationAxis(rotZ))
                throw new ArgumentException($"Rotation matrix axis Z is not defined correctly: {rotZ}. It should be a unit vector.", nameof(rotation));

            if (scale <= 0.0f)
                throw new ArgumentException("Scale should be positive. If you need to flip axis direction use rotation matrix instead.", nameof(scale));

            Rotation = rotation;
            Scale = scale;
            FlipV = flipV;
            FlipFaceIndices = flipFaceIndices;
        }

        public SpaceTransformer Transformer => new SpaceTransformer(Rotation, Scale);

        private static bool ValidateRotationAxis(Vector3 axis)
        {
            if (axis.X.IsNanOrInf() || axis.Y.IsNanOrInf() || axis.Z.IsNanOrInf())
                return false;

            int countNotUnitComponents = 0;
            axis = Vector3.Abs(Vector3.Abs(axis) - Vector3.One);
            if (axis.X > 1e-6f)
                ++countNotUnitComponents;
            if (axis.Y > 1e-6f)
                ++countNotUnitComponents;
            if (axis.Z > 1e-6f)
                ++countNotUnitComponents;
            return countNotUnitComponents == 2;
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
            return new SpaceTransform(inverted, 1.0f/Scale, FlipV, FlipFaceIndices);
        }

        public static Matrix4x4 CreateMatrix(Vector3 x, Vector3 y, Vector3 z)
        {
            return new Matrix4x4(x.X, x.Y, x.Z, 0.0f,
                                 y.X, y.Y, y.Z, 0.0f,
                                 z.X, z.Y, z.Z, 0.0f,
                                 0.0f, 0.0f, 0.0f, 1.0f);
        }

        public bool IsIdentity()
        {
            return Rotation == Matrix4x4.Identity && Scale == 1.0f && FlipV == false && FlipFaceIndices == false;
        }
    }
}
