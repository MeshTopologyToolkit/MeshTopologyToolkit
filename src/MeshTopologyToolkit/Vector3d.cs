using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Represents a vector with three double-precision floating-point values.
    /// </summary>
    public struct Vector3d : IEquatable<Vector3d>
    {
        /// <summary>
        /// The X component of the vector.
        /// </summary>
        public double X;

        /// <summary>
        /// The Y component of the vector.
        /// </summary>
        public double Y;

        /// <summary>
        /// The Z component of the vector.
        /// </summary>
        public double Z;

        public Vector3d(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public Vector3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double LengthSquared()
        {
            return X*X + Y*Y + Z*Z;
        }

        public static double DistanceSquared(Vector3d a, Vector3d b)
        {
            var dif = b - a;
            return dif.LengthSquared();
        }

        public static double Dot(Vector3d a, Vector3d b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector3d d && Equals(d);
        }

        public bool Equals(Vector3d other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public bool Equals(Vector3d other, double eps)
        {
            var dif = this - other;
            return (dif.X >= -eps) && (dif.X <= eps)
                && (dif.Y >= -eps) && (dif.Y <= eps)
                && (dif.Z >= -eps) && (dif.Z <= eps);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static Vector3d operator *(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static Vector3d operator *(Vector3d a, double scale)
        {
            return new Vector3d(a.X * scale, a.Y * scale, a.Z * scale);
        }

        public static Vector3d operator -(Vector3d value) { return new Vector3d(-value.X, -value.Y, -value.Z);  }

        public static Vector3d operator -(Vector3d left, Vector3d right)
        {
            return new Vector3d(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3d operator +(Vector3d left, Vector3d right)
        {
            return new Vector3d(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static bool operator ==(Vector3d left, Vector3d right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3d left, Vector3d right)
        {
            return !(left == right);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)X, (float)Y, (float)Z);
        }


        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public static Vector3d Cross(Vector3d left, Vector3d right)
        {
            return new Vector3d(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);
        }
   }
}
