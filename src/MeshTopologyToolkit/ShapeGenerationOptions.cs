using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class ShapeGenerationOptions
    {
        public Matrix4x4 Transform { get; }

        public bool FlipIndices { get; }

        public MeshAttributeMask Mask { get; }

        public ShapeGenerationOptions(MeshAttributeMask mask = MeshAttributeMask.All)
        {
            Transform = Matrix4x4.Identity;
            Mask = mask;
        }
        public ShapeGenerationOptions(Matrix4x4 matrix, MeshAttributeMask mask, bool flipIndices)
        {
            Transform = matrix;
            FlipIndices = flipIndices;
            Mask = mask;
        }

        public ShapeGenerationOptions WithScale(float scale)
        {
            return new ShapeGenerationOptions(Matrix4x4.CreateScale(scale) * Transform, Mask, FlipIndices);
        }

        public ShapeGenerationOptions WithFlipIndices()
        {
            return new ShapeGenerationOptions(Transform, Mask, true);
        }

        public Vector3 TransformPosition(Vector3 pos)
        {
            return Vector3.Transform(pos, Transform);
        }

        public Vector3 TransformNormal(Vector3 normal)
        {
            return Vector3.TransformNormal(normal, Transform);
        }

        public Vector4 TransformTangent(Vector4 tangent)
        {
            var transformed = Vector4.Transform(new Vector4(tangent.X, tangent.Y, tangent.Z, 0.0f), Transform);
            return new Vector4(transformed.X, transformed.Y, transformed.Z, tangent.W);
        }

        public IReadOnlyList<int> TransformIndices(IReadOnlyList<int> list)
        {
            if (!FlipIndices)
                return list;

            var result = new int[list.Count];
            for (int i = 0; i < list.Count; i += 3)
            {
                result[i] = list[i];
                result[i + 1] = list[i + 2];
                result[i + 2] = list[i + 1];
            }
            return result;
        }
    }
}
