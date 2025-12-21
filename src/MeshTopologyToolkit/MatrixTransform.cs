using System.Numerics;

namespace MeshTopologyToolkit
{
    public class MatrixTransform : ITransform
    {
        public static readonly MatrixTransform Identity = new MatrixTransform();

        public MatrixTransform()
        {
            Transform = Matrix4x4.Identity;
        }

        public MatrixTransform(Matrix4x4 transform)
        {
            Transform = transform;
        }

        public Matrix4x4 Transform { get; }

        public ITransform Combine(ITransform childTransform)
        {
            return new MatrixTransform(childTransform.ToMatrix() * Transform);
        }

        public Matrix4x4 ToMatrix()
        {
            return Transform;
        }

        /// <summary>
        /// Transforms a position vector (point) from local space defined by this Transform
        /// into world space. A position is affected by scale, rotation, and translation.
        /// </summary>
        /// <param name="localPoint">The position vector in local space.</param>
        /// <returns>The position vector in world space.</returns>
        public Vector3 TransformPosition(Vector3 localPoint)
        {
            // Vector3.Transform handles the 4D homogenous coordinate (x, y, z, 1) transformation
            // for points, including translation.
            return Vector3.Transform(localPoint, Transform);
        }

        /// <summary>
        /// Transforms a direction vector from local space defined by this Transform
        /// into world space. A direction is affected by scale and rotation, but NOT translation.
        /// </summary>
        /// <param name="localDirection">The direction vector in local space (does not need to be unit length).</param>
        /// <returns>The direction vector in world space (retains length from scale).</returns>
        public Vector3 TransformNormal(Vector3 localDirection)
        {
            return Vector3.TransformNormal(localDirection, Transform);
        }

        public TRSTransform ToTRS()
        {
            Matrix4x4.Decompose(Transform, out var scale, out var rotation, out var translation);
            return new TRSTransform(translation, rotation, scale);
        }
    }
}
