using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Represents a sphere in 3D space defined by a center point and a radius.
    /// </summary>
    /// <remarks>The <see cref="BoundingSphere"/> is used to perform spatial calculations such as collision
    /// detection and visibility testing. It provides methods to determine if a point is contained within the sphere,
    /// merge with other points, and compute the minimal bounding sphere for a set of points using Ritter's
    /// algorithm.</remarks>
    public struct BoundingSphere : IEquatable<BoundingSphere>
    {
        /// <summary>
        /// Center of the bounding sphere.
        /// </summary>
        public Vector3 Center { get; }

        /// <summary>
        /// Radius of the bounding sphere.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingSphere"/> class with the specified center and radius.
        /// </summary>
        /// <remarks>The <see cref="BoundingSphere"/> represents a sphere in 3D space defined by a center
        /// point and a radius.</remarks>
        /// <param name="center">The center point of the bounding sphere.</param>
        /// <param name="radius">The radius of the bounding sphere. Must be a non-negative value.</param>
        public BoundingSphere(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingSphere"/> class that encompasses the specified points.
        /// </summary>
        /// <remarks>This constructor uses Ritter's algorithm to calculate the smallest bounding sphere
        /// that can contain all the specified points.</remarks>
        /// <param name="points">An array of <see cref="Vector3"/> points that the bounding sphere will encompass. Must not be null or empty.</param>
        public BoundingSphere(params Vector3[] points)
        {
            this = RittersBoundingSphere(points);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingSphere"/> struct that encompasses the specified points.
        /// </summary>
        /// <remarks>The bounding sphere is calculated using Ritter's algorithm, which provides an
        /// efficient approximation of the minimal bounding sphere.</remarks>
        /// <param name="points">A read-only list of <see cref="Vector3"/> points that the bounding sphere will encompass. Must contain at
        /// least one point.</param>
        public BoundingSphere(IReadOnlyList<Vector3> points)
        {
            this = RittersBoundingSphere(points);
        }

        /// <summary>
        /// Determines whether the specified point is contained within this bounding sphere.
        /// Points on the boundary are considered contained.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <returns><c>true</c> if <paramref name="point"/> lies inside or on the bounds of the sphere; otherwise <c>false</c>.</returns>
        public bool Contains(Vector3 point)
        {
            return Vector3.DistanceSquared(point, Center) <= Radius * Radius;
        }

        /// <summary>
        /// Returns a new bounding sphere that contains both this sphere and the specified point.
        /// </summary>
        /// <param name="position">The point to include in the resulting sphere.</param>
        /// <returns>A bounding box that encloses this sphere and <paramref name="other"/>.</returns>
        public BoundingSphere Merge(Vector3 position)
        {
            // Check if the point 'p' is outside the current sphere
            float distSqToCenter = Vector3.DistanceSquared(position, Center);

            if (distSqToCenter > Radius*Radius)
            {
                // The point is outside. We must expand the sphere to enclose it.
                float distance = (float)Math.Sqrt(distSqToCenter);

                // The new radius is set to cover the original sphere plus the distance to the new point
                float newRadius = (Radius + distance) * 0.5f;

                // 'k' is the fraction we need to move the center from 'center' towards 'p'
                // The new center will lie on the line segment connecting the old center and the outlier point.
                float k = (newRadius - Radius) / distance;

                // Vector from old center to outlier point
                Vector3 centerToP = position - Center;

                // Move the center along the vector centerToP by factor k
                return new BoundingSphere(Center + centerToP * k, newRadius);
            }
            return this;
        }

        /// <summary>
        /// Computes the smallest bounding sphere that encloses a set of 3D points using Ritter's algorithm.
        /// </summary>
        /// <param name="points">A read-only list of 3D points to be enclosed by the bounding sphere. Must contain at least one point.</param>
        /// <returns>A <see cref="BoundingSphere"/> that minimally encloses all the specified points. If the list is empty,
        /// returns a sphere with zero radius at the origin.</returns>
        public static BoundingSphere RittersBoundingSphere(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count == 0)
            {
                return new BoundingSphere(Vector3.Zero, 0.0f);
            }
            if (points.Count == 1)
            {
                return new BoundingSphere(points[0], 0.0f);
            }
            if (points.Count == 2)
            {
                return new BoundingSphere((points[0] + points[1]) * 0.5f, Vector3.Distance(points[0], points[1]) * 0.5f);
            }

            // Find the initial diameter (two farthest points)
            Vector3 p1 = points[0];
            Vector3 p2 = points[0];
            float maxDistSq = -float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    float currentDistSq = Vector3.DistanceSquared(points[i], points[j]);
                    if (currentDistSq > maxDistSq)
                    {
                        maxDistSq = currentDistSq;
                        p1 = points[i];
                        p2 = points[j];
                    }
                }
            }

            var result = new BoundingSphere((p1 + p2) * 0.5f, (float)Math.Sqrt(maxDistSq) * 0.5f);

            // Iteratively expand the sphere to include all points
            foreach (var p in points)
            {
                result = result.Merge(p);
            }

            return result;
        }

        /// <summary>
        /// Build 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        public void BuildEnclosingTetrahedron(out Vector3 a, out Vector3 b, out Vector3 c, out Vector3 d)
        {
            a = new Vector3(3, 3, 3) * Radius;
            b = new Vector3(3, -3, -3) * Radius;
            c = new Vector3(-3, 3, -3) * Radius;
            d = new Vector3(-3, -3, 3) * Radius;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is BoundingSphere sphere && Equals(sphere);
        }

        /// <summary>
        /// Determines whether the specified <see cref="BoundingSphere"/> is equal to the current <see
        /// cref="BoundingSphere"/>.
        /// </summary>
        /// <param name="other">The <see cref="BoundingSphere"/> to compare with the current <see cref="BoundingSphere"/>.</param>
        /// <returns><see langword="true"/> if the specified <see cref="BoundingSphere"/> is equal to the current <see
        /// cref="BoundingSphere"/>; otherwise, <see langword="false"/>.</returns>
        public bool Equals(BoundingSphere other)
        {
            return Center.Equals(other.Center) &&
                   Radius == other.Radius;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Center, Radius);
        }

        /// <summary>
        /// Determines whether two <see cref="BoundingSphere"/> instances are equal.
        /// </summary>
        /// <param name="left">The first <see cref="BoundingSphere"/> to compare.</param>
        /// <param name="right">The second <see cref="BoundingSphere"/> to compare.</param>
        /// <returns><see langword="true"/> if the specified <see cref="BoundingSphere"/> instances are equal; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool operator ==(BoundingSphere left, BoundingSphere right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="BoundingSphere"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="BoundingSphere"/> to compare.</param>
        /// <param name="right">The second <see cref="BoundingSphere"/> to compare.</param>
        /// <returns><see langword="true"/> if the two <see cref="BoundingSphere"/> instances are not equal; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool operator !=(BoundingSphere left, BoundingSphere right)
        {
            return !(left == right);
        }
    }
}
