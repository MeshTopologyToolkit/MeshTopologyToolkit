using System.Numerics;

namespace MeshTopologyToolkit.TextureFormats
{
    public struct Color32
    {
        public byte R, G, B, A;
        public Color32(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }

        public Color32(Vector4 v)
        {
            R = (byte)MathF.Round((v.X * 0.5f + 0.5f) * 255f);
            G = (byte)MathF.Round((v.Y * 0.5f + 0.5f) * 255f);
            B = (byte)MathF.Round((v.Z * 0.5f + 0.5f) * 255f);
            A = (byte)MathF.Round((v.W * 0.5f + 0.5f) * 255f);
        }

        public Color32(Vector3 v)
        {
            R = (byte)MathF.Round((v.X * 0.5f + 0.5f) * 255f);
            G = (byte)MathF.Round((v.Y * 0.5f + 0.5f) * 255f);
            B = (byte)MathF.Round((v.Z * 0.5f + 0.5f) * 255f);
            A = 255;
        }

        public static Color32 FromNormal(Vector3 normal)
        {
            normal = Vector3.Normalize(normal);
            byte r = (byte)MathF.Round((normal.X * 0.5f + 0.5f) * 255f);
            byte g = (byte)MathF.Round((normal.Y * 0.5f + 0.5f) * 255f);
            byte b = (byte)MathF.Round((normal.Z * 0.5f + 0.5f) * 255f);
            return new Color32(r, g, b, 255);
        }
    }

}
