using System;
using System.Numerics;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Transform properties of an FBX model: translation, Euler rotation, scale, pre/post rotation, pivots and offsets.
    /// Follows the evaluation order from the FBX SDK (also used by ufbx):
    /// <c>World = Parent * T * Roff * Rp * Rpre * R * Rpost^-1 * Rp^-1 * Soff * Sp * S * Sp^-1</c> in the column vector convention.
    /// </summary>
    internal struct FbxNodeTransform
    {
        public Vector3 Translation;
        /// <summary>Euler angles in degrees.</summary>
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3 PreRotation;
        public Vector3 PostRotation;
        public Vector3 RotationPivot;
        public Vector3 RotationOffset;
        public Vector3 ScalePivot;
        public Vector3 ScaleOffset;
        public int RotationOrder;
        public bool RotationActive;

        public static FbxNodeTransform Read(FbxProps props)
        {
            return new FbxNodeTransform
            {
                Translation = props.GetVector3("Lcl Translation", Vector3.Zero),
                Rotation = props.GetVector3("Lcl Rotation", Vector3.Zero),
                Scale = props.GetVector3("Lcl Scaling", Vector3.One),
                PreRotation = props.GetVector3("PreRotation", Vector3.Zero),
                PostRotation = props.GetVector3("PostRotation", Vector3.Zero),
                RotationPivot = props.GetVector3("RotationPivot", Vector3.Zero),
                RotationOffset = props.GetVector3("RotationOffset", Vector3.Zero),
                ScalePivot = props.GetVector3("ScalingPivot", Vector3.Zero),
                ScaleOffset = props.GetVector3("ScalingOffset", Vector3.Zero),
                RotationOrder = props.GetInt("RotationOrder", 0),
                RotationActive = props.GetBool("RotationActive", true),
            };
        }

        /// <summary>
        /// Rotation matrix (row vector convention) for the Euler angles in degrees. The first letter of the order is applied first.
        /// </summary>
        public static Matrix4x4 EulerToMatrix(Vector3 degrees, int order)
        {
            const float ToRad = (float)(Math.PI / 180.0);
            var x = Matrix4x4.CreateRotationX(degrees.X * ToRad);
            var y = Matrix4x4.CreateRotationY(degrees.Y * ToRad);
            var z = Matrix4x4.CreateRotationZ(degrees.Z * ToRad);
            switch (order)
            {
                case 1: return x * z * y;
                case 2: return y * z * x;
                case 3: return y * x * z;
                case 4: return z * x * y;
                case 5: return z * y * x;
                default: return x * y * z;
            }
        }

        public bool HasPivotsOrOffsets =>
            RotationPivot != Vector3.Zero || RotationOffset != Vector3.Zero || ScalePivot != Vector3.Zero || ScaleOffset != Vector3.Zero;

        public Matrix4x4 GetRotationMatrix()
        {
            // Same as ufbx: pre/post rotation and the rotation order are only used when the rotation is active.
            if (!RotationActive)
                return EulerToMatrix(Rotation, 0);

            Matrix4x4.Invert(EulerToMatrix(PostRotation, 0), out var postInverse);
            return postInverse * EulerToMatrix(Rotation, RotationOrder) * EulerToMatrix(PreRotation, 0);
        }

        /// <summary>Gets the local matrix in the row vector convention (a point is transformed as <c>p * M</c>).</summary>
        public Matrix4x4 ToMatrix()
        {
            var rotation = GetRotationMatrix();
            // Operations in order of application.
            return Matrix4x4.CreateTranslation(-ScalePivot)
                 * Matrix4x4.CreateScale(Scale)
                 * Matrix4x4.CreateTranslation(ScalePivot)
                 * Matrix4x4.CreateTranslation(ScaleOffset)
                 * Matrix4x4.CreateTranslation(-RotationPivot)
                 * rotation
                 * Matrix4x4.CreateTranslation(RotationPivot)
                 * Matrix4x4.CreateTranslation(RotationOffset)
                 * Matrix4x4.CreateTranslation(Translation);
        }

        public ITransform ToTransform()
        {
            if (HasPivotsOrOffsets)
                return new MatrixTransform(ToMatrix());
            var rotation = Quaternion.CreateFromRotationMatrix(GetRotationMatrix());
            if (float.IsNaN(rotation.X))
                rotation = Quaternion.Identity;
            return new TRSTransform(Translation, Quaternion.Normalize(rotation), Scale);
        }
    }
}
