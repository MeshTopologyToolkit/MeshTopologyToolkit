using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class Tetrahedralization
    {
        /// <summary>Represents a single tetrahedron defined by four vertices.</summary>
        public class Tetrahedron : IEnumerable<int>
        {
            public int A, B, C, D;

            // Cached geometric properties
            public double CircumRadiusSq { get; private set; }
            public Vector3d CircumCenter { get; private set; }
#if DEBUG
            public double Volume { get; private set; }
#endif

            public Tetrahedron(int v1, int v2, int v3, int v4, IReadOnlyList<Vector3d> positions)
            {
#if DEBUG
                if (v1 == v2 || v1 == v3 || v1 == v4 || v2 == v3 || v2 == v4 || v3 == v4)
                    throw new ArgumentException("Invalid set of vertices");
#endif

                // Ensure canonical ordering (optional, but helpful for debugging/uniqueness)
                A = v1; B = v2; C = v3; D = v4;

                // Recalculate geometric properties immediately
                CalculateCircumSphere(positions);
            }

            // Represents a triangular face of the tetrahedron.
            // Used to define the boundary of the "bad" cavity.
            public Face GetFace(int index)
            {
                // Returns faces: (1,2,3), (1,2,4), (1,3,4), (2,3,4)
                return index switch
                {
                    0 => new Face(A, B, C),
                    1 => new Face(A, B, D),
                    2 => new Face(A, C, D),
                    3 => new Face(B, C, D),
                    _ => throw new IndexOutOfRangeException("Face index must be 0-3.")
                };
            }

            /// <summary>
            /// Calculates the circumcenter and circumradius squared of the tetrahedron
            /// by solving a system of linear equations using determinants (Cramer's Rule).
            /// </summary>
            private void CalculateCircumSphere(IReadOnlyList<Vector3d> positions)
            {
                Vector3d p0 = positions[A];
                Vector3d p1 = positions[B];
                Vector3d p2 = positions[C];
                Vector3d p3 = positions[D];

                Vector3d u1 = p1 - p0;
                Vector3d u2 = p2 - p0;
                Vector3d u3 = p3 - p0;

                double d01 = u1.LengthSquared();
                double d02 = u2.LengthSquared();
                double d03 = u3.LengthSquared();

                var u2u3 = Vector3d.Cross(u2, u3);
                var u3u1 = Vector3d.Cross(u3, u1);
                var u1u2 = Vector3d.Cross(u1, u2);

                var radiusNum = u2u3 * d01 + u3u1 * d02 + u1u2 * d03;
                double radiusDen = 2 * Vector3d.Dot(u1, u2u3);
#if DEBUG
                Volume = Math.Abs(Vector3d.Dot(u1, u2u3)) / 6.0f;
#endif

                if (Math.Abs(radiusDen) < 1e-6)
                {
                    CircumCenter = new Vector3d(double.NaN, double.NaN, double.NaN);
                    CircumRadiusSq = double.NaN;
                    return;
                }

                Vector3d center = p0 + radiusNum * (1.0 / radiusDen);

                // Radius is the minimum distance
                var radiusSquared = (p0 - center).LengthSquared();
                var dist1 = (p1 - center).LengthSquared();
                var dist2 = (p2 - center).LengthSquared();
                var dist3 = (p3 - center).LengthSquared();

                radiusSquared = Math.Min(radiusSquared, dist1);
                radiusSquared = Math.Min(radiusSquared, dist2);
                radiusSquared = Math.Min(radiusSquared, dist3);
                CircumRadiusSq = radiusSquared;
                CircumCenter = center;
            }

            public IEnumerator<int> GetEnumerator()
            {
                yield return A;
                yield return B;
                yield return C;
                yield return D;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Performs the Delaunay Tetrahedralization using the Bowyer-Watson algorithm.
        /// </summary>
        /// <param name="inputPoints">The list of vertices to triangulate.</param>
        /// <returns>A list of Delaunay tetrahedra.</returns>
        public List<Tetrahedron> Generate(IReadOnlyList<Vector3> positions)
        {
            if (positions == null || positions.Count < 4)
            {
                return new List<Tetrahedron>();
            }

            var inputPoints = new List<Vector3d>(positions.Count + 4);
            inputPoints.AddRange(positions.Select(_ => new Vector3d(_)));

            // Create a "Super-Tetrahedron" that bounds all input points.
            // The vertices of this super-tetrahedron will be removed at the end.
            var superTetra = CreateSuperTetrahedron(inputPoints, BoundingSphere.RittersBoundingSphere(positions));
            var tetrahedra = new List<Tetrahedron> { superTetra };

            // Incrementally add each point
            for (int p = 0; p < positions.Count; p++)
            {
                var badTetrahedra = new List<Tetrahedron>();

                // Find all tetrahedra whose circumsphere contains the new point 'p'
                foreach (var t in tetrahedra)
                {
                    if (IsInCircumsphere(t, inputPoints[p]))
                    {
                        badTetrahedra.Add(t);
                    }
                }

                // If no bad tetrahedra, the point is already outside the existing circumspheres
                if (badTetrahedra.Count == 0)
                {
                    continue;
                }

                // Find the boundary of the cavity (the "bad" region)
                var boundaryFaces = FindBoundary(badTetrahedra);

#if DEBUG
                var totalBadVolume = badTetrahedra.Sum(_ => _.Volume);
                var actualVolume = 0.0;
#endif

                // Remove the bad tetrahedra from the main list
                foreach (var t in badTetrahedra)
                {
                    tetrahedra.Remove(t);
                }

                // Retriangulate the cavity: create new tetrahedra from the new point 'p'
                // to each face on the boundary.
                foreach (var face in boundaryFaces)
                {
                    // A face is a list of 3 vertices (vA, vB, vC)
                    var newTetra = new Tetrahedron(p, face.A, face.B, face.C, inputPoints);
                    if (!double.IsNaN(newTetra.CircumRadiusSq))
                        tetrahedra.Add(newTetra);
#if DEBUG
                    actualVolume += newTetra.Volume;
#endif
                }
#if DEBUG
                if (Math.Abs(actualVolume - totalBadVolume) > 1e-6)
                {
                    throw new Exception("Something went wrong");
                }
#endif
            }

            // 3. Remove all tetrahedra that reference any of the super-tetrahedron's vertices.
            var finalTetrahedra = tetrahedra
                .Where(t => !t.Any(v => v >= positions.Count))
                .ToList();

            return finalTetrahedra;
        }

        /// <summary>
        /// Checks if a point p is inside the circumsphere of a tetrahedron t, using Vector3 for distance.
        /// </summary>
        private bool IsInCircumsphere(Tetrahedron t, Vector3d p)
        {
            // Handle degenerate cases gracefully
            if (double.IsNaN(t.CircumRadiusSq)) return false;

            // Calculate the distance squared between the point p and the tetrahedron's circumcenter, 
            // using Vector3.DistanceSquared.
            var distanceSq = Vector3d.DistanceSquared(p, t.CircumCenter);

            // Point is inside the circumsphere if distanceSq < CircumRadiusSq
            return distanceSq < t.CircumRadiusSq;
        }

        /// <summary>
        /// Finds the unique boundary faces of a collection of bad tetrahedra.
        /// A boundary face is shared by exactly one tetrahedron in the collection.
        /// </summary>
        private List<Face> FindBoundary(List<Tetrahedron> badTetrahedra)
        {
            var faceCounts = new Dictionary<Face, int>();

            foreach (var t in badTetrahedra)
            {
                for (int i = 0; i < 4; i++)
                {
                    var face = t.GetFace(i);

                    if (faceCounts.ContainsKey(face))
                    {
                        faceCounts[face]++;
                    }
                    else
                    {
                        faceCounts[face] = 1;
                    }
                }
            }

            // Boundary faces are those that were counted only once
            var boundaryFaces = new List<Face>();
            foreach (var kvp in faceCounts)
            {
                if (kvp.Value == 1)
                {
                    boundaryFaces.Add(kvp.Key);
                }
            }

            return boundaryFaces;
        }

        /// <summary>
        /// Helper struct for identifying and comparing triangular faces robustly.
        /// Uses vertex IDs for comparison.
        /// </summary>
        public struct Face : IEnumerable<int>
        {
            public int A { get; }
            public int B { get; }
            public int C { get; }

            public Face(int a, int b, int c)
            {
                if (a == b || b == c || a == c)
                    throw new ArgumentException("Invalid face indices");

                A = a;
                B = b;
                C = c;
                if (A > B)
                {
                    (A, B) = (B, A);
                }
                if (A > C)
                {
                    (A, C) = (C, A);
                }
                if (B > C)
                {
                    (B, C) = (C, B);
                }
                if (A > B || B > C)
                    throw new Exception();
            }

            // Override Equals and GetHashCode for Dictionary use
            public override bool Equals(object obj)
            {
                if (!(obj is Face other)) return false;
                return A == A &&
                       B == B &&
                       C == C;
            }

            public override int GetHashCode()
            {
                // Combine hash codes of the three unique, ordered IDs
                var code = new HashCode();
                code.Add(A);
                code.Add(B);
                code.Add(C);
                return code.ToHashCode();
            }

            public IEnumerator<int> GetEnumerator()
            {
                yield return A;
                yield return B;
                yield return C;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Creates a large tetrahedron encompassing all points.
        /// </summary>
        /// <returns>A tuple containing the super-tetrahedron and a stating index of super tetrahedron.</returns>
        private Tetrahedron CreateSuperTetrahedron(IList<Vector3d> points, BoundingSphere sphere)
        {
            sphere.BuildEnclosingTetrahedron(out var a, out var b, out var c, out var d);

            // Define four vertices of a tetrahedron large enough to contain the bounding box
            // Using large, unique IDs (negative) to ensure they don't clash with user points.
            int superIdStart = points.Count;
            points.Add(new Vector3d(a));
            points.Add(new Vector3d(b));
            points.Add(new Vector3d(c));
            points.Add(new Vector3d(d));

            var superTetra = new Tetrahedron(superIdStart, superIdStart + 1, superIdStart + 2, superIdStart + 3, (IReadOnlyList<Vector3d>)points);

            return superTetra;

            //// Determine the bounding box of the input points (using float components from Position)
            //double minX = points.Min(p => p.X), maxX = points.Max(p => p.X);
            //double minY = points.Min(p => p.Y), maxY = points.Max(p => p.Y);
            //double minZ = points.Min(p => p.Z), maxZ = points.Max(p => p.Z);

            //// Calculations still use double for intermediate steps
            //var range = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
            //var center = (minX + maxX + minY + maxY + minZ + maxZ) / 6.0;
            //var big = range * 1000.0; // A sufficiently large value

            //// Define four vertices of a tetrahedron large enough to contain the bounding box
            //// Using large, unique IDs (negative) to ensure they don't clash with user points.
            //int superIdStart = points.Count;
            //points.Add(new Vector3d(center, center, center + big));
            //points.Add(new Vector3d(center + big, center - big, center - big));
            //points.Add(new Vector3d(center - big, center + big, center - big));
            //points.Add(new Vector3d(center - big, center - big, center + big));

            //var superTetra = new Tetrahedron(superIdStart, superIdStart+1, superIdStart+2, superIdStart+3, (IReadOnlyList<Vector3d>)points);

            //return superTetra;
        }
    }
}
