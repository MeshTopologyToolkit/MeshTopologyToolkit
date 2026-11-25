using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public struct SpaceTransformer
    {
        public static readonly SpaceTransformer Identity = new SpaceTransformer(Matrix4x4.Identity, 1.0f);

        private Matrix4x4 _rotationOnly;
        private Vector3 _axisSigns;
        private Matrix4x4 _unitRotation;

        public SpaceTransformer(Matrix4x4 rotationMatrix, float unitScale)
        {
            var axisX = new Vector3(rotationMatrix.M11, rotationMatrix.M12, rotationMatrix.M13);
            var axisY = new Vector3(rotationMatrix.M21, rotationMatrix.M22, rotationMatrix.M23);
            var axisZ = new Vector3(rotationMatrix.M31, rotationMatrix.M32, rotationMatrix.M33);
            _axisSigns = new Vector3(
                Vector3.Abs(axisX) == axisX ? 1.0f : -1.0f,
                Vector3.Abs(axisY) == axisY ? 1.0f : -1.0f,
                Vector3.Abs(axisZ) == axisZ ? 1.0f : -1.0f
            );

            _unitRotation = rotationMatrix;
            _unitRotation.M11 *= unitScale;
            _unitRotation.M12 *= unitScale;
            _unitRotation.M13 *= unitScale;

            _unitRotation.M21 *= unitScale;
            _unitRotation.M22 *= unitScale;
            _unitRotation.M23 *= unitScale;

            _unitRotation.M31 *= unitScale;
            _unitRotation.M32 *= unitScale;
            _unitRotation.M33 *= unitScale;

            _rotationOnly = rotationMatrix;
        }

        // Transform mesh vertex position (includes unit scale)
        public Vector3 TransformPosition(Vector3 position)
        {
            return Vector3.Transform(position, _unitRotation);
        }

        // Transform vertex normal (rotation only, ignore scale)
        public Vector3 TransformNormal(Vector3 normal)
        {
            Vector3 transformed = Vector3.TransformNormal(normal, _rotationOnly);
            return Vector3.Normalize(transformed);
        }

        // Transform vertex tangent
        public Vector4 TransformTangent(Vector4 tangent, bool flipW)
        {
            var t = new Vector3(tangent.X, tangent.Y, tangent.Z);
            t = TransformNormal(t);
            return new Vector4(t, tangent.W);
        }

        // Transform node transform given as full 4x4 matrix
        public ITransform TransformNode(ITransform nodeMatrix)
        {
            if (nodeMatrix is MatrixTransform matrixTransform)
            {
                return TransformNode(matrixTransform);
            }
            else if (nodeMatrix is TRSTransform trsTransform)
            {
                return TransformNode(trsTransform);
            }
            else
            {
                throw new InvalidOperationException("Unsupported ITransform type.");
            }
        }

        // Transform node transform given as full 4x4 matrix
        public MatrixTransform TransformNode(MatrixTransform nodeMatrix)
        {
            // Decompose node
            if (!Matrix4x4.Decompose(nodeMatrix.Transform, out Vector3 nodeScale, out Quaternion nodeRotation, out Vector3 nodeTranslation))
                throw new InvalidOperationException("Failed to decompose node matrix.");

#if DEBUG
            var reconstructed = Matrix4x4.CreateScale(nodeScale) *
                   Matrix4x4.CreateFromQuaternion(nodeRotation) *
                   Matrix4x4.CreateTranslation(nodeTranslation);
            if (Math.Abs(reconstructed.M12 - nodeMatrix.Transform.M12) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M12} != {nodeMatrix.Transform.M12}.");
            if (Math.Abs(reconstructed.M13 - nodeMatrix.Transform.M13) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M13} != {nodeMatrix.Transform.M13}.");
            if (Math.Abs(reconstructed.M14 - nodeMatrix.Transform.M14) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M14} != {nodeMatrix.Transform.M14}.");
            if (Math.Abs(reconstructed.M21 - nodeMatrix.Transform.M21) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M21} != {nodeMatrix.Transform.M21}.");
            if (Math.Abs(reconstructed.M22 - nodeMatrix.Transform.M22) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M22} != {nodeMatrix.Transform.M22}.");
            if (Math.Abs(reconstructed.M23 - nodeMatrix.Transform.M23) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M23} != {nodeMatrix.Transform.M23}.");
            if (Math.Abs(reconstructed.M24 - nodeMatrix.Transform.M24) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M24} != {nodeMatrix.Transform.M24}.");
            if (Math.Abs(reconstructed.M31 - nodeMatrix.Transform.M31) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M31} != {nodeMatrix.Transform.M31}.");
            if (Math.Abs(reconstructed.M32 - nodeMatrix.Transform.M32) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M32} != {nodeMatrix.Transform.M32}.");
            if (Math.Abs(reconstructed.M33 - nodeMatrix.Transform.M33) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M33} != {nodeMatrix.Transform.M33}.");
            if (Math.Abs(reconstructed.M34 - nodeMatrix.Transform.M34) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M34} != {nodeMatrix.Transform.M34}.");
            if (Math.Abs(reconstructed.M41 - nodeMatrix.Transform.M41) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M41} != {nodeMatrix.Transform.M41}.");
            if (Math.Abs(reconstructed.M42 - nodeMatrix.Transform.M42) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M42} != {nodeMatrix.Transform.M42}.");
            if (Math.Abs(reconstructed.M43 - nodeMatrix.Transform.M43) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M43} != {nodeMatrix.Transform.M43}.");
            if (Math.Abs(reconstructed.M44 - nodeMatrix.Transform.M44) > 1e-6f)
                throw new Exception($"Failed to decompose node matrix correctly {reconstructed.M44} != {nodeMatrix.Transform.M44}.");
#endif

            // Apply TRS version
            TransformNode(ref nodeTranslation, ref nodeRotation, ref nodeScale);

            // Recompose
            return new MatrixTransform(Matrix4x4.CreateScale(nodeScale) *
                   Matrix4x4.CreateFromQuaternion(nodeRotation) *
                   Matrix4x4.CreateTranslation(nodeTranslation));
        }


        // Transform node transform given as TRS
        public TRSTransform TransformNode(TRSTransform nodeMatrix)
        {
            Vector3 nodeScale = nodeMatrix.Scale;
            Quaternion nodeRotation = nodeMatrix.Rotation;
            Vector3 nodeTranslation = nodeMatrix.Translation;
            
            // Apply TRS version
            TransformNode(ref nodeTranslation, ref nodeRotation, ref nodeScale);

            // Recompose
            return new TRSTransform(nodeTranslation, nodeRotation, nodeScale);
        }

        // Transform node TRS: translation scaled, rotation remapped, scale remapped
        public void TransformNode(ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
        {
            // Translation: apply rotation + unit scale
            translation = Vector3.Transform(translation, _unitRotation);

            rotation = TransformRotation(rotation);
            scale = TransformScale(scale);
        }

        /// <summary>
        /// Expresses a rotation in the new space (basis change U^-1 * R * U, which also works when the space change flips handedness).
        /// </summary>
        /// <remarks>
        /// Conjugating a rotation by an orthogonal matrix U keeps the angle and maps the axis to det(U) * U^-1 * axis,
        /// so the quaternion remap is linear (and can be applied to cubic spline tangents too).
        /// </remarks>
        public Quaternion TransformRotation(Quaternion rotation)
        {
            Matrix4x4.Invert(_rotationOnly, out var inverse);
            float determinant = _rotationOnly.GetDeterminant() < 0.0f ? -1.0f : 1.0f;
            var axis = Vector3.TransformNormal(new Vector3(rotation.X, rotation.Y, rotation.Z), inverse) * determinant;
            return new Quaternion(axis, rotation.W);
        }

        /// <summary>
        /// Remaps axes of a node scale. Unit scale is not applied.
        /// </summary>
        public Vector3 TransformScale(Vector3 scale)
        {
            var axisX = Vector3.Abs(new Vector3(_rotationOnly.M11, _rotationOnly.M21, _rotationOnly.M31));
            var axisY = Vector3.Abs(new Vector3(_rotationOnly.M12, _rotationOnly.M22, _rotationOnly.M32));
            var axisZ = Vector3.Abs(new Vector3(_rotationOnly.M13, _rotationOnly.M23, _rotationOnly.M33));

            return new Vector3(
                Vector3.Dot(scale, axisX),
                Vector3.Dot(scale, axisY),
                Vector3.Dot(scale, axisZ));
        }

        /// <summary>
        /// Expresses a matrix that maps between two spaces that are both being changed (for example, an inverse bind matrix) in the new space.
        /// </summary>
        public Matrix4x4 TransformMatrix(Matrix4x4 matrix)
        {
            var u = _unitRotation;
            u.M44 = 1.0f;
            Matrix4x4.Invert(u, out var inverse);
            return inverse * matrix * u;
        }
    }
}
