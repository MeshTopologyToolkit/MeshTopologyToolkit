/*
 * Ported from QuickHull3D, Copyright John E. Lloyd, 2004. All rights reserved.
 * Permission to use, copy, modify and redistribute is granted, provided that
 * this copyright notice is retained and the author is given credit whenever
 * appropriate. See HullPoint.cs for the full notice.
 */

using System;

namespace MeshTopologyToolkit
{
    internal sealed class InternalHullException : Exception
    {
        public InternalHullException(string message) : base(message) { }
    }

    /// <summary>
    /// Face used to form the hull: a planar normal, a plane offset and a circular
    /// list of half-edges surrounding the face counter-clockwise.
    /// </summary>
    internal sealed class HullFace
    {
        public const int Visible = 1;
        public const int NonConvex = 2;
        public const int Deleted = 3;

        private double _planeOffset;

        public HullHalfEdge He0 { get; private set; } = null!;
        public HullPoint Normal { get; } = new HullPoint();
        public double Area { get; private set; }
        public HullPoint Centroid { get; } = new HullPoint();
        public HullFace? Next { get; set; }
        public int Mark { get; set; } = Visible;
        public HullVertex? Outside { get; set; }
        public int VertexCount { get; private set; }

        private void ComputeCentroid(HullPoint centroid)
        {
            centroid.SetAll(0.0);
            var he = He0;
            do
            {
                centroid.Add(he.Head.Point);
                he = he.Next;
            }
            while (he != He0);
            centroid.Scale(1.0 / VertexCount);
        }

        private void ComputeNormal(HullPoint normal, double minArea)
        {
            ComputeNormal(normal);

            if (Area < minArea)
            {
                // make the normal more robust by removing
                // components parallel to the longest edge
                HullHalfEdge? hedgeMax = null;
                double lenSqrMax = 0;
                var hedge = He0;
                do
                {
                    double lenSqr = hedge.LengthSquared;
                    if (lenSqr > lenSqrMax)
                    {
                        hedgeMax = hedge;
                        lenSqrMax = lenSqr;
                    }
                    hedge = hedge.Next;
                }
                while (hedge != He0);

                var p2 = hedgeMax!.Head.Point;
                var p1 = hedgeMax.Tail.Point;
                double lenMax = Math.Sqrt(lenSqrMax);
                double ux = (p2.X - p1.X) / lenMax;
                double uy = (p2.Y - p1.Y) / lenMax;
                double uz = (p2.Z - p1.Z) / lenMax;
                double dot = normal.X * ux + normal.Y * uy + normal.Z * uz;
                normal.X -= dot * ux;
                normal.Y -= dot * uy;
                normal.Z -= dot * uz;

                normal.Normalize();
            }
        }

        private void ComputeNormal(HullPoint normal)
        {
            var he1 = He0.Next;
            var he2 = he1.Next;

            var p0 = He0.Head.Point;
            var p2 = he1.Head.Point;

            double d2x = p2.X - p0.X;
            double d2y = p2.Y - p0.Y;
            double d2z = p2.Z - p0.Z;

            normal.SetAll(0.0);

            VertexCount = 2;

            while (he2 != He0)
            {
                double d1x = d2x;
                double d1y = d2y;
                double d1z = d2z;

                p2 = he2.Head.Point;
                d2x = p2.X - p0.X;
                d2y = p2.Y - p0.Y;
                d2z = p2.Z - p0.Z;

                normal.X += d1y * d2z - d1z * d2y;
                normal.Y += d1z * d2x - d1x * d2z;
                normal.Z += d1x * d2y - d1y * d2x;

                he1 = he2;
                he2 = he2.Next;
                VertexCount++;
            }
            Area = normal.Norm;
            normal.Scale(1 / Area);
        }

        private void ComputeNormalAndCentroid()
        {
            ComputeNormal(Normal);
            ComputeCentroid(Centroid);
            _planeOffset = Normal.Dot(Centroid);
            int numv = 0;
            var he = He0;
            do
            {
                numv++;
                he = he.Next;
            }
            while (he != He0);
            if (numv != VertexCount)
            {
                throw new InternalHullException($"face {VertexString} numVerts={VertexCount} should be {numv}");
            }
        }

        private void ComputeNormalAndCentroid(double minArea)
        {
            ComputeNormal(Normal, minArea);
            ComputeCentroid(Centroid);
            _planeOffset = Normal.Dot(Centroid);
        }

        /// <summary>Constructs a triangular face from vertices v0, v1 and v2.</summary>
        public static HullFace CreateTriangle(HullVertex v0, HullVertex v1, HullVertex v2, double minArea = 0)
        {
            var face = new HullFace();
            var he0 = new HullHalfEdge(v0, face);
            var he1 = new HullHalfEdge(v1, face);
            var he2 = new HullHalfEdge(v2, face);

            he0.Prev = he2;
            he0.Next = he1;
            he1.Prev = he0;
            he1.Next = he2;
            he2.Prev = he1;
            he2.Next = he0;

            face.He0 = he0;

            face.ComputeNormalAndCentroid(minArea);
            return face;
        }

        /// <summary>Gets the i-th half-edge of the face (negative values walk backwards).</summary>
        public HullHalfEdge GetEdge(int i)
        {
            var he = He0;
            while (i > 0)
            {
                he = he.Next;
                i--;
            }
            while (i < 0)
            {
                he = he.Prev;
                i++;
            }
            return he;
        }

        /// <summary>Distance from a point to the plane of this face.</summary>
        public double DistanceToPlane(HullPoint p)
            => Normal.X * p.X + Normal.Y * p.Y + Normal.Z * p.Z - _planeOffset;

        public string VertexString
        {
            get
            {
                var s = "";
                var he = He0;
                do
                {
                    if (s.Length > 0)
                    {
                        s += " ";
                    }
                    s += he.Head.Index;
                    he = he.Next;
                }
                while (he != He0);
                return s;
            }
        }

        private HullFace? ConnectHalfEdges(HullHalfEdge hedgePrev, HullHalfEdge hedge)
        {
            HullFace? discardedFace = null;

            if (hedgePrev.OppositeFace == hedge.OppositeFace)
            {
                // there is a redundant edge that we can get rid of
                var oppFace = hedge.OppositeFace;
                HullHalfEdge hedgeOpp;

                if (hedgePrev == He0)
                {
                    He0 = hedge;
                }
                if (oppFace.VertexCount == 3)
                {
                    // we can get rid of the opposite face altogether
                    hedgeOpp = hedge.Opposite.Prev.Opposite;

                    oppFace.Mark = Deleted;
                    discardedFace = oppFace;
                }
                else
                {
                    hedgeOpp = hedge.Opposite.Next;

                    if (oppFace.He0 == hedgeOpp.Prev)
                    {
                        oppFace.He0 = hedgeOpp;
                    }
                    hedgeOpp.Prev = hedgeOpp.Prev.Prev;
                    hedgeOpp.Prev.Next = hedgeOpp;
                }
                hedge.Prev = hedgePrev.Prev;
                hedge.Prev.Next = hedge;

                hedge.Opposite = hedgeOpp;
                hedgeOpp.Opposite = hedge;

                // oppFace was modified, so need to recompute
                oppFace.ComputeNormalAndCentroid();
            }
            else
            {
                hedgePrev.Next = hedge;
                hedge.Prev = hedgePrev;
            }
            return discardedFace;
        }

        public void CheckConsistency()
        {
            var hedge = He0;
            int numv = 0;

            if (VertexCount < 3)
            {
                throw new InternalHullException($"degenerate face: {VertexString}");
            }
            do
            {
                var hedgeOpp = hedge.Opposite;
                if (hedgeOpp == null)
                {
                    throw new InternalHullException($"face {VertexString}: unreflected half edge {hedge.VertexString}");
                }
                if (hedgeOpp.Opposite != hedge)
                {
                    throw new InternalHullException($"face {VertexString}: opposite half edge {hedgeOpp.VertexString} has opposite {hedgeOpp.Opposite.VertexString}");
                }
                if (hedgeOpp.Head != hedge.Tail || hedge.Head != hedgeOpp.Tail)
                {
                    throw new InternalHullException($"face {VertexString}: half edge {hedge.VertexString} reflected by {hedgeOpp.VertexString}");
                }
                var oppFace = hedgeOpp.Face;
                if (oppFace == null)
                {
                    throw new InternalHullException($"face {VertexString}: no face on half edge {hedgeOpp.VertexString}");
                }
                if (oppFace.Mark == Deleted)
                {
                    throw new InternalHullException($"face {VertexString}: opposite face {oppFace.VertexString} not on hull");
                }
                numv++;
                hedge = hedge.Next;
            }
            while (hedge != He0);

            if (numv != VertexCount)
            {
                throw new InternalHullException($"face {VertexString} numVerts={VertexCount} should be {numv}");
            }
        }

        public int MergeAdjacentFace(HullHalfEdge hedgeAdj, HullFace[] discarded)
        {
            var oppFace = hedgeAdj.OppositeFace;
            int numDiscarded = 0;

            discarded[numDiscarded++] = oppFace;
            oppFace.Mark = Deleted;

            var hedgeOpp = hedgeAdj.Opposite;

            var hedgeAdjPrev = hedgeAdj.Prev;
            var hedgeAdjNext = hedgeAdj.Next;
            var hedgeOppPrev = hedgeOpp.Prev;
            var hedgeOppNext = hedgeOpp.Next;

            while (hedgeAdjPrev.OppositeFace == oppFace)
            {
                hedgeAdjPrev = hedgeAdjPrev.Prev;
                hedgeOppNext = hedgeOppNext.Next;
            }

            while (hedgeAdjNext.OppositeFace == oppFace)
            {
                hedgeOppPrev = hedgeOppPrev.Prev;
                hedgeAdjNext = hedgeAdjNext.Next;
            }

            for (var hedge = hedgeOppNext; hedge != hedgeOppPrev.Next; hedge = hedge.Next)
            {
                hedge.Face = this;
            }

            if (hedgeAdj == He0)
            {
                He0 = hedgeAdjNext;
            }

            // handle the half edges at the head
            var discardedFace = ConnectHalfEdges(hedgeOppPrev, hedgeAdjNext);
            if (discardedFace != null)
            {
                discarded[numDiscarded++] = discardedFace;
            }

            // handle the half edges at the tail
            discardedFace = ConnectHalfEdges(hedgeAdjPrev, hedgeOppNext);
            if (discardedFace != null)
            {
                discarded[numDiscarded++] = discardedFace;
            }

            ComputeNormalAndCentroid();
            CheckConsistency();

            return numDiscarded;
        }

        public void Triangulate(HullFaceList newFaces, double minArea)
        {
            if (VertexCount < 4)
            {
                return;
            }

            var v0 = He0.Head;

            var hedge = He0.Next;
            var oppPrev = hedge.Opposite;
            HullFace? face0 = null;

            for (hedge = hedge.Next; hedge != He0.Prev; hedge = hedge.Next)
            {
                var face = CreateTriangle(v0, hedge.Prev.Head, hedge.Head, minArea);
                face.He0.Next.SetMutualOpposites(oppPrev);
                face.He0.Prev.SetMutualOpposites(hedge.Opposite);
                oppPrev = face.He0;
                newFaces.Add(face);
                if (face0 == null)
                {
                    face0 = face;
                }
            }
            hedge = new HullHalfEdge(He0.Prev.Prev.Head, this);
            hedge.SetMutualOpposites(oppPrev);

            hedge.Prev = He0;
            hedge.Prev.Next = hedge;

            hedge.Next = He0.Prev;
            hedge.Next.Prev = hedge;

            ComputeNormalAndCentroid(minArea);
            CheckConsistency();

            for (var face = face0; face != null; face = face.Next)
            {
                face.CheckConsistency();
            }
        }
    }
}
