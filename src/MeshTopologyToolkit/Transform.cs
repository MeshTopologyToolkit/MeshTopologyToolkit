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
    }
}
