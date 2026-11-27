/*
 * Ported from QuickHull3D, Copyright John E. Lloyd, 2004. All rights reserved.
 * Permission to use, copy, modify and redistribute is granted, provided that
 * this copyright notice is retained and the author is given credit whenever
 * appropriate. See HullPoint.cs for the full notice.
 */

using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Computes the convex hull of a set of three dimensional points.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The algorithm is a three dimensional implementation of Quickhull, as described in
    /// Barber, Dobkin, and Huhdanpaa, "The Quickhull Algorithm for Convex Hulls"
    /// (ACM Transactions on Mathematical Software, Vol. 22, No. 4, December 1996), and has
    /// a complexity of O(n log(n)) with respect to the number of points. This is a port of
    /// John E. Lloyd's QuickHull3D.
    /// </para>
    /// <para>
    /// After <see cref="Build(IReadOnlyList{Vector3})"/> the hull faces are convex polygons (coplanar
    /// triangles are merged). Call <see cref="Triangulate"/> or use <see cref="ToMesh"/> to get triangles.
    /// </para>
    /// </remarks>
    public class ConvexHull
    {
        /// <summary>
        /// Specifies that the distance tolerance should be computed automatically from the input point data.
        /// </summary>
        public const double AutomaticTolerance = -1;

        private const double DoublePrecision = 2.2204460492503131e-16;
        private const int NonConvexWrtLargerFace = 1;
        private const int NonConvexMerge = 2;

        private HullVertex[] _points = Array.Empty<HullVertex>();
        private int[] _vertexPointIndices = Array.Empty<int>();
        private readonly HullFace[] _discardedFaces = new HullFace[3];
        private readonly HullVertex[] _maxVtxs = new HullVertex[3];
        private readonly HullVertex[] _minVtxs = new HullVertex[3];
        private readonly List<HullFace> _faces = new List<HullFace>(16);
        private readonly List<HullHalfEdge> _horizon = new List<HullHalfEdge>(16);
        private readonly HullFaceList _newFaces = new HullFaceList();
        private readonly HullVertexList _unclaimed = new HullVertexList();
        private readonly HullVertexList _claimed = new HullVertexList();
        private double _charLength;
        private int _numVertices;
        private Vector3d[]? _verticesCache;
        private int[][]? _facesCache;

        /// <summary>
        /// The distance tolerance used for the most recently computed hull.
        /// </summary>
        public double DistanceTolerance { get; private set; }

        /// <summary>
        /// Explicit distance tolerance for convexity tests. If <see cref="AutomaticTolerance"/>
        /// (the default), the tolerance is computed from the point data.
        /// </summary>
        public double ExplicitDistanceTolerance { get; set; } = AutomaticTolerance;

        /// <summary>
        /// Hull vertices (a subset of the input points).
        /// </summary>
        public IReadOnlyList<Vector3d> Vertices
        {
            get
            {
                if (_verticesCache == null)
                {
                    var result = new Vector3d[_numVertices];
                    for (int i = 0; i < _numVertices; i++)
                    {
                        var p = _points[_vertexPointIndices[i]].Point;
                        result[i] = new Vector3d(p.X, p.Y, p.Z);
                    }
                    _verticesCache = result;
                }
                return _verticesCache;
            }
        }

        /// <summary>
        /// For each hull vertex, the index of the corresponding input point.
        /// </summary>
        public IReadOnlyList<int> VertexPointIndices
        {
            get
            {
                var result = new int[_numVertices];
                Array.Copy(_vertexPointIndices, result, _numVertices);
                return result;
            }
        }

        /// <summary>
        /// Hull faces. Each face is a list of indices into <see cref="Vertices"/>,
        /// arranged counter-clockwise when seen from outside of the hull.
        /// </summary>
        public IReadOnlyList<int[]> Faces
        {
            get
            {
                if (_facesCache == null)
                {
                    var result = new int[_faces.Count][];
                    int k = 0;
                    foreach (var face in _faces)
                    {
                        var indices = new int[face.VertexCount];
                        var hedge = face.He0;
                        int i = 0;
                        do
                        {
                            indices[i++] = hedge.Head.Index;
                            hedge = hedge.Next;
                        }
                        while (hedge != face.He0);
                        result[k++] = indices;
                    }
                    _facesCache = result;
                }
                return _facesCache;
            }
        }

        /// <summary>
        /// Constructs the convex hull of a set of points.
        /// </summary>
        /// <exception cref="ArgumentException">The number of input points is less than four,
        /// or the points appear to be coincident, colinear, or coplanar.</exception>
        public void Build(IReadOnlyList<Vector3> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }
            var converted = new Vector3d[points.Count];
            for (int i = 0; i < converted.Length; i++)
            {
                converted[i] = new Vector3d(points[i]);
            }
            Build(converted);
        }

        /// <summary>
        /// Constructs the convex hull of a set of points.
        /// </summary>
        /// <exception cref="ArgumentException">The number of input points is less than four,
        /// or the points appear to be coincident, colinear, or coplanar.</exception>
        public void Build(IReadOnlyList<Vector3d> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }
            if (points.Count < 4)
            {
                throw new ArgumentException("Less than four input points specified", nameof(points));
            }

            _verticesCache = null;
            _facesCache = null;
            _numVertices = 0;
            _faces.Clear();
            _claimed.Clear();
            _unclaimed.Clear();

            _points = new HullVertex[points.Count];
            _vertexPointIndices = new int[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                _points[i] = new HullVertex(p.X, p.Y, p.Z, i);
            }
            BuildHull();
        }

        /// <summary>
        /// Triangulates any non-triangular hull faces. In some cases, due to precision issues,
        /// the resulting triangles may be very thin or small, and hence appear to be non-convex.
        /// </summary>
        public void Triangulate()
        {
            double minArea = 1000 * _charLength * DoublePrecision;
            _newFaces.Clear();
            foreach (var face in _faces)
            {
                if (face.Mark == HullFace.Visible)
                {
                    face.Triangulate(_newFaces, minArea);
                }
            }
            for (var face = _newFaces.First; face != null; face = face.Next)
            {
                _faces.Add(face);
            }
            _facesCache = null;
        }

        /// <summary>
        /// Checks the correctness of the hull: no faces are non-convex and no input points are
        /// outside any face. If the hull has been triangulated, this may fail for very thin triangles.
        /// </summary>
        /// <param name="tolerance">Distance tolerance; <see cref="DistanceTolerance"/> by default.</param>
        /// <returns>true if the hull is valid.</returns>
        public bool Check(double? tolerance = null)
        {
            double tol = tolerance ?? DistanceTolerance;
            double pointTol = 10 * tol;

            try
            {
                foreach (var face in _faces)
                {
                    if (face.Mark == HullFace.Visible && !CheckFaceConvexity(face, DistanceTolerance))
                    {
                        return false;
                    }
                }
            }
            catch (InternalHullException)
            {
                return false;
            }

            foreach (var vertex in _points)
            {
                foreach (var face in _faces)
                {
                    if (face.Mark == HullFace.Visible && face.DistanceToPlane(vertex.Point) > pointTol)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Converts the hull to a triangle mesh with counter-clockwise winding (outward facing).
        /// </summary>
        /// <param name="flatNormals">
        /// If true, vertices are duplicated per face and a flat normal is generated for each of them.
        /// Otherwise the mesh shares hull vertices and has positions only.
        /// </param>
        public UnifiedIndexedMesh ToMesh(bool flatNormals = false)
        {
            var mesh = new UnifiedIndexedMesh();
            var positions = new ListMeshVertexAttribute<Vector3>();
            var hullVertices = Vertices;
            var faces = Faces;

            if (!flatNormals)
            {
                foreach (var v in hullVertices)
                {
                    positions.Add(v.ToVector3());
                }
                foreach (var face in faces)
                {
                    for (int i = 1; i + 1 < face.Length; i++)
                    {
                        mesh.Indices.Add(face[0]);
                        mesh.Indices.Add(face[i]);
                        mesh.Indices.Add(face[i + 1]);
                    }
                }
                mesh.AddAttribute(MeshAttributeKey.Position, positions);
            }
            else
            {
                var normals = new ListMeshVertexAttribute<Vector3>();
                foreach (var face in faces)
                {
                    var p0 = hullVertices[face[0]].ToVector3();
                    var normal = Vector3.Zero;
                    for (int i = 1; i + 1 < face.Length; i++)
                    {
                        var p1 = hullVertices[face[i]].ToVector3();
                        var p2 = hullVertices[face[i + 1]].ToVector3();
                        normal += Vector3.Cross(p1 - p0, p2 - p0);
                    }
                    normal = normal.LengthSquared() > 0 ? Vector3.Normalize(normal) : Vector3.UnitY;

                    int baseIndex = positions.Count;
                    foreach (var vertexIndex in face)
                    {
                        positions.Add(hullVertices[vertexIndex].ToVector3());
                        normals.Add(normal);
                    }
                    for (int i = 1; i + 1 < face.Length; i++)
                    {
                        mesh.Indices.Add(baseIndex);
                        mesh.Indices.Add(baseIndex + i);
                        mesh.Indices.Add(baseIndex + i + 1);
                    }
                }
                mesh.AddAttribute(MeshAttributeKey.Position, positions);
                mesh.AddAttribute(MeshAttributeKey.Normal, normals);
            }

            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, mesh.Indices.Count));
            return mesh;
        }

        #region Algorithm

        private void AddPointToFace(HullVertex vtx, HullFace face)
        {
            vtx.Face = face;

            if (face.Outside == null)
            {
                _claimed.Add(vtx);
            }
            else
            {
                _claimed.InsertBefore(vtx, face.Outside);
            }
            face.Outside = vtx;
        }

        private void RemovePointFromFace(HullVertex vtx, HullFace face)
        {
            if (vtx == face.Outside)
            {
                if (vtx.Next != null && vtx.Next.Face == face)
                {
                    face.Outside = vtx.Next;
                }
                else
                {
                    face.Outside = null;
                }
            }
            _claimed.Remove(vtx);
        }

        private HullVertex? RemoveAllPointsFromFace(HullFace face)
        {
            if (face.Outside != null)
            {
                var end = face.Outside;
                while (end.Next != null && end.Next.Face == face)
                {
                    end = end.Next;
                }
                _claimed.Remove(face.Outside, end);
                end.Next = null;
                return face.Outside;
            }
            return null;
        }

        private void ComputeMaxAndMin()
        {
            var max = new HullPoint();
            var min = new HullPoint();

            for (int i = 0; i < 3; i++)
            {
                _maxVtxs[i] = _minVtxs[i] = _points[0];
            }
            max.Set(_points[0].Point);
            min.Set(_points[0].Point);

            for (int i = 1; i < _points.Length; i++)
            {
                var pnt = _points[i].Point;
                if (pnt.X > max.X)
                {
                    max.X = pnt.X;
                    _maxVtxs[0] = _points[i];
                }
                else if (pnt.X < min.X)
                {
                    min.X = pnt.X;
                    _minVtxs[0] = _points[i];
                }
                if (pnt.Y > max.Y)
                {
                    max.Y = pnt.Y;
                    _maxVtxs[1] = _points[i];
                }
                else if (pnt.Y < min.Y)
                {
                    min.Y = pnt.Y;
                    _minVtxs[1] = _points[i];
                }
                if (pnt.Z > max.Z)
                {
                    max.Z = pnt.Z;
                    _maxVtxs[2] = _points[i];
                }
                else if (pnt.Z < min.Z)
                {
                    min.Z = pnt.Z;
                    _minVtxs[2] = _points[i];
                }
            }

            // this epsilon formula comes from QuickHull
            _charLength = Math.Max(max.X - min.X, max.Y - min.Y);
            _charLength = Math.Max(max.Z - min.Z, _charLength);
            if (ExplicitDistanceTolerance == AutomaticTolerance)
            {
                DistanceTolerance =
                    3 * DoublePrecision * (Math.Max(Math.Abs(max.X), Math.Abs(min.X)) +
                                           Math.Max(Math.Abs(max.Y), Math.Abs(min.Y)) +
                                           Math.Max(Math.Abs(max.Z), Math.Abs(min.Z)));
            }
            else
            {
                DistanceTolerance = ExplicitDistanceTolerance;
            }
        }

        private void CreateInitialSimplex()
        {
            double max = 0;
            int imax = 0;

            for (int i = 0; i < 3; i++)
            {
                double diff = _maxVtxs[i].Point[i] - _minVtxs[i].Point[i];
                if (diff > max)
                {
                    max = diff;
                    imax = i;
                }
            }

            if (max <= DistanceTolerance)
            {
                throw new ArgumentException("Input points appear to be coincident");
            }
            var vtx = new HullVertex[4];

            // set first two vertices to be those with the greatest one dimensional separation
            vtx[0] = _maxVtxs[imax];
            vtx[1] = _minVtxs[imax];

            // set third vertex to be the vertex farthest from the line between vtx0 and vtx1
            var u01 = new HullPoint();
            var diff02 = new HullPoint();
            var nrml = new HullPoint();
            var xprod = new HullPoint();
            double maxSqr = 0;
            u01.Sub(vtx[1].Point, vtx[0].Point);
            u01.Normalize();
            HullVertex? vtx2 = null;
            for (int i = 0; i < _points.Length; i++)
            {
                diff02.Sub(_points[i].Point, vtx[0].Point);
                xprod.Cross(u01, diff02);
                double lenSqr = xprod.NormSquared;
                if (lenSqr > maxSqr &&
                    _points[i] != vtx[0] &&  // paranoid
                    _points[i] != vtx[1])
                {
                    maxSqr = lenSqr;
                    vtx2 = _points[i];
                    nrml.Set(xprod);
                }
            }
            if (vtx2 == null || Math.Sqrt(maxSqr) <= 100 * DistanceTolerance)
            {
                throw new ArgumentException("Input points appear to be colinear");
            }
            vtx[2] = vtx2;
            nrml.Normalize();

            double maxDist = 0;
            double d0 = vtx[2].Point.Dot(nrml);
            HullVertex? vtx3 = null;
            for (int i = 0; i < _points.Length; i++)
            {
                double dist = Math.Abs(_points[i].Point.Dot(nrml) - d0);
                if (dist > maxDist &&
                    _points[i] != vtx[0] &&  // paranoid
                    _points[i] != vtx[1] &&
                    _points[i] != vtx[2])
                {
                    maxDist = dist;
                    vtx3 = _points[i];
                }
            }
            if (vtx3 == null || Math.Abs(maxDist) <= 100 * DistanceTolerance)
            {
                throw new ArgumentException("Input points appear to be coplanar");
            }
            vtx[3] = vtx3;

            var tris = new HullFace[4];

            if (vtx[3].Point.Dot(nrml) - d0 < 0)
            {
                tris[0] = HullFace.CreateTriangle(vtx[0], vtx[1], vtx[2]);
                tris[1] = HullFace.CreateTriangle(vtx[3], vtx[1], vtx[0]);
                tris[2] = HullFace.CreateTriangle(vtx[3], vtx[2], vtx[1]);
                tris[3] = HullFace.CreateTriangle(vtx[3], vtx[0], vtx[2]);

                for (int i = 0; i < 3; i++)
                {
                    int k = (i + 1) % 3;
                    tris[i + 1].GetEdge(1).SetMutualOpposites(tris[k + 1].GetEdge(0));
                    tris[i + 1].GetEdge(2).SetMutualOpposites(tris[0].GetEdge(k));
                }
            }
            else
            {
                tris[0] = HullFace.CreateTriangle(vtx[0], vtx[2], vtx[1]);
                tris[1] = HullFace.CreateTriangle(vtx[3], vtx[0], vtx[1]);
                tris[2] = HullFace.CreateTriangle(vtx[3], vtx[1], vtx[2]);
                tris[3] = HullFace.CreateTriangle(vtx[3], vtx[2], vtx[0]);

                for (int i = 0; i < 3; i++)
                {
                    int k = (i + 1) % 3;
                    tris[i + 1].GetEdge(0).SetMutualOpposites(tris[k + 1].GetEdge(1));
                    tris[i + 1].GetEdge(2).SetMutualOpposites(tris[0].GetEdge((3 - i) % 3));
                }
            }

            for (int i = 0; i < 4; i++)
            {
                _faces.Add(tris[i]);
            }

            for (int i = 0; i < _points.Length; i++)
            {
                var v = _points[i];

                if (v == vtx[0] || v == vtx[1] || v == vtx[2] || v == vtx[3])
                {
                    continue;
                }

                maxDist = DistanceTolerance;
                HullFace? maxFace = null;
                for (int k = 0; k < 4; k++)
                {
                    double dist = tris[k].DistanceToPlane(v.Point);
                    if (dist > maxDist)
                    {
                        maxFace = tris[k];
                        maxDist = dist;
                    }
                }
                if (maxFace != null)
                {
                    AddPointToFace(v, maxFace);
                }
            }
        }

        private void ResolveUnclaimedPoints(HullFaceList newFaces)
        {
            var vtxNext = _unclaimed.First;
            for (var vtx = vtxNext; vtx != null; vtx = vtxNext)
            {
                vtxNext = vtx.Next;

                double maxDist = DistanceTolerance;
                HullFace? maxFace = null;
                for (var newFace = newFaces.First; newFace != null; newFace = newFace.Next)
                {
                    if (newFace.Mark == HullFace.Visible)
                    {
                        double dist = newFace.DistanceToPlane(vtx.Point);
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                            maxFace = newFace;
                        }
                        if (maxDist > 1000 * DistanceTolerance)
                        {
                            break;
                        }
                    }
                }
                if (maxFace != null)
                {
                    AddPointToFace(vtx, maxFace);
                }
            }
        }

        private void DeleteFacePoints(HullFace face, HullFace? absorbingFace)
        {
            var faceVtxs = RemoveAllPointsFromFace(face);
            if (faceVtxs != null)
            {
                if (absorbingFace == null)
                {
                    _unclaimed.AddRange(faceVtxs);
                }
                else
                {
                    var vtxNext = faceVtxs;
                    for (var vtx = vtxNext; vtx != null; vtx = vtxNext)
                    {
                        vtxNext = vtx.Next;
                        double dist = absorbingFace.DistanceToPlane(vtx.Point);
                        if (dist > DistanceTolerance)
                        {
                            AddPointToFace(vtx, absorbingFace);
                        }
                        else
                        {
                            _unclaimed.Add(vtx);
                        }
                    }
                }
            }
        }

        private static double OppFaceDistance(HullHalfEdge he)
            => he.Face.DistanceToPlane(he.Opposite.Face.Centroid);

        private bool MergeAdjacent(HullFace face, int mergeType)
        {
            var hedge = face.He0;

            bool convex = true;
            do
            {
                var oppFace = hedge.OppositeFace;
                bool merge = false;

                if (mergeType == NonConvexMerge)
                {
                    // merge faces if they are definitively non-convex
                    if (OppFaceDistance(hedge) > -DistanceTolerance ||
                        OppFaceDistance(hedge.Opposite) > -DistanceTolerance)
                    {
                        merge = true;
                    }
                }
                else
                {
                    // merge faces if they are parallel or non-convex wrt to the larger face;
                    // otherwise, just mark the face non-convex for the second pass.
                    if (face.Area > oppFace.Area)
                    {
                        if (OppFaceDistance(hedge) > -DistanceTolerance)
                        {
                            merge = true;
                        }
                        else if (OppFaceDistance(hedge.Opposite) > -DistanceTolerance)
                        {
                            convex = false;
                        }
                    }
                    else
                    {
                        if (OppFaceDistance(hedge.Opposite) > -DistanceTolerance)
                        {
                            merge = true;
                        }
                        else if (OppFaceDistance(hedge) > -DistanceTolerance)
                        {
                            convex = false;
                        }
                    }
                }

                if (merge)
                {
                    int numd = face.MergeAdjacentFace(hedge, _discardedFaces);
                    for (int i = 0; i < numd; i++)
                    {
                        DeleteFacePoints(_discardedFaces[i], face);
                    }
                    return true;
                }
                hedge = hedge.Next;
            }
            while (hedge != face.He0);
            if (!convex)
            {
                face.Mark = HullFace.NonConvex;
            }
            return false;
        }

        private void CalculateHorizon(HullPoint eyePnt, HullHalfEdge? edge0, HullFace face, List<HullHalfEdge> horizon)
        {
            DeleteFacePoints(face, null);
            face.Mark = HullFace.Deleted;
            HullHalfEdge edge;
            if (edge0 == null)
            {
                edge0 = face.GetEdge(0);
                edge = edge0;
            }
            else
            {
                edge = edge0.Next;
            }
            do
            {
                var oppFace = edge.OppositeFace;
                if (oppFace.Mark == HullFace.Visible)
                {
                    if (oppFace.DistanceToPlane(eyePnt) > DistanceTolerance)
                    {
                        CalculateHorizon(eyePnt, edge.Opposite, oppFace, horizon);
                    }
                    else
                    {
                        horizon.Add(edge);
                    }
                }
                edge = edge.Next;
            }
            while (edge != edge0);
        }

        private HullHalfEdge AddAdjoiningFace(HullVertex eyeVtx, HullHalfEdge he)
        {
            var face = HullFace.CreateTriangle(eyeVtx, he.Tail, he.Head);
            _faces.Add(face);
            face.GetEdge(-1).SetMutualOpposites(he.Opposite);
            return face.GetEdge(0);
        }

        private void AddNewFaces(HullFaceList newFaces, HullVertex eyeVtx, List<HullHalfEdge> horizon)
        {
            newFaces.Clear();

            HullHalfEdge? hedgeSidePrev = null;
            HullHalfEdge? hedgeSideBegin = null;

            foreach (var horizonHe in horizon)
            {
                var hedgeSide = AddAdjoiningFace(eyeVtx, horizonHe);
                if (hedgeSidePrev != null)
                {
                    hedgeSide.Next.SetMutualOpposites(hedgeSidePrev);
                }
                else
                {
                    hedgeSideBegin = hedgeSide;
                }
                newFaces.Add(hedgeSide.Face);
                hedgeSidePrev = hedgeSide;
            }
            hedgeSideBegin!.Next.SetMutualOpposites(hedgeSidePrev!);
        }

        private HullVertex? NextPointToAdd()
        {
            if (_claimed.IsEmpty)
            {
                return null;
            }
            var eyeFace = _claimed.First!.Face!;
            HullVertex? eyeVtx = null;
            double maxDist = 0;
            for (var vtx = eyeFace.Outside; vtx != null && vtx.Face == eyeFace; vtx = vtx.Next)
            {
                double dist = eyeFace.DistanceToPlane(vtx.Point);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    eyeVtx = vtx;
                }
            }
            return eyeVtx;
        }

        private void AddPointToHull(HullVertex eyeVtx)
        {
            _horizon.Clear();
            _unclaimed.Clear();

            RemovePointFromFace(eyeVtx, eyeVtx.Face!);
            CalculateHorizon(eyeVtx.Point, null, eyeVtx.Face!, _horizon);
            _newFaces.Clear();
            AddNewFaces(_newFaces, eyeVtx, _horizon);

            // first merge pass ... merge faces which are non-convex as determined by the larger face
            for (var face = _newFaces.First; face != null; face = face.Next)
            {
                if (face.Mark == HullFace.Visible)
                {
                    while (MergeAdjacent(face, NonConvexWrtLargerFace))
                    {
                    }
                }
            }
            // second merge pass ... merge faces which are non-convex wrt either face
            for (var face = _newFaces.First; face != null; face = face.Next)
            {
                if (face.Mark == HullFace.NonConvex)
                {
                    face.Mark = HullFace.Visible;
                    while (MergeAdjacent(face, NonConvexMerge))
                    {
                    }
                }
            }
            ResolveUnclaimedPoints(_newFaces);
        }

        private void BuildHull()
        {
            ComputeMaxAndMin();
            CreateInitialSimplex();
            HullVertex? eyeVtx;
            while ((eyeVtx = NextPointToAdd()) != null)
            {
                AddPointToHull(eyeVtx);
            }
            ReindexFacesAndVertices();
        }

        private static void MarkFaceVertices(HullFace face, int mark)
        {
            var he0 = face.He0;
            var he = he0;
            do
            {
                he.Head.Index = mark;
                he = he.Next;
            }
            while (he != he0);
        }

        private void ReindexFacesAndVertices()
        {
            foreach (var point in _points)
            {
                point.Index = -1;
            }

            // remove inactive faces and mark active vertices
            _faces.RemoveAll(face => face.Mark != HullFace.Visible);
            foreach (var face in _faces)
            {
                MarkFaceVertices(face, 0);
            }

            // reindex vertices
            _numVertices = 0;
            for (int i = 0; i < _points.Length; i++)
            {
                var vtx = _points[i];
                if (vtx.Index == 0)
                {
                    _vertexPointIndices[_numVertices] = i;
                    vtx.Index = _numVertices++;
                }
            }
        }

        private static bool CheckFaceConvexity(HullFace face, double tol)
        {
            var he = face.He0;
            do
            {
                face.CheckConsistency();
                // make sure edge is convex
                if (OppFaceDistance(he) > tol)
                {
                    return false;
                }
                if (OppFaceDistance(he.Opposite) > tol)
                {
                    return false;
                }
                if (he.Next.OppositeFace == he.OppositeFace)
                {
                    // redundant vertex
                    return false;
                }
                he = he.Next;
            }
            while (he != face.He0);
            return true;
        }

        #endregion
    }
}
