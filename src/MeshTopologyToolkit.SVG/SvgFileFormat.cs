using Svg;
using Svg.Pathing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Xml.Linq;

namespace MeshTopologyToolkit.SVG
{
    public class SvgFileFormat : IFileFormat
    {
        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Scalable Vector Graphics (SVG)", ".svg"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            var container = new FileContainer();
            content = container;

            var scene = new Scene();
            content.Scenes.Add(scene);

            using var stream = entry.OpenRead();
            var doc = SvgDocument.Open<SvgDocument>(stream, new SvgOptions());

            void AddMesh(IMesh? mesh)
            {
                if (mesh == null)
                    return;
                var node = new Node() { Mesh = new MeshReference(mesh) };
                container.Meshes.Add(mesh);
                scene.AddChild(node);
            }

            foreach (var el in doc.Descendants())
            {
                switch (el)
                {
                    case NonSvgElement nonSvg:
                        break;
                    case SvgDefinitionList svgDefinitionList:
                        break;
                    case SvgGroup svgGroup:
                        break;
                    case SvgPath svgPath:
                        AddMesh(ReadPath(svgPath));
                        break;
                    case SvgLine line:
                        AddMesh(ReadLine(line));
                        break;
                    case SvgRectangle svgRectangle:
                        break;
                    case SvgEllipse svgEllipse:
                        break;
                    case SvgText svgText:
                        break;
                    case SvgTextSpan svgTextSpan:
                        break;
                    default:
                        throw new NotImplementedException($"Element {el.GetType().Name} not supported yet.");
                }
            }

            return true;
        }

        // Helper to transform a point by an element's transform (if any)
        Vector3 TransformPoint(SvgElement el, PointF p)
        {
            var transforms = el.Transforms;
            if (transforms != null && transforms.Count > 0)
            {
                using (var m = transforms.GetMatrix()) // returns System.Drawing.Drawing2D.Matrix
                {
                    var pts = new[] { p };
                    m.TransformPoints(pts);
                    return new Vector3(pts[0].X, pts[0].Y, 0.0f);
                }
            }
            return new Vector3(p.X, p.Y, 0.0f);
        }

        private IMesh ReadLine(SvgLine line)
        {
            var mesh = new UnifiedIndexedMesh() { Name = line.ID };
            IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
            var indices = new List<int>
            {
                positions.Add(TransformPoint(line, new PointF(line.StartX, line.StartY))),
                positions.Add(TransformPoint(line, new PointF(line.EndX, line.EndY)))
            };
            mesh.AddAttribute(MeshAttributeKey.Position, positions);
            mesh.AddIndices(indices);
            mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.LineStrip, 0, indices.Count));

            return mesh;
        }

        private IMesh? ReadPath(SvgPath path)
        {
            var mesh = new UnifiedIndexedMesh() { Name = path.ID };
            IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
            var indices = new List<int>();

            // iterate path segments and flatten them
            var segs = path.PathData;
            if (segs == null)
                return null;
            PointF cur = PointF.Empty;
            bool curIsSet = false;

            foreach (var seg in segs)
            {
                switch (seg)
                {
                    case SvgMoveToSegment m:
                        cur = m.End;
                        curIsSet = true;
                        break;

                    case SvgLineSegment ls:
                        {
                            if (!curIsSet)
                                throw new FormatException();
                            var a = cur;
                            var b = ls.End;
                            indices.Add(positions.Add(TransformPoint(path, a)));
                            indices.Add(positions.Add(TransformPoint(path, b)));
                            cur = b;
                            curIsSet = true;
                        }
                        break;

                    case SvgCubicCurveSegment cs:
                        {
                            if (!curIsSet)
                                throw new FormatException();
                            // cs.Start, cs.FirstControlPoint, cs.SecondControlPoint, cs.End
                            var p0 = TransformPoint(path, cur);
                            var c1 = TransformPoint(path, cs.FirstControlPoint);
                            var c2 = TransformPoint(path, cs.SecondControlPoint);
                            var p3 = TransformPoint(path, cs.End);
                            FlattenCubic(positions, indices, p0, c1, c2, p3);
                            cur = cs.End;
                            curIsSet = true;
                        }
                        break;

                    case SvgQuadraticCurveSegment qs:
                        {
                            if (!curIsSet)
                                throw new FormatException();
                            var p0 = cur;
                            var cp = qs.ControlPoint;
                            var p1 = qs.End;
                            //FlattenQuadratic(path, p0, cp, p1);
                            cur = p1;
                            curIsSet = true;
                        }
                        break;

                    case SvgArcSegment asg:
                        {
                            if (!curIsSet)
                                throw new FormatException();
                            var start = cur;
                            //FlattenArc(path, asg, start);
                            cur = asg.End;
                            curIsSet = true;
                        }
                        break;

                    case SvgClosePathSegment _:
                        // optionally connect cur to subpath start — handled by segments if present
                        break;

                    default:
                        // Other segments (if any) can be handled similarly.
                        break;
                }
            }

            mesh.AddAttribute(MeshAttributeKey.Position, positions);
            mesh.AddIndices(indices);
            mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.LineList, 0, indices.Count));

            return mesh;
        }

        private Vector3 Mid(Vector3 a, Vector3 b)
        {
            return (a + b) * 0.5f;
        }

        private void FlattenCubic(IMeshVertexAttribute<Vector3> positions, IList<int> indices, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var tol = 0.1f;

            // Recursively subdivide until control points are close to chord
            void Recurse(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                // Flatness: max distance of control points b,c to chord a-d
                double d1 = DistancePointToLine(b, a, d);
                double d2 = DistancePointToLine(c, a, d);
                if (Math.Max(d1, d2) <= tol)
                {
                    indices.Add(positions.Add(a));
                    indices.Add(positions.Add(d));
                    return;
                }
                // subdivide using de Casteljau
                // ab, bc, cd
                var ab = Mid(a, b);
                var bc = Mid(b, c);
                var cd = Mid(c, d);
                var abc = Mid(ab, bc);
                var bcd = Mid(bc, cd);
                var abcd = Mid(abc, bcd);
                Recurse(a, ab, abc, abcd);
                Recurse(abcd, bcd, cd, d);
            }
            Recurse(p0, p1, p2, p3);
        }

        private static float DistancePointToLine(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 u = b - a;
            Vector3 v = p - a;
            float len2 = u.LengthSquared();
            if (len2 == 0) return v.Length();
            var t = Vector3.Dot(u,v) / len2;
            var c = a + u * t;
            var d = p - c;
            return d.Length();
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            return false;
        }
    }
}
