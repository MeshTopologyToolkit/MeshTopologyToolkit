using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class Shapes
    {
        /// <summary>
        /// Generate box shape in Gltf style (right-handed UVs, bitangent pointing down).
        /// </summary>
        /// <param name="options">Shape generation options.</param>
        /// <returns>Generated mesh.</returns>
        public static IMesh BuildBox(ShapeGenerationOptions? options = null)
        {
            options = options ?? new ShapeGenerationOptions();

            var mesh = new SeparatedIndexedMesh();
            {
                var values = new ListMeshVertexAttribute<Vector3>();
                values.Add(options.TransformPosition(new Vector3(-0.5f, -0.5f, 0.5f)));
                values.Add(options.TransformPosition(new Vector3(-0.5f, 0.5f, 0.5f)));
                values.Add(options.TransformPosition(new Vector3(-0.5f, 0.5f, -0.5f)));
                values.Add(options.TransformPosition(new Vector3(-0.5f, -0.5f, -0.5f)));
                values.Add(options.TransformPosition(new Vector3(0.5f, 0.5f, 0.5f)));
                values.Add(options.TransformPosition(new Vector3(0.5f, 0.5f, -0.5f)));
                values.Add(options.TransformPosition(new Vector3(0.5f, -0.5f, 0.5f)));
                values.Add(options.TransformPosition(new Vector3(0.5f, -0.5f, -0.5f)));
                var indices = options.TransformIndices(new List<int>() { 
                    0, 1, 2,  0, 2, 3,
                    1, 4, 5,  1, 5, 2,
                    4, 6, 7,  4, 7, 5,
                    6, 0, 3,  6, 3, 7,
                    4, 1, 0,  4, 0, 6,
                    3, 2, 5,  7, 3, 5 });
                mesh.AddAttribute(MeshAttributeKey.Position, values, indices);
            }
            if ((options.Mask & MeshAttributeMask.Normal) == MeshAttributeMask.Normal)
            {
                var values = new ListMeshVertexAttribute<Vector3>();
                values.Add(new Vector3(-1f, 0f, 0f));
                values.Add(new Vector3(0f, 1f, 0f));
                values.Add(new Vector3(1f, 0f, 0f));
                values.Add(new Vector3(0f, -1f, 0f));
                values.Add(new Vector3(0f, 0f, 1f));
                values.Add(new Vector3(0f, 0f, -1f));
                var indices = options.TransformIndices(new List<int>() { 
                    0, 0, 0, 0, 0, 0,
                    1, 1, 1, 1, 1, 1,
                    2, 2, 2, 2, 2, 2,
                    3, 3, 3, 3, 3, 3,
                    4, 4, 4, 4, 4, 4,
                    5, 5, 5, 5, 5, 5 });
                mesh.AddAttribute(MeshAttributeKey.Normal, values, indices);
            }
            if ((options.Mask & MeshAttributeMask.TexCoord) == MeshAttributeMask.TexCoord)
            {
                var values = new ListMeshVertexAttribute<Vector2>();
                values.Add(new Vector2(1f, 1f));
                values.Add(new Vector2(1f, 0f));
                values.Add(new Vector2(0f, 0f));
                values.Add(new Vector2(0f, 1f));
                var indices = options.TransformIndices(new List<int>() { 
                    0, 1, 2, 0, 2, 3, 
                    1, 2, 3, 1, 3, 0, 
                    2, 3, 0, 2, 0, 1, 
                    3, 0, 1, 3, 1, 2, 
                    1, 2, 3, 1, 3, 0, 
                    0, 1, 2, 3, 0, 2 });
                mesh.AddAttribute(MeshAttributeKey.TexCoord, values, indices);
            }
            if ((options.Mask & MeshAttributeMask.Color) == MeshAttributeMask.Color)
            {
                var values = new ConstMeshVertexAttribute<Vector4>(Vector4.One, 1);
                var indices = options.TransformIndices(new List<int>() {
                    0, 0, 0, 0, 0, 0, 
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0 });
                mesh.AddAttribute(MeshAttributeKey.Color, values, indices);
            }

            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, mesh.GetAttributeIndices(MeshAttributeKey.Position).Count));

            if ((options.Mask & MeshAttributeMask.Tangent) == MeshAttributeMask.Tangent)
            {
                mesh.EnsureTangents();
            }
            return mesh;
        }

        /// <summary>
        /// Generate sphere shape in Gltf style (right-handed UVs, bitangent pointing down).
        /// </summary>
        /// <param name="options">Shape generation options.</param>
        /// <returns>Generated mesh.</returns>
        public static IMesh BuildSphere(int segments, ShapeGenerationOptions? options = null)
        {
            options = options ?? new ShapeGenerationOptions();

            segments = Math.Max(segments, 1);
            var mesh = new UnifiedIndexedMesh();

            int maxPhiIndex = segments * 4;
            int maxThetaIndex = segments * 2;
            {
                var positions = new ListMeshVertexAttribute<Vector3>();
                for (int phiIndex = 0; phiIndex <= maxPhiIndex; ++phiIndex)
                {
                    var phi = (float)phiIndex / maxThetaIndex * MathF.PI;
                    for (int thetaIndex = 0; thetaIndex <= maxThetaIndex; ++thetaIndex)
                    {
                        var theta = (float)thetaIndex / maxThetaIndex * MathF.PI;
                        var st = MathF.Sin(theta);
                        var x = st * MathF.Cos(phi);
                        var y = -MathF.Cos(theta);
                        var z = st * MathF.Sin(phi);
                        var position = new Vector3(x * 0.5f, y * 0.5f, z * 0.5f);
                        positions.Add(options.TransformPosition(position));
                    }
                }
                
                mesh.AddAttribute(MeshAttributeKey.Position, positions);
            }

            if ((options.Mask & MeshAttributeMask.Normal) == MeshAttributeMask.Normal)
            {
                var normals = new ListMeshVertexAttribute<Vector3>();
                for (int phiIndex = 0; phiIndex <= maxPhiIndex; ++phiIndex)
                {
                    var phi = (float)phiIndex / maxThetaIndex * MathF.PI;
                    for (int thetaIndex = 0; thetaIndex <= maxThetaIndex; ++thetaIndex)
                    {
                        var theta = (float)thetaIndex / maxThetaIndex * MathF.PI;
                        var st = MathF.Sin(theta);
                        var x = st* MathF.Cos(phi);
                        var y = -MathF.Cos(theta);
                        var z = st* MathF.Sin(phi);
                        var normal = new Vector3(x, y, z);
                        normals.Add(options.TransformNormal(normal));
                    }
                }

                mesh.AddAttribute(MeshAttributeKey.Normal, normals);
            }

            if ((options.Mask & MeshAttributeMask.TexCoord) == MeshAttributeMask.TexCoord)
            {
                var values = new ListMeshVertexAttribute<Vector2>();
                var indices = new List<int>();
                for (int phiIndex = 0; phiIndex <= maxPhiIndex; ++phiIndex)
                {
                    var phi = (float)phiIndex / maxPhiIndex;
                    for (int thetaIndex = 0; thetaIndex <= maxThetaIndex; ++thetaIndex)
                    {
                        var theta = (float)thetaIndex / maxThetaIndex;
                        var x = 1.0f - phi;
                        var y = 1.0f-theta;
                        var uv = new Vector2(x, y);
                        values.Add(uv);
                    }
                }
                
                mesh.AddAttribute(MeshAttributeKey.TexCoord, values);
            }
            if ((options.Mask & MeshAttributeMask.Color) == MeshAttributeMask.Color)
            {
                var values = new ConstMeshVertexAttribute<Vector4>(Vector4.One, (maxPhiIndex+1)*(maxThetaIndex+1));
                mesh.AddAttribute(MeshAttributeKey.Color, values);
            }

            for (int phiIndex = 0; phiIndex < maxPhiIndex; ++phiIndex)
            {
                var phi = (float)phiIndex / maxThetaIndex * MathF.PI;
                int stride = maxThetaIndex + 1;
                for (int thetaIndex = 0; thetaIndex < maxThetaIndex; ++thetaIndex)
                {
                    var baseIndex = phiIndex * stride + thetaIndex;

                    if (thetaIndex != 0)
                    {
                        mesh.Indices.Add(baseIndex);
                        mesh.Indices.Add(baseIndex + 1);
                        mesh.Indices.Add(baseIndex + stride);
                    }

                    if (thetaIndex != maxThetaIndex - 1)
                    {
                        mesh.Indices.Add(baseIndex + stride + 1);
                        mesh.Indices.Add(baseIndex + stride);
                        mesh.Indices.Add(baseIndex + 1);
                    }
                }
            }
            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, mesh.Indices.Count));

            if ((options.Mask & MeshAttributeMask.Tangent) == MeshAttributeMask.Tangent)
            {
                mesh.EnsureTangents();
            }

            return mesh;

        }


        /// <summary>
        /// Generate sphere shape in Gltf style (right-handed UVs, bitangent pointing down).
        /// </summary>
        /// <param name="segmentsPhi">Number of segments of phi.</param>
        /// <param name="segmentsTheta">Number of segments of theta.</param>
        /// <param name="r">Inner radius.</param>
        /// <param name="options">Shape generation options.</param>
        /// <returns>Generated mesh.</returns>
        public static IMesh BuildTorus(int segmentsPhi = 3, int segmentsTheta = 3, float r = 0.9f, ShapeGenerationOptions? options = null)
        {
            options = options ?? new ShapeGenerationOptions();

            segmentsPhi = Math.Max(segmentsPhi, 4);
            segmentsTheta = Math.Max(segmentsTheta, 4);

            var phiStep = 2.0f * MathF.PI / segmentsPhi;
            var thetaStep = 2.0f * MathF.PI / segmentsTheta;

            var mesh = new UnifiedIndexedMesh();

            var centralRadius = (r + 1.0f) * 0.25f;
            var tubeRadius = (1.0f - r) * 0.25f;
            {
                var positions = new ListMeshVertexAttribute<Vector3>();
                var normals = ((options.Mask & MeshAttributeMask.Normal) == MeshAttributeMask.Normal) ? new ListMeshVertexAttribute<Vector3>() : null;
                for (int phiIndex = 0; phiIndex <= segmentsPhi; ++phiIndex)
                {
                    var phi = (float)phiIndex * phiStep;
                    var direction = new Vector3(MathF.Cos(phi), 0.0f, MathF.Sin(phi));
                    var up = Vector3.UnitY;
                    var center = direction * centralRadius;
                    for (int thetaIndex = 0; thetaIndex <= segmentsTheta; ++thetaIndex)
                    {
                        var theta = (float)thetaIndex * thetaStep;
                        var normal = - direction * MathF.Cos(theta) - up * MathF.Sin(theta);
                        var position = center + tubeRadius* normal;
                        positions.Add(options.TransformPosition(position));
                        if (normals != null)
                        {
                            normals.Add(options.TransformNormal(normal));
                        }
                    }
                }

                mesh.AddAttribute(MeshAttributeKey.Position, positions);
                if (normals != null)
                {
                    mesh.AddAttribute(MeshAttributeKey.Normal, normals);
                }
            }

            if ((options.Mask & MeshAttributeMask.TexCoord) == MeshAttributeMask.TexCoord)
            {
                var values = new ListMeshVertexAttribute<Vector2>();
                var indices = new List<int>();
                for (int phiIndex = 0; phiIndex <= segmentsPhi; ++phiIndex)
                {
                    var phi = (float)phiIndex / segmentsPhi;
                    for (int thetaIndex = 0; thetaIndex <= segmentsTheta; ++thetaIndex)
                    {
                        var theta = (float)thetaIndex / segmentsTheta;
                        var x = 1.0f - phi;
                        var y = 1.0f - theta;
                        var uv = new Vector2(x, y);
                        values.Add(uv);
                    }
                }

                mesh.AddAttribute(MeshAttributeKey.TexCoord, values);
            }
            if ((options.Mask & MeshAttributeMask.Color) == MeshAttributeMask.Color)
            {
                var values = new ConstMeshVertexAttribute<Vector4>(Vector4.One, (segmentsPhi + 1) * (segmentsTheta + 1));
                mesh.AddAttribute(MeshAttributeKey.Color, values);
            }

            for (int phiIndex = 0; phiIndex < segmentsPhi; ++phiIndex)
            {
                var phi = (float)phiIndex / segmentsTheta * MathF.PI;
                int stride = segmentsTheta + 1;
                for (int thetaIndex = 0; thetaIndex < segmentsTheta; ++thetaIndex)
                {
                    var baseIndex = phiIndex * stride + thetaIndex;

                    mesh.Indices.Add(baseIndex);
                    mesh.Indices.Add(baseIndex + 1);
                    mesh.Indices.Add(baseIndex + stride);
                    mesh.Indices.Add(baseIndex + stride + 1);
                    mesh.Indices.Add(baseIndex + stride);
                    mesh.Indices.Add(baseIndex + 1);
                }
            }
            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, mesh.Indices.Count));

            if ((options.Mask & MeshAttributeMask.Tangent) == MeshAttributeMask.Tangent)
            {
                mesh.EnsureTangents();
            }

            return mesh;

        }

        private const int Degree = 3; // Standard cubic Bezier patches

        /// <summary>
        /// Calculates the 3rd degree Bernstein basis polynomial B_i,3(t).
        /// </summary>
        private static float BernsteinBasis(int i, float t)
        {
            switch (i)
            {
                case 0: return (1 - t) * (1 - t) * (1 - t);
                case 1: return 3 * t * (1 - t) * (1 - t);
                case 2: return 3 * t * t * (1 - t);
                case 3: return t * t * t;
                default: return 0;
            }
        }

        /// <summary>
        /// Calculates the derivative of the 3rd degree Bernstein basis polynomial B_i,3'(t).
        /// </summary>
        private static float BernsteinDerivative(int i, float t)
        {
            // d/dt [ B_i,n(t) ] = n * [ B_{i-1,n-1}(t) - B_{i,n-1}(t) ]
            // Since B_{j,n} = 0 if j < 0 or j > n, we handle boundaries.
            float B_prev = (i > 0) ? BernsteinBasis(i - 1, t) : 0f;
            float B_curr = (i < Degree) ? BernsteinBasis(i, t) : 0f;
            return Degree * (B_prev - B_curr);
        }

        public static IMesh BuildQuadPatch(IReadOnlyList<Vector3> controlPoints, int steps, ShapeGenerationOptions? options = null)
        {
            if (controlPoints == null || controlPoints.Count != 16)
                throw new ArgumentException("Quad Bezier patch requires exactly 16 control points (4x4).");

            options = options ?? new ShapeGenerationOptions();

            var data = new UnifiedIndexedMesh();
            var Positions = new ListMeshVertexAttribute<Vector3>((steps + 1) * (steps + 1));
            var Normals = new ListMeshVertexAttribute<Vector3>((steps + 1) * (steps + 1));
            data.AddAttribute(MeshAttributeKey.Position, Positions);
            if (options.Mask.HasFlag(MeshAttributeMask.Normal))
                data.AddAttribute(MeshAttributeKey.Normal, Normals);
            float stepSize = 1f / steps;

            // 1. Calculate Positions and Normals
            for (int i = 0; i <= steps; i++)
            {
                float u = i * stepSize;
                for (int j = 0; j <= steps; j++)
                {
                    float v = j * stepSize;

                    Vector3 position = new Vector3(0, 0, 0);
                    Vector3 tangentU = new Vector3(0, 0, 0);
                    Vector3 tangentV = new Vector3(0, 0, 0);

                    // Sum over all 16 control points
                    for (int m = 0; m <= Degree; m++) // u-axis basis
                    {
                        float Bu = BernsteinBasis(m, u);
                        float dBu = BernsteinDerivative(m, u);

                        for (int n = 0; n <= Degree; n++) // v-axis basis
                        {
                            int cpIndex = m * 4 + n; // Index into the 1D array
                            Vector3 C = controlPoints[cpIndex];
                            float Bv = BernsteinBasis(n, v);
                            float dBv = BernsteinDerivative(n, v);

                            // P(u,v) = Sum_m Sum_n ( B_m,3(u) * B_n,3(v) * C_mn )
                            float weight = Bu * Bv;
                            position += C * weight;

                            // dP/du = Sum_m Sum_n ( B'_m,3(u) * B_n,3(v) * C_mn )
                            float weightU = dBu * Bv;
                            tangentU += C * weightU;

                            // dP/dv = Sum_m Sum_n ( B_m,3(u) * B'_n,3(v) * C_mn )
                            float weightV = Bu * dBv;
                            tangentV += C * weightV;
                        }
                    }

                    Positions.Add(position);

                    // Normal is the normalized cross product of the tangents
                    Vector3 normal = Vector3.Normalize(Vector3.Cross(tangentU, tangentV));
                    if (normal.IsNanOrInf())
                    {
                        //throw new Exception();
                    }
                    Normals.Add(normal);
                }
            }

            // 2. Generate Face Indices (Quads into two triangles)
            int numVerticesPerSide = steps + 1;
            for (int i = 0; i < steps; i++)
            {
                for (int j = 0; j < steps; j++)
                {
                    int i0 = i * numVerticesPerSide + j;
                    int i1 = i0 + 1;
                    int i2 = (i + 1) * numVerticesPerSide + j;
                    int i3 = i2 + 1;

                    // Triangle 1 (i0, i2, i1)
                    data.Indices.Add(i0);
                    data.Indices.Add(i1);
                    data.Indices.Add(i2);

                    // Triangle 2 (i1, i2, i3)
                    data.Indices.Add(i1);
                    data.Indices.Add(i3);
                    data.Indices.Add(i2);
                }
            }
            data.WithTriangleList();

            return data;
        }
    }
}
