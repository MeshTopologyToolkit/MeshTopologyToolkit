using System;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public struct Color32
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }


        public Color32(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
            A = 255;
        }
        public Color32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        public Color32(float r, float g, float b)
        {
            R = ToByte(r);
            G = ToByte(g);
            B = ToByte(b);
            A = 255;
        }
        public Color32(float r, float g, float b, float a)
        {
            R = ToByte(r);
            G = ToByte(g);
            B = ToByte(b);
            A = ToByte(a);
        }

        public Color32(Vector4 rgba) : this(rgba.X, rgba.Y, rgba.Z, rgba.W)
        {
        }

        public Color32(Vector3 rgb) : this(rgb.X, rgb.Y, rgb.Z)
        {
        }

        public override string ToString()
        {
            return A == 255
                ? $"#{R:X2}{G:X2}{B:X2}"
                : $"#{R:X2}{G:X2}{B:X2}{A:X2}";
        }

        public static implicit operator Color32(Vector4 rgba) => new Color32(rgba);

        public static implicit operator Color32(Vector3 rgb) => new Color32(rgb);

        public static implicit operator Vector4(Color32 color) => new Vector4(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            color.A / 255f);

        public static implicit operator Vector3(Color32 color) => new Vector3(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f);

        public static Color32 EncodeNormal(Vector3 normal)
        {
            normal = Vector3.Normalize(normal);
            byte r = ToByte(normal.X * 0.5f + 0.5f);
            byte g = ToByte(normal.Y * 0.5f + 0.5f);
            byte b = ToByte(normal.Z * 0.5f + 0.5f);
            return new Color32(r, g, b, 255);
        }

        private static byte ToByte(float value)
        {
            var intValue = (int)MathF.Round(value * 255.0f);
            return intValue <= 0 ? (byte)0 : intValue >= 255 ? (byte)255 : (byte)intValue;
        }
    }
}
