/*
 * Ported from QuickHull3D, Copyright John E. Lloyd, 2004. All rights reserved.
 * Permission to use, copy, modify and redistribute is granted, provided that
 * this copyright notice is retained and the author is given credit whenever
 * appropriate. See HullPoint.cs for the full notice.
 */

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Represents vertices of the hull, as well as the points from which it is formed.
    /// </summary>
    internal sealed class HullVertex
    {
        /// <summary>Spatial point associated with this vertex.</summary>
        public HullPoint Point { get; }

        /// <summary>Back index into an array.</summary>
        public int Index { get; set; }

        /// <summary>List backward link.</summary>
        public HullVertex? Prev { get; set; }

        /// <summary>List forward link.</summary>
        public HullVertex? Next { get; set; }

        /// <summary>Current face that this vertex is outside of.</summary>
        public HullFace? Face { get; set; }

        public HullVertex(double x, double y, double z, int index)
        {
            Point = new HullPoint(x, y, z);
            Index = index;
        }
    }
}
