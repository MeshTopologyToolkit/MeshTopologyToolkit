/*
 * Ported from QuickHull3D, Copyright John E. Lloyd, 2004. All rights reserved.
 * Permission to use, copy, modify and redistribute is granted, provided that
 * this copyright notice is retained and the author is given credit whenever
 * appropriate. See HullPoint.cs for the full notice.
 */

namespace MeshTopologyToolkit
{
    /// <summary>Doubly-linked list of vertices.</summary>
    internal sealed class HullVertexList
    {
        private HullVertex? _tail;

        public HullVertex? First { get; private set; }

        public bool IsEmpty => First == null;

        public void Clear() => First = _tail = null;

        public void Add(HullVertex vtx)
        {
            if (First == null)
            {
                First = vtx;
            }
            else
            {
                _tail!.Next = vtx;
            }
            vtx.Prev = _tail;
            vtx.Next = null;
            _tail = vtx;
        }

        /// <summary>Adds a chain of vertices to the end of this list.</summary>
        public void AddRange(HullVertex vtx)
        {
            if (First == null)
            {
                First = vtx;
            }
            else
            {
                _tail!.Next = vtx;
            }
            vtx.Prev = _tail;
            while (vtx.Next != null)
            {
                vtx = vtx.Next;
            }
            _tail = vtx;
        }

        public void Remove(HullVertex vtx)
        {
            if (vtx.Prev == null)
            {
                First = vtx.Next;
            }
            else
            {
                vtx.Prev.Next = vtx.Next;
            }
            if (vtx.Next == null)
            {
                _tail = vtx.Prev;
            }
            else
            {
                vtx.Next.Prev = vtx.Prev;
            }
        }

        /// <summary>Removes a chain of vertices from this list.</summary>
        public void Remove(HullVertex vtx1, HullVertex vtx2)
        {
            if (vtx1.Prev == null)
            {
                First = vtx2.Next;
            }
            else
            {
                vtx1.Prev.Next = vtx2.Next;
            }
            if (vtx2.Next == null)
            {
                _tail = vtx1.Prev;
            }
            else
            {
                vtx2.Next.Prev = vtx1.Prev;
            }
        }

        public void InsertBefore(HullVertex vtx, HullVertex next)
        {
            vtx.Prev = next.Prev;
            if (next.Prev == null)
            {
                First = vtx;
            }
            else
            {
                next.Prev.Next = vtx;
            }
            vtx.Next = next;
            next.Prev = vtx;
        }
    }
}
