using System.Numerics;

namespace MeshTopologyToolkit
{
    public class TRSTransform: ITransform
    {
        public static readonly TRSTransform Identity = new TRSTransform();

        public TRSTransform()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public TRSTransform(Vector3 pos)
        {
            Position = pos;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
        }

        public TRSTransform(Vector3 pos, Quaternion rot)
        {
            Position = pos;
            Rotation = rot;
            Scale = Vector3.One;
        }

        public TRSTransform(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            Position = pos;
            Rotation = rot;
            Scale = scale;
        }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }

        public Vector3 Scale { get; }
    }
}
