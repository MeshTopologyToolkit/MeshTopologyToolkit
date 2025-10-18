using System.Numerics;

namespace MeshTopologyToolkit
{
    public class TRSTransform: ITransform
    {
        public static readonly TRSTransform Identity = new TRSTransform();

        public TRSTransform()
        {
            Translation = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public TRSTransform(Vector3 pos)
        {
            Translation = pos;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public TRSTransform(Vector3 pos, Quaternion rot)
        {
            Translation = pos;
            Rotation = rot;
            Scale = Vector3.One;
        }

        public TRSTransform(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            Translation = pos;
            Rotation = rot;
            Scale = scale;
        }

        public Vector3 Translation { get; }

        public Quaternion Rotation { get; }

        public Vector3 Scale { get; }

        public ITransform Combine(ITransform childTransform)
        {
            if (childTransform is TRSTransform child)
            {
                // 1. Combine Rotation: Quaternions are multiplied to chain rotations.
                // The rotation from right to left is applied first, then the parent's rotation.
                Quaternion newRotation = Rotation * child.Rotation;

                // 2. Combine Scale: Scales are multiplied component-wise (standard for non-uniform scaling).
                // Vector3 multiplication in System.Numerics performs component-wise multiplication.
                Vector3 newScale = Scale * child.Scale;

                // 3. Combine Position:
                //    a) Apply child's scale to its position vector.
                //    b) Rotate the scaled position vector by the parent's rotation.
                //    c) Add the parent's world position (translation).
                Vector3 scaledChildPosition = child.Translation * Scale;
                Vector3 rotatedChildPosition = Vector3.Transform(scaledChildPosition, Rotation);
                Vector3 newPosition = Translation + rotatedChildPosition;

                return new TRSTransform(newPosition, newRotation, newScale);
            }

            return new MatrixTransform(ToMatrix()).Combine(childTransform);
        }

        public Matrix4x4 ToMatrix()
        {
            // Create the individual transformation matrices
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(Scale);
            Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(Rotation);
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(Translation);

            // Combine them in the standard order: Scale -> Rotation -> Translation
            // Note: Matrix multiplication is applied right-to-left for vectors (v * M1 * M2),
            // but System.Numerics Matrix4x4.operator* chains them correctly for transformation (M_final = M_translation * M_rotation * M_scale).
            Matrix4x4 worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;

            return worldMatrix;
        }

        /// <summary>
        /// Transforms a position vector (point) from local space defined by this Transform
        /// into world space. A position is affected by scale, rotation, and translation.
        /// </summary>
        /// <param name="localPoint">The position vector in local space.</param>
        /// <returns>The position vector in world space.</returns>
        public Vector3 TransformPosition(Vector3 localPoint)
        {
            // 1. Apply Scale (component-wise multiplication)
            Vector3 scaledPosition = localPoint * Scale;

            // 2. Apply Rotation
            // Vector3.Transform(vector, quaternion) handles the rotation.
            Vector3 rotatedPosition = Vector3.Transform(scaledPosition, Rotation);

            // Translation is intentionally excluded for direction vectors.
            return rotatedPosition + Translation;
        }

        /// <summary>
        /// Transforms a direction vector from local space defined by this Transform
        /// into world space. A direction is affected by scale and rotation, but NOT translation.
        /// </summary>
        /// <param name="localDirection">The direction vector in local space (does not need to be unit length).</param>
        /// <returns>The direction vector in world space (retains length from scale).</returns>
        public Vector3 TransformDirection(Vector3 localDirection)
        {
            // 1. Apply Scale (component-wise multiplication)
            Vector3 scaledDirection = localDirection * Scale;

            // 2. Apply Rotation
            // Vector3.Transform(vector, quaternion) handles the rotation.
            Vector3 rotatedDirection = Vector3.Transform(scaledDirection, Rotation);

            // Translation is intentionally excluded for direction vectors.
            return rotatedDirection;
        }
    }
}
