/*
 * Ported from QuickHull3D, Copyright John E. Lloyd, 2004. All rights reserved.
 * Permission to use, copy, modify and redistribute is granted, provided that
 * this copyright notice is retained and the author is given credit whenever
 * appropriate. See HullPoint.cs for the full notice.
 */

namespace MeshTopologyToolkit
{
    /// <summary>Singly-linked list of faces.</summary>
    internal sealed class HullFaceList
    {
        private HullFace? _tail;

        public HullFace? First { get; private set; }

        public void Clear() => First = _tail = null;

        public void Add(HullFace face)
        {
            if (First == null)
            {
                First = face;
            }
            else
            {
                _tail!.Next = face;
            }
            face.Next = null;
            _tail = face;
        }
    }
}
