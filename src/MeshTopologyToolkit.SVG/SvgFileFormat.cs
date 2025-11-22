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
                        AddMesh(ReadRectangle(svgRectangle));
                        break;
                    case SvgEllipse svgEllipse:
                        AddMesh(ReadEllipse(svgEllipse));
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

        private IMesh? ReadRectangle(SvgRectangle rect)
        {
            // Resolve rectangle position & size to device/absolute values (you can change this if you want local units)
            float x = rect.X.ToDeviceValue(null, UnitRenderingType.Horizontal, rect);
            float y = rect.Y.ToDeviceValue(null, UnitRenderingType.Vertical, rect);
            float w = rect.Width.ToDeviceValue(null, UnitRenderingType.Horizontal, rect);
            float h = rect.Height.ToDeviceValue(null, UnitRenderingType.Vertical, rect);

            // Resolve radii (if not specified they default to 0)
            float rx = rect.CornerRadiusX != null && !rect.CornerRadiusX.IsEmpty ? rect.CornerRadiusX.ToDeviceValue(null, UnitRenderingType.Horizontal, rect) : 0f;
            float ry = rect.CornerRadiusY != null && !rect.CornerRadiusY.IsEmpty ? rect.CornerRadiusY.ToDeviceValue(null, UnitRenderingType.Vertical, rect) : 0f;

            // Clamp radii per SVG spec: rx <= w/2, ry <= h/2. If one radius is zero, corners are sharp.
            if (rx < 0) rx = 0;
            if (ry < 0) ry = 0;
            if (rx > w / 2f) rx = w / 2f;
            if (ry > h / 2f) ry = h / 2f;

            // If radii are zero, we can return a simple rectangle path (move+4 lines+close)
            var segments = new SvgPathSegmentList();

            if (rx == 0f || ry == 0f)
            {
                // simple rectangle
                segments.Add(new SvgMoveToSegment(false, new PointF(x, y)));
                segments.Add(new SvgLineSegment(false, new PointF(x + w, y)));
                segments.Add(new SvgLineSegment(false, new PointF(x + w, y + h)));
                segments.Add(new SvgLineSegment(false, new PointF(x, y + h)));
                segments.Add(new SvgClosePathSegment(false));
            }
            else
            {
                // Helper to build a cubic approximating an elliptical arc from theta0 -> theta1
                // center: (cx,cy), radii rx,ry, returns (start at theta0 is assumed to be already current point)
                static void AddQuarterBezier(SvgPathSegmentList segs, PointF center, float rx, float ry, double theta0, double theta1)
                {
                    // compute k factor for cubic approximation
                    double dt = theta1 - theta0;
                    double k = (4.0 / 3.0) * Math.Tan(dt / 4.0);

                    // start and end points
                    var sx = (float)(center.X + rx * Math.Cos(theta0));
                    var sy = (float)(center.Y + ry * Math.Sin(theta0));
                    var ex = (float)(center.X + rx * Math.Cos(theta1));
                    var ey = (float)(center.Y + ry * Math.Sin(theta1));

                    // derivative vectors at start and end
                    var dx0 = (float)(-rx * Math.Sin(theta0));
                    var dy0 = (float)(ry * Math.Cos(theta0));
                    var dx1 = (float)(-rx * Math.Sin(theta1));
                    var dy1 = (float)(ry * Math.Cos(theta1));

                    // control points:
                    var c1 = new PointF(sx + (float)(k * dx0), sy + (float)(k * dy0));
                    var c2 = new PointF(ex - (float)(k * dx1), ey - (float)(k * dy1));

                    // The MoveTo/LineTo that precedes this call should already have positioned the current point
                    // to (sx,sy). Here we add a cubic segment using absolute coordinates.
                    segs.Add(new SvgCubicCurveSegment(false, c1, c2, new PointF(ex, ey)));
                }

                // Corner centers
                var tlCenter = new PointF(x + rx, y + ry);                     // top-left corner center
                var trCenter = new PointF(x + w - rx, y + ry);                 // top-right
                var brCenter = new PointF(x + w - rx, y + h - ry);             // bottom-right
                var blCenter = new PointF(x + rx, y + h - ry);                 // bottom-left

                // Start point: top edge, after left corner: (x + rx, y)
                var start = new PointF(x + rx, y);
                segments.Add(new SvgMoveToSegment(false, start));

                // Top edge: line to before top-right corner
                segments.Add(new SvgLineSegment(false, new PointF(x + w - rx, y)));

                // Top-right corner: from angle -pi/2 -> 0
                AddQuarterBezier(segments, trCenter, rx, ry, -Math.PI / 2.0, 0.0);

                // Right edge: line down to before bottom-right corner
                segments.Add(new SvgLineSegment(false, new PointF(x + w, y + h - ry)));

                // Bottom-right corner: 0 -> +pi/2
                AddQuarterBezier(segments, brCenter, rx, ry, 0.0, Math.PI / 2.0);

                // Bottom edge: line left to before bottom-left corner
                segments.Add(new SvgLineSegment(false, new PointF(x + rx, y + h)));

                // Bottom-left corner: pi/2 -> pi
                AddQuarterBezier(segments, blCenter, rx, ry, Math.PI / 2.0, Math.PI);

                // Left edge: line up to before top-left corner
                segments.Add(new SvgLineSegment(false, new PointF(x, y + ry)));

                // Top-left corner: pi -> 3pi/2
                AddQuarterBezier(segments, tlCenter, rx, ry, Math.PI, 3.0 * Math.PI / 2.0);

                segments.Add(new SvgClosePathSegment(false));
            }

            return ReadPath(new SvgPath { PathData = segments });
        }

        private IMesh? ReadEllipse(SvgEllipse ellipse)
        {
            // Resolve center and radii to device (absolute) values.
            // If you want purely local coordinates, read ellipse.CenterX/CenterY/RadiusX/RadiusY directly (SvgUnit).
            float cx = ellipse.CenterX.ToDeviceValue(null, UnitRenderingType.Horizontal, ellipse);
            float cy = ellipse.CenterY.ToDeviceValue(null, UnitRenderingType.Vertical, ellipse);
            float rx = ellipse.RadiusX.ToDeviceValue(null, UnitRenderingType.Horizontal, ellipse);
            float ry = ellipse.RadiusY.ToDeviceValue(null, UnitRenderingType.Vertical, ellipse);

            // Standard "kappa" factor for approximating a quarter of a circle by a cubic Bezier:
            // kappa = 4*(sqrt(2)-1)/3 ≈ 0.55228474983
            const double kappa = 0.5522847498307936;
            float kx = (float)(kappa * rx);
            float ky = (float)(kappa * ry);

            // Four anchor points around the ellipse (starting at angle 0 and going CCW)
            var p0 = new PointF(cx + rx, cy);            // right
            var p1 = new PointF(cx, cy + ry);            // bottom
            var p2 = new PointF(cx - rx, cy);            // left
            var p3 = new PointF(cx, cy - ry);            // top

            // Corresponding control points for each cubic (p0 -> p1), (p1 -> p2), (p2 -> p3), (p3 -> p0)
            // For each quarter: control points are offset along tangents by kx/ky
            var c0a = new PointF(p0.X, p0.Y + ky);      // first control for segment p0->p1
            var c0b = new PointF(p1.X + kx, p1.Y);      // second control for segment p0->p1

            var c1a = new PointF(p1.X - kx, p1.Y);      // p1->p2
            var c1b = new PointF(p2.X, p2.Y + ky);

            var c2a = new PointF(p2.X, p2.Y - ky);      // p2->p3
            var c2b = new PointF(p3.X - kx, p3.Y);

            var c3a = new PointF(p3.X + kx, p3.Y);      // p3->p0
            var c3b = new PointF(p0.X, p0.Y - ky);

            // Build path data: MoveTo p0, then four cubic curve segments, then close
            var segs = new SvgPathSegmentList
            {
                new SvgMoveToSegment(false, p0),

                // SvgCubicCurveSegment constructors:
                // public SvgCubicCurveSegment(bool isRelative, PointF firstControlPoint, PointF secondControlPoint, PointF end)
                // we use absolute coordinates (isRelative: false)
                new SvgCubicCurveSegment(false, c0a, c0b, p1), // p0 -> p1
                new SvgCubicCurveSegment(false, c1a, c1b, p2), // p1 -> p2
                new SvgCubicCurveSegment(false, c2a, c2b, p3), // p2 -> p3
                new SvgCubicCurveSegment(false, c3a, c3b, p0), // p3 -> p0

                new SvgClosePathSegment(false) // z
            };

            return ReadPath(new SvgPath { PathData = segs });
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
            PointF cur = new PointF(0, 0);
            PointF? start = null;
            foreach (var seg in segs)
            {
                switch (seg)
                {
                    case SvgMoveToSegment m:
                        cur = PopulatePointValues(m.End, m.IsRelative, cur);
                        break;

                    case SvgLineSegment ls:
                        {
                            var a = cur;
                            var b = PopulatePointValues(ls.End, ls.IsRelative, cur);
                            indices.Add(positions.Add(TransformPoint(path, a)));
                            indices.Add(positions.Add(TransformPoint(path, b)));
                            cur = b;
                        }
                        break;

                    case SvgCubicCurveSegment cs:
                        {
                            var p0 = TransformPoint(path, cur);
                            PointF firstCP = PopulatePointValues(cs.FirstControlPoint, cs.IsRelative, cur);
                            var c1 = TransformPoint(path, firstCP);
                            PointF secondCP = PopulatePointValues(cs.SecondControlPoint, cs.IsRelative, cur);
                            var c2 = TransformPoint(path, secondCP);
                            PointF end = PopulatePointValues(cs.End, cs.IsRelative, cur);
                            var p3 = TransformPoint(path, end);
                            FlattenCubic(positions, indices, p0, c1, c2, p3);
                            cur = end;
                        }
                        break;

                    case SvgQuadraticCurveSegment qs:
                        {
                            var p0 = cur;
                            var transformedStart = TransformPoint(path, p0);
                            var cp = PopulatePointValues(qs.ControlPoint, qs.IsRelative, cur);
                            var transformedCP = TransformPoint(path, cp);
                            var end = PopulatePointValues(qs.End, qs.IsRelative, cur);
                            var transformedEnd = TransformPoint(path, end);
                            FlattenQuadratic(positions, indices, transformedStart, transformedCP, transformedEnd);
                            cur = end;
                        }
                        break;

                    //case SvgArcSegment asg:
                    //    {
                    //        var arcStart = cur;
                    //        var end = PopulatePointValues(asg.End, asg.IsRelative, cur);
                    //        //FlattenArc(path, asg, arcStart);
                    //        cur = end;
                    //        throw new NotImplementedException();
                    //    }
                    //    break;

                    case SvgClosePathSegment cl:
                        {
                            var end = PopulatePointValues(cl.End, cl.IsRelative, cur);
                            if (cur != end)
                            {
                                indices.Add(positions.Add(TransformPoint(path, cur)));
                                indices.Add(positions.Add(TransformPoint(path, end)));
                            }
                            cur = end;
                        }
                        break;

                    default:
                        // Other segments (if any) can be handled similarly.
                        throw new NotImplementedException($"{seg.GetType().Name} not supported yet.");
                }
                if (!start.HasValue)
                    start = cur;
            }

            mesh.AddAttribute(MeshAttributeKey.Position, positions);
            mesh.AddIndices(indices);
            mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.LineList, 0, indices.Count));

            return mesh;
        }

        private PointF PopulatePointValues(PointF end, bool isRelative, PointF cur)
        {
            var res = end;
            if (isRelative)
            {
                if (float.IsNaN(res.X))
                    res.X = 0.0f;
                if (float.IsNaN(res.Y))
                    res.Y = 0.0f;
                res = new PointF(res.X + cur.X, res.Y + cur.Y);
            }
            else
            {
                if (float.IsNaN(res.X))
                    res.X = cur.X;
                if (float.IsNaN(res.Y))
                    res.Y = cur.Y;
            }
            return res;
        }

        private Vector3 Mid(Vector3 a, Vector3 b)
        {
            return (a + b) * 0.5f;
        }

        private void FlattenCubic(IMeshVertexAttribute<Vector3> positions, IList<int> indices, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            if (p0.IsNanOrInf() || p1.IsNanOrInf() || p2.IsNanOrInf() || p3.IsNanOrInf())
                throw new ArgumentException("Invalid point argument. It should not contain NaN or Inf numbers.");
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

        private void FlattenQuadratic(IMeshVertexAttribute<Vector3> positions, IList<int> indices, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var c1 = p0 + (2f / 3f) * (p1 - p0);
            var c2 = p2 + (2f / 3f) * (p1 - p2);
            FlattenCubic(positions, indices, p0, c1, c2, p2);
        }

        private static float DistancePointToLine(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 u = b - a;
            Vector3 v = p - a;
            float len2 = u.LengthSquared();
            if (len2 == 0) return v.Length();
            var t = Vector3.Dot(u, v) / len2;
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
