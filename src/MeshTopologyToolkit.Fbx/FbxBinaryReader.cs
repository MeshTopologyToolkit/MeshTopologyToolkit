using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Parses binary FBX files (versions 3000 - 7700) into an <see cref="FbxDocument"/>.
    /// </summary>
    public static class FbxBinaryReader
    {
        private const string Magic = "Kaydara FBX Binary  \0";
        private const int HeaderSize = 27;

        public static bool HasBinaryHeader(byte[] data)
        {
            if (data.Length < HeaderSize)
                return false;
            for (int i = 0; i < Magic.Length; ++i)
            {
                if (data[i] != (byte)Magic[i])
                    return false;
            }
            return true;
        }

        public static FbxDocument Parse(byte[] data)
        {
            if (!HasBinaryHeader(data))
                throw new FormatException("Not a binary FBX file.");

            var doc = new FbxDocument { IsBinary = true };
            doc.Version = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(23, 4));
            bool wide = doc.Version >= 7500;

            var reader = new Reader(data, wide);
            int pos = HeaderSize;
            while (true)
            {
                var node = reader.ReadNode(ref pos, 0);
                if (node == null)
                    break;
                doc.Roots.Add(node);
            }
            return doc;
        }

        private sealed class Reader
        {
            private readonly byte[] _data;
            private readonly bool _wide;
            private readonly int _recordHeaderSize;

            public Reader(byte[] data, bool wide)
            {
                _data = data;
                _wide = wide;
                _recordHeaderSize = wide ? 25 : 13;
            }

            private long ReadOffset(int pos)
            {
                return _wide
                    ? (long)BinaryPrimitives.ReadUInt64LittleEndian(_data.AsSpan(pos, 8))
                    : BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(pos, 4));
            }

            public FbxNode? ReadNode(ref int pos, int depth)
            {
                if (depth > 512)
                    throw new FormatException("FBX node nesting is too deep.");
                if (pos + _recordHeaderSize > _data.Length)
                    return null;

                int fieldSize = _wide ? 8 : 4;
                long endOffset = ReadOffset(pos);
                long numProperties = ReadOffset(pos + fieldSize);
                long propertyListLength = ReadOffset(pos + fieldSize * 2);
                int nameLength = _data[pos + fieldSize * 3];
                if (endOffset == 0)
                {
                    // Null record terminates the list of nodes.
                    pos += _recordHeaderSize;
                    return null;
                }
                if (endOffset > _data.Length || endOffset < pos + _recordHeaderSize)
                    throw new FormatException("Invalid FBX node end offset.");

                // The name starts right after the name length byte.
                int p = pos + fieldSize * 3 + 1;
                var node = new FbxNode(Encoding.UTF8.GetString(_data, p, nameLength));
                p += nameLength;

                int propsEnd = p + (int)propertyListLength;
                if (propsEnd > endOffset)
                    throw new FormatException("Invalid FBX property list length.");
                for (long i = 0; i < numProperties; ++i)
                    node.Properties.Add(ReadProperty(ref p, propsEnd));
                p = propsEnd;

                // Children follow until the end offset, the last one is a null record.
                while (p < endOffset)
                {
                    var child = ReadNode(ref p, depth + 1);
                    if (child == null)
                        break;
                    node.Children.Add(child);
                }
                pos = (int)endOffset;
                return node;
            }

            private FbxProperty ReadProperty(ref int p, int limit)
            {
                if (p >= limit)
                    throw new FormatException("Unexpected end of FBX property list.");
                char type = (char)_data[p++];
                switch (type)
                {
                    case 'C': return FbxProperty.FromByte(_data[p++]);
                    case 'Y': { var v = BinaryPrimitives.ReadInt16LittleEndian(_data.AsSpan(p, 2)); p += 2; return FbxProperty.FromShort(v); }
                    case 'I': { var v = BinaryPrimitives.ReadInt32LittleEndian(_data.AsSpan(p, 4)); p += 4; return FbxProperty.FromInt(v); }
                    case 'F': { var v = BitConverter.ToSingle(_data, p); p += 4; return FbxProperty.FromFloat(v); }
                    case 'D': { var v = BitConverter.ToDouble(_data, p); p += 8; return FbxProperty.FromDouble(v); }
                    case 'L': { var v = BinaryPrimitives.ReadInt64LittleEndian(_data.AsSpan(p, 8)); p += 8; return FbxProperty.FromLong(v); }
                    case 'S':
                        {
                            int len = (int)BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(p, 4)); p += 4;
                            CheckLength(p, len, limit);
                            var s = Encoding.UTF8.GetString(_data, p, len); p += len;
                            return FbxProperty.FromString(s);
                        }
                    case 'R':
                        {
                            int len = (int)BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(p, 4)); p += 4;
                            CheckLength(p, len, limit);
                            var raw = new byte[len];
                            Buffer.BlockCopy(_data, p, raw, 0, len); p += len;
                            return FbxProperty.FromRaw(raw);
                        }
                    case 'b': return FbxProperty.FromArray(ReadArray<bool>(ref p, limit, 1));
                    case 'i': return FbxProperty.FromArray(ReadArray<int>(ref p, limit, 4));
                    case 'l': return FbxProperty.FromArray(ReadArray<long>(ref p, limit, 8));
                    case 'f': return FbxProperty.FromArray(ReadArray<float>(ref p, limit, 4));
                    case 'd': return FbxProperty.FromArray(ReadArray<double>(ref p, limit, 8));
                    default:
                        throw new FormatException($"Unknown FBX property type '{type}'.");
                }
            }

            private static void CheckLength(int p, int len, int limit)
            {
                if (len < 0 || p + len > limit)
                    throw new FormatException("FBX property exceeds the property list.");
            }

            private T[] ReadArray<T>(ref int p, int limit, int elementSize) where T : struct
            {
                int count = (int)BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(p, 4));
                int encoding = (int)BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(p + 4, 4));
                int encodedLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(p + 8, 4));
                p += 12;
                CheckLength(p, encodedLength, limit);
                long byteCount = (long)count * elementSize;
                if (count < 0 || byteCount > int.MaxValue)
                    throw new FormatException("FBX array is too large.");

                byte[] bytes;
                if (encoding == 0)
                {
                    if (encodedLength != byteCount)
                        throw new FormatException("FBX array length mismatch.");
                    bytes = new byte[byteCount];
                    Buffer.BlockCopy(_data, p, bytes, 0, (int)byteCount);
                }
                else if (encoding == 1)
                {
                    bytes = Inflate(_data, p, encodedLength, (int)byteCount);
                }
                else
                {
                    throw new FormatException($"Unknown FBX array encoding {encoding}.");
                }
                p += encodedLength;

                var result = new T[count];
                if (typeof(T) == typeof(bool))
                {
                    var r = (bool[])(object)result;
                    for (int i = 0; i < r.Length; ++i)
                        r[i] = (bytes[i] & 1) != 0;
                }
                else
                {
                    Buffer.BlockCopy(bytes, 0, result, 0, (int)byteCount);
                    if (!BitConverter.IsLittleEndian)
                        SwapEndian(result, elementSize);
                }
                return result;
            }

            private static void SwapEndian(Array array, int elementSize)
            {
                var bytes = new byte[array.Length * elementSize];
                Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
                for (int i = 0; i < bytes.Length; i += elementSize)
                    Array.Reverse(bytes, i, elementSize);
                Buffer.BlockCopy(bytes, 0, array, 0, bytes.Length);
            }

            private static byte[] Inflate(byte[] data, int offset, int length, int expectedSize)
            {
                var result = new byte[expectedSize];
                // The data is a zlib stream: 2 bytes header, deflate data, 4 bytes Adler-32.
                if (length < 2)
                    throw new FormatException("Invalid compressed FBX array.");
                using (var input = new MemoryStream(data, offset + 2, length - 2, false))
                using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
                {
                    int read = 0;
                    while (read < expectedSize)
                    {
                        int n = deflate.Read(result, read, expectedSize - read);
                        if (n <= 0)
                            throw new FormatException("Compressed FBX array is shorter than expected.");
                        read += n;
                    }
                }
                return result;
            }
        }
    }
}
