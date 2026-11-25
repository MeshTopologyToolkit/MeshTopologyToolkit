using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Fbx
{
    internal sealed class FbxMeshData
    {
        public FbxMeshData(SeparatedIndexedMesh mesh, int controlPointCount, List<int> cornerControlPoints)
        {
            Mesh = mesh;
            ControlPointCount = controlPointCount;
            CornerControlPoints = cornerControlPoints;
        }

        public SeparatedIndexedMesh Mesh { get; }

        public int ControlPointCount { get; }

        /// <summary>Control point (position) index of every corner of the triangulated mesh. Also used as the position index list of the mesh.</summary>
        public List<int> CornerControlPoints { get; }
    }

    /// <summary>
    /// Converts the geometry nodes of an FBX file (a <c>Geometry</c> object for 7.x, or a <c>Model</c> for legacy files)
    /// to <see cref="SeparatedIndexedMesh"/>. Polygons are triangulated and the triangles are grouped by material slot.
    /// </summary>
    internal static class FbxMeshReader
    {
        private enum Mapping { None, ByPolygonVertex, ByControlPoint, ByPolygon, AllSame, ByEdge }

        private sealed class Layer
        {
            public Mapping Mapping;
            public double[] Data = Array.Empty<double>();
            public int[]? Indices;
            public int Count;
            public int[] ResolveCorners(IReadOnlyList<int> cornerPolygonVertex, IReadOnlyList<int> cornerControlPoint, IReadOnlyList<int> cornerPolygon)
            {
                var result = new int[cornerPolygonVertex.Count];
                for (int i = 0; i < result.Length; ++i)
                {
                    int raw;
                    switch (Mapping)
                    {
                        case Mapping.ByPolygonVertex: raw = cornerPolygonVertex[i]; break;
                        case Mapping.ByControlPoint: raw = cornerControlPoint[i]; break;
                        case Mapping.ByPolygon: raw = cornerPolygon[i]; break;
                        default: raw = 0; break;
                    }
                    int idx = Indices != null ? (raw >= 0 && raw < Indices.Length ? Indices[raw] : 0) : raw;
                    if (idx < 0 || idx >= Count)
                        idx = 0;
                    result[i] = idx;
                }
                return result;
            }
        }

        private static Mapping ParseMapping(string? s)
        {
            switch (s)
            {
                case "ByPolygonVertex": return Mapping.ByPolygonVertex;
                case "ByVertice":
                case "ByVertex":
                case "ByControlPoint": return Mapping.ByControlPoint;
                case "ByPolygon": return Mapping.ByPolygon;
                case "AllSame": return Mapping.AllSame;
                case "ByEdge": return Mapping.ByEdge;
                default: return Mapping.None;
            }
        }

        private static Layer? ReadLayer(FbxNode element, string dataName, string indexName, int stride)
        {
            var mapping = ParseMapping(element.Child("MappingInformationType")?.GetString(0));
            if (mapping == Mapping.None || mapping == Mapping.ByEdge)
                return null;
            var dataNode = element.Child(dataName);
            if (dataNode == null)
                return null;
            var layer = new Layer { Mapping = mapping, Data = dataNode.GetDoubles() };
            layer.Count = layer.Data.Length / stride;
            var reference = element.Child("ReferenceInformationType")?.GetString(0);
            var indexNode = element.Child(indexName);
            if (indexNode != null && (reference == "IndexToDirect" || reference == "Index"))
                layer.Indices = indexNode.GetInts();
            return layer;
        }

        private static List<FbxNode> LayerElements(FbxNode geometry, string name)
        {
            var list = new List<FbxNode>(geometry.ChildrenNamed(name));
            // Sort by the layer element index (the first property) so that UV set 0 is the first one.
            list.Sort((a, b) => a.GetInt(0).CompareTo(b.GetInt(0)));
            return list;
        }

        public static FbxMeshData? Read(FbxNode geometry, string? name)
        {
            var vertexValues = geometry.Child("Vertices")?.GetDoubles();
            var polygonVertexIndex = geometry.Child("PolygonVertexIndex")?.GetInts();
            if (vertexValues == null || polygonVertexIndex == null || vertexValues.Length < 3)
                return null;

            int controlPointCount = vertexValues.Length / 3;
            var positions = new ListMeshVertexAttribute<Vector3>(controlPointCount);
            for (int i = 0; i < controlPointCount; ++i)
                positions.Add(new Vector3((float)vertexValues[i * 3], (float)vertexValues[i * 3 + 1], (float)vertexValues[i * 3 + 2]));

            // Triangulate. Every triangle corner remembers its polygon vertex (index in PolygonVertexIndex), control point and polygon.
            var triPolygonVertex = new List<int>();
            var triPolygon = new List<int>();
            int polygonIndex = 0;
            int start = 0;
            for (int i = 0; i < polygonVertexIndex.Length; ++i)
            {
                if (polygonVertexIndex[i] >= 0)
                    continue;
                int count = i - start + 1;
                if (count >= 3)
                {
                    var local = Triangulate(polygonVertexIndex, start, count, positions);
                    foreach (var l in local)
                    {
                        triPolygonVertex.Add(start + l);
                        if ((triPolygonVertex.Count % 3) == 0)
                            triPolygon.Add(polygonIndex);
                    }
                }
                ++polygonIndex;
                start = i + 1;
            }

            int triangleCount = triPolygon.Count;

            // Group the triangles by material.
            var materialLayerNode = geometry.Child("LayerElementMaterial");
            Layer? materialLayer = materialLayerNode != null ? ReadLayer(materialLayerNode, "Materials", "Materials", 1) : null;
            var triangleMaterial = new int[triangleCount];
            if (materialLayer != null)
            {
                for (int t = 0; t < triangleCount; ++t)
                {
                    int raw = materialLayer.Mapping == Mapping.ByPolygon ? triPolygon[t] : 0;
                    triangleMaterial[t] = raw < materialLayer.Data.Length ? (int)materialLayer.Data[raw] : 0;
                }
            }
            var order = new List<int>(triangleCount);
            var materials = new SortedDictionary<int, List<int>>();
            for (int t = 0; t < triangleCount; ++t)
            {
                int m = triangleMaterial[t];
                if (!materials.TryGetValue(m, out var list))
                    materials[m] = list = new List<int>();
                list.Add(t);
            }

            var mesh = new SeparatedIndexedMesh(name);
            var cornerPolygonVertex = new List<int>(triangleCount * 3);
            var cornerControlPoint = new List<int>(triangleCount * 3);
            var cornerPolygon = new List<int>(triangleCount * 3);
            foreach (var kv in materials)
            {
                int first = cornerPolygonVertex.Count;
                foreach (var t in kv.Value)
                {
                    for (int c = 0; c < 3; ++c)
                    {
                        int pv = triPolygonVertex[t * 3 + c];
                        int cp = polygonVertexIndex[pv];
                        if (cp < 0)
                            cp = ~cp;
                        if (cp >= controlPointCount)
                            cp = 0;
                        cornerPolygonVertex.Add(pv);
                        cornerControlPoint.Add(cp);
                        cornerPolygon.Add(triPolygon[t]);
                    }
                }
                mesh.DrawCalls.Add(new MeshDrawCall(0, kv.Key, MeshTopology.TriangleList, first, cornerPolygonVertex.Count - first));
            }

            mesh.SetAttribute(MeshAttributeKey.Position, positions, cornerControlPoint);

            // Normals.
            Layer? normalLayer = null;
            foreach (var e in LayerElements(geometry, "LayerElementNormal"))
            {
                normalLayer = ReadLayer(e, "Normals", "NormalsIndex", 3);
                if (normalLayer != null)
                    break;
            }
            int[]? normalCorners = null;
            ListMeshVertexAttribute<Vector3>? normals = null;
            if (normalLayer != null)
            {
                normals = ToVector3List(normalLayer);
                normalCorners = normalLayer.ResolveCorners(cornerPolygonVertex, cornerControlPoint, cornerPolygon);
                mesh.SetAttribute(MeshAttributeKey.Normal, normals, normalCorners);
            }

            // Tangents. The sign of W is taken from the binormal when it is available, because exporters write 1 there.
            foreach (var e in LayerElements(geometry, "LayerElementTangent"))
            {
                var tangentLayer = ReadLayer(e, "Tangents", "TangentsIndex", 3);
                if (tangentLayer == null)
                    continue;
                var tangentCorners = tangentLayer.ResolveCorners(cornerPolygonVertex, cornerControlPoint, cornerPolygon);
                var tangents3 = ToVector3List(tangentLayer);

                Layer? binormalLayer = null;
                foreach (var b in LayerElements(geometry, "LayerElementBinormal"))
                {
                    binormalLayer = ReadLayer(b, "Binormals", "BinormalsIndex", 3);
                    if (binormalLayer != null)
                        break;
                }
                double[]? wData = null;
                var wNode = e.Child("TangentsW");
                if (wNode != null)
                    wData = wNode.GetDoubles();

                if (binormalLayer != null && normals != null && normalCorners != null)
                {
                    var binormalCorners = binormalLayer.ResolveCorners(cornerPolygonVertex, cornerControlPoint, cornerPolygon);
                    var binormals = ToVector3List(binormalLayer);
                    var tangents = new ListMeshVertexAttribute<Vector4>(tangentCorners.Length);
                    var identity = new List<int>(tangentCorners.Length);
                    for (int k = 0; k < tangentCorners.Length; ++k)
                    {
                        var n = normals[normalCorners[k]];
                        var t3 = tangents3[tangentCorners[k]];
                        var b3 = binormals[binormalCorners[k]];
                        float w = Vector3.Dot(Vector3.Cross(n, t3), b3) < 0.0f ? -1.0f : 1.0f;
                        tangents.Add(new Vector4(t3, w));
                        identity.Add(k);
                    }
                    mesh.SetAttribute(MeshAttributeKey.Tangent, tangents, identity);
                }
                else
                {
                    var tangents = new ListMeshVertexAttribute<Vector4>(tangents3.Count);
                    for (int k = 0; k < tangents3.Count; ++k)
                    {
                        float w = wData != null && k < wData.Length && wData[k] < 0.0 ? -1.0f : 1.0f;
                        tangents.Add(new Vector4(tangents3[k], w));
                    }
                    mesh.SetAttribute(MeshAttributeKey.Tangent, tangents, tangentCorners);
                }
                break;
            }

            // UV sets and colors.
            int uvSet = 0;
            foreach (var e in LayerElements(geometry, "LayerElementUV"))
            {
                var layer = ReadLayer(e, "UV", "UVIndex", 2);
                if (layer == null)
                    continue;
                var uvs = new ListMeshVertexAttribute<Vector2>(layer.Count);
                for (int k = 0; k < layer.Count; ++k)
                    uvs.Add(new Vector2((float)layer.Data[k * 2], (float)layer.Data[k * 2 + 1]));
                mesh.SetAttribute(new MeshAttributeKey(MeshAttributeNames.TexCoord, uvSet++), uvs, layer.ResolveCorners(cornerPolygonVertex, cornerControlPoint, cornerPolygon));
            }

            int colorSet = 0;
            foreach (var e in LayerElements(geometry, "LayerElementColor"))
            {
                var layer = ReadLayer(e, "Colors", "ColorIndex", 4);
                if (layer == null)
                    continue;
                var colors = new ListMeshVertexAttribute<Vector4>(layer.Count);
                for (int k = 0; k < layer.Count; ++k)
                    colors.Add(new Vector4((float)layer.Data[k * 4], (float)layer.Data[k * 4 + 1], (float)layer.Data[k * 4 + 2], (float)layer.Data[k * 4 + 3]));
                mesh.SetAttribute(new MeshAttributeKey(MeshAttributeNames.Color, colorSet++), colors, layer.ResolveCorners(cornerPolygonVertex, cornerControlPoint, cornerPolygon));
            }

            return new FbxMeshData(mesh, controlPointCount, cornerControlPoint);
        }

        private static ListMeshVertexAttribute<Vector3> ToVector3List(Layer layer)
        {
            var list = new ListMeshVertexAttribute<Vector3>(layer.Count);
            for (int k = 0; k < layer.Count; ++k)
                list.Add(new Vector3((float)layer.Data[k * 3], (float)layer.Data[k * 3 + 1], (float)layer.Data[k * 3 + 2]));
            return list;
        }

        /// <summary>Triangulates a polygon, returns offsets (0 .. count-1) of the corners relative to <paramref name="start"/>.</summary>
        private static List<int> Triangulate(int[] polygonVertexIndex, int start, int count, IReadOnlyList<Vector3> positions)
        {
            var result = new List<int>((count - 2) * 3);
            if (count <= 4)
            {
                for (int k = 1; k + 1 < count; ++k)
                {
                    result.Add(0);
                    result.Add(k);
                    result.Add(k + 1);
                }
                return result;
            }

            var pts = new Vector3[count];
            for (int k = 0; k < count; ++k)
            {
                int cp = polygonVertexIndex[start + k];
                if (cp < 0)
                    cp = ~cp;
                pts[k] = cp < positions.Count ? positions[cp] : Vector3.Zero;
            }

            // Newell's method gives the polygon normal, which selects the projection plane and the winding.
            var normal = Vector3.Zero;
            for (int k = 0; k < count; ++k)
            {
                var a = pts[k];
                var b = pts[(k + 1) % count];
                normal.X += (a.Y - b.Y) * (a.Z + b.Z);
                normal.Y += (a.Z - b.Z) * (a.X + b.X);
                normal.Z += (a.X - b.X) * (a.Y + b.Y);
            }

            var remaining = new List<int>(count);
            for (int k = 0; k < count; ++k)
                remaining.Add(k);

            int guard = count * count;
            int cursor = 0;
            while (remaining.Count > 3 && guard-- > 0)
            {
                int n = remaining.Count;
                int ia = remaining[(cursor + n - 1) % n];
                int ib = remaining[cursor % n];
                int ic = remaining[(cursor + 1) % n];
                if (IsEar(pts, remaining, ia, ib, ic, normal))
                {
                    result.Add(ia);
                    result.Add(ib);
                    result.Add(ic);
                    remaining.RemoveAt(cursor % n);
                    if (cursor >= remaining.Count)
                        cursor = 0;
                }
                else
                {
                    cursor = (cursor + 1) % n;
                }
            }

            if (remaining.Count > 3)
            {
                // Degenerate polygon, fall back to a fan over what is left.
                for (int k = 1; k + 1 < remaining.Count; ++k)
                {
                    result.Add(remaining[0]);
                    result.Add(remaining[k]);
                    result.Add(remaining[k + 1]);
                }
            }
            else if (remaining.Count == 3)
            {
                result.Add(remaining[0]);
                result.Add(remaining[1]);
                result.Add(remaining[2]);
            }
            return result;
        }

        private static bool IsEar(Vector3[] pts, List<int> polygon, int ia, int ib, int ic, Vector3 normal)
        {
            var a = pts[ia];
            var b = pts[ib];
            var c = pts[ic];
            // The corner has to be convex with respect to the polygon normal.
            if (Vector3.Dot(Vector3.Cross(b - a, c - b), normal) <= 0.0f)
                return false;
            foreach (var i in polygon)
            {
                if (i == ia || i == ib || i == ic)
                    continue;
                var p = pts[i];
                if (p == a || p == b || p == c)
                    continue;
                if (PointInTriangle(p, a, b, c, normal))
                    return false;
            }
            return true;
        }

        private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            return Vector3.Dot(Vector3.Cross(b - a, p - a), normal) >= 0.0f
                && Vector3.Dot(Vector3.Cross(c - b, p - b), normal) >= 0.0f
                && Vector3.Dot(Vector3.Cross(a - c, p - c), normal) >= 0.0f;
        }
    }
}
