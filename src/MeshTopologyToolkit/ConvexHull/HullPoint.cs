/*
 * Ported from QuickHull3D, Copyright John E. Lloyd, 2004. All rights reserved.
 * Permission to use, copy, modify and redistribute is granted, provided that
 * this copyright notice is retained and the author is given credit whenever
 * appropriate.
 *
 * This software is distributed "as is", without any warranty, including
 * any implied warranty of merchantability or fitness for a particular
 * use. The author assumes no responsibility for, and shall not be liable
 * for, any special, indirect, or consequential damages, or any damages
 * whatsoever, arising out of or in connection with the use of this
 * software.
 */

using System;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Mutable 3D vector used internally by the QuickHull implementation.
    /// </summary>
    internal sealed class HullPoint
    {
        private const double DoublePrecision = 2.2204460492503131e-16;

        public double X;
        public double Y;
        public double Z;

        public HullPoint() { }

        public HullPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public void Set(HullPoint v) => Set(v.X, v.Y, v.Z);

        public void Set(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void SetAll(double value) => Set(value, value, value);

        public void Add(HullPoint v) => Set(X + v.X, Y + v.Y, Z + v.Z);

        public void Sub(HullPoint v1, HullPoint v2) => Set(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);

        public void Scale(double s) => Set(s * X, s * Y, s * Z);

        public double NormSquared => Dot(this);

        public double Norm => Math.Sqrt(NormSquared);

        public double DistanceSquared(HullPoint v)
        {
            var dx = X - v.X;
            var dy = Y - v.Y;
            var dz = Z - v.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        public double Distance(HullPoint v) => Math.Sqrt(DistanceSquared(v));

        public double Dot(HullPoint v) => X * v.X + Y * v.Y + Z * v.Z;

        public void Normalize()
        {
            double lenSqr = NormSquared;
            if (Math.Abs(lenSqr - 1) > 2 * DoublePrecision)
            {
                Scale(1.0 / Math.Sqrt(lenSqr));
            }
        }

        public void Cross(HullPoint v1, HullPoint v2)
            => Set(v1.Y * v2.Z - v1.Z * v2.Y,
                   v1.Z * v2.X - v1.X * v2.Z,
                   v1.X * v2.Y - v1.Y * v2.X);
    }
}
