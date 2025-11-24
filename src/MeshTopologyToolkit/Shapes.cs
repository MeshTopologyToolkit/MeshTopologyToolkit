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
    }
}
