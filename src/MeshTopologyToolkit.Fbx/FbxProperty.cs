using System;
using System.Globalization;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Type codes of FBX node properties. The values are the characters used by the binary format.
    /// </summary>
    public enum FbxPropertyType : byte
    {
        Bool = (byte)'C',
        Short = (byte)'Y',
        Int = (byte)'I',
        Float = (byte)'F',
        Double = (byte)'D',
        Long = (byte)'L',
        String = (byte)'S',
        Raw = (byte)'R',
        BoolArray = (byte)'b',
        IntArray = (byte)'i',
        LongArray = (byte)'l',
        FloatArray = (byte)'f',
        DoubleArray = (byte)'d',
    }

    /// <summary>
    /// A single value of an <see cref="FbxNode"/>. Accessors convert leniently between numeric types.
    /// </summary>
    public sealed class FbxProperty
    {
        private FbxProperty(FbxPropertyType type, object value)
        {
            Type = type;
            Value = value;
        }

        public FbxPropertyType Type { get; }

        /// <summary>
        /// Boxed value: byte (type C, a boolean or a character), short, int, float, double, long, string, byte[] (raw) or an array of bool/int/long/float/double.
        /// </summary>
        public object Value { get; }

        public static FbxProperty FromBool(bool v) => new FbxProperty(FbxPropertyType.Bool, (byte)(v ? 1 : 0));
        /// <summary>A byte property (type code C). Legacy files store bare characters as bytes.</summary>
        public static FbxProperty FromByte(byte v) => new FbxProperty(FbxPropertyType.Bool, v);
        public static FbxProperty FromShort(short v) => new FbxProperty(FbxPropertyType.Short, v);
        public static FbxProperty FromInt(int v) => new FbxProperty(FbxPropertyType.Int, v);
        public static FbxProperty FromFloat(float v) => new FbxProperty(FbxPropertyType.Float, v);
        public static FbxProperty FromDouble(double v) => new FbxProperty(FbxPropertyType.Double, v);
        public static FbxProperty FromLong(long v) => new FbxProperty(FbxPropertyType.Long, v);
        public static FbxProperty FromString(string v) => new FbxProperty(FbxPropertyType.String, v);
        public static FbxProperty FromRaw(byte[] v) => new FbxProperty(FbxPropertyType.Raw, v);
        public static FbxProperty FromArray(bool[] v) => new FbxProperty(FbxPropertyType.BoolArray, v);
        public static FbxProperty FromArray(int[] v) => new FbxProperty(FbxPropertyType.IntArray, v);
        public static FbxProperty FromArray(long[] v) => new FbxProperty(FbxPropertyType.LongArray, v);
        public static FbxProperty FromArray(float[] v) => new FbxProperty(FbxPropertyType.FloatArray, v);
        public static FbxProperty FromArray(double[] v) => new FbxProperty(FbxPropertyType.DoubleArray, v);

        public bool IsArray => Type == FbxPropertyType.BoolArray || Type == FbxPropertyType.IntArray || Type == FbxPropertyType.LongArray
            || Type == FbxPropertyType.FloatArray || Type == FbxPropertyType.DoubleArray;

        public bool IsNumber => Type == FbxPropertyType.Bool || Type == FbxPropertyType.Short || Type == FbxPropertyType.Int
            || Type == FbxPropertyType.Float || Type == FbxPropertyType.Double || Type == FbxPropertyType.Long;

        public int ArrayLength => Value is Array a ? a.Length : 0;

        public double AsDouble(double defaultValue = 0.0)
        {
            switch (Type)
            {
                case FbxPropertyType.Bool: return (byte)Value;
                case FbxPropertyType.Short: return (short)Value;
                case FbxPropertyType.Int: return (int)Value;
                case FbxPropertyType.Float: return (float)Value;
                case FbxPropertyType.Double: return (double)Value;
                case FbxPropertyType.Long: return (long)Value;
                case FbxPropertyType.String:
                    return double.TryParse((string)Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : defaultValue;
                default: return defaultValue;
            }
        }

        public long AsLong(long defaultValue = 0)
        {
            switch (Type)
            {
                case FbxPropertyType.Bool: return (byte)Value;
                case FbxPropertyType.Short: return (short)Value;
                case FbxPropertyType.Int: return (int)Value;
                case FbxPropertyType.Float: return (long)(float)Value;
                case FbxPropertyType.Double: return (long)(double)Value;
                case FbxPropertyType.Long: return (long)Value;
                case FbxPropertyType.String:
                    return long.TryParse((string)Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : defaultValue;
                default: return defaultValue;
            }
        }

        public int AsInt(int defaultValue = 0) => (int)AsLong(defaultValue);

        public bool AsBool(bool defaultValue = false) => IsNumber ? AsDouble() != 0.0 : defaultValue;

        public string? AsString()
        {
            switch (Type)
            {
                case FbxPropertyType.String: return (string)Value;
                case FbxPropertyType.Raw: return System.Text.Encoding.UTF8.GetString((byte[])Value);
                default:
                    return IsNumber ? Convert.ToString(Value, CultureInfo.InvariantCulture) : null;
            }
        }

        public override string ToString()
        {
            if (IsArray)
                return $"{(char)Type}[{ArrayLength}]";
            if (Type == FbxPropertyType.Raw)
                return $"R[{((byte[])Value).Length}]";
            return $"{(char)Type}:{AsString()}";
        }
    }
}
