/*
 * Ported from QuickHull3D, Copyright John E. Lloyd, 2004. All rights reserved.
 * Permission to use, copy, modify and redistribute is granted, provided that
 * this copyright notice is retained and the author is given credit whenever
 * appropriate. See HullPoint.cs for the full notice.
 */

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Represents the half-edges that surround each face in a counter-clockwise direction.
    /// </summary>
    internal sealed class HullHalfEdge
    {
        /// <summary>Face to the left of this half-edge.</summary>
        public HullFace Face { get; set; }

        public HullHalfEdge Next { get; set; } = null!;

        public HullHalfEdge Prev { get; set; } = null!;

        /// <summary>Half-edge of the adjacent face sharing this edge.</summary>
        public HullHalfEdge Opposite { get; set; } = null!;

        /// <summary>The head vertex of this half-edge.</summary>
        public HullVertex Head { get; }

        public HullHalfEdge(HullVertex head, HullFace face)
        {
            Head = head;
            Face = face;
        }

        public void SetMutualOpposites(HullHalfEdge edge)
        {
            Opposite = edge;
            edge.Opposite = this;
        }

        /// <summary>The tail vertex of this half-edge.</summary>
        public HullVertex Tail => Prev.Head;

        public HullFace OppositeFace => Opposite.Face;

        public string VertexString => $"{Tail.Index}-{Head.Index}";

        public double LengthSquared => Head.Point.DistanceSquared(Tail.Point);
    }
}
