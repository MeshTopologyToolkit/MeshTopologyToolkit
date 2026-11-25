using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Serializes an <see cref="FbxDocument"/> as a binary FBX file (version 7.4, 32-bit node offsets).
    /// </summary>
    public static class FbxBinaryWriter
    {
        private static readonly byte[] FooterId = { 0xfa, 0xbc, 0xab, 0x09, 0xd0, 0xc8, 0xd4, 0x66, 0xb1, 0x76, 0xfb, 0x83, 0x1c, 0xf7, 0x26, 0x7e };
        private static readonly byte[] FooterMagic = { 0xf8, 0x5a, 0x8c, 0x6a, 0xde, 0xf5, 0xd9, 0x7e, 0xec, 0xe9, 0x0c, 0xe3, 0x75, 0x8f, 0x29, 0x0b };

        /// <summary>Arrays smaller than this are stored uncompressed.</summary>
        private const int CompressionThreshold = 128;

        public static byte[] Write(FbxDocument doc)
        {
            const int version = 7400;
            using (var memory = new MemoryStream())
            using (var w = new BinaryWriter(memory, Encoding.UTF8, true))
            {
                w.Write(Encoding.ASCII.GetBytes("Kaydara FBX Binary  \0"));
                w.Write((byte)0x1A);
                w.Write((byte)0x00);
                w.Write((uint)version);

                foreach (var node in doc.Roots)
                    WriteNode(w, node);
                w.Write(new byte[13]); // null record

                w.Write(FooterId);
                w.Write(new byte[4]);
                long offset = memory.Position;
                int padding = (int)(((offset + 15) & ~15L) - offset);
                if (padding == 0)
                    padding = 16;
                w.Write(new byte[padding]);
                w.Write((uint)version);
                w.Write(new byte[120]);
                w.Write(FooterMagic);
                w.Flush();
                return memory.ToArray();
            }
        }

        private static void WriteNode(BinaryWriter w, FbxNode node)
        {
            long start = w.BaseStream.Position;
            var name = Encoding.UTF8.GetBytes(node.Name);
            if (name.Length > 255)
                throw new InvalidOperationException($"FBX node name is too long: {node.Name}");

            w.Write(0u); // end offset, patched below
            w.Write((uint)node.Properties.Count);
            w.Write(0u); // property list length, patched below
            w.Write((byte)name.Length);
            w.Write(name);

            long propertiesStart = w.BaseStream.Position;
            foreach (var p in node.Properties)
                WriteProperty(w, p);
            long propertiesEnd = w.BaseStream.Position;

            if (node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                    WriteNode(w, child);
                w.Write(new byte[13]); // null record terminates the children
            }

            long end = w.BaseStream.Position;
            if (end > uint.MaxValue)
                throw new InvalidOperationException("The scene is too large for FBX 7.4 (4 GB limit).");
            w.Flush();
            w.BaseStream.Position = start;
            w.Write((uint)end);
            w.BaseStream.Position = start + 8;
            w.Write((uint)(propertiesEnd - propertiesStart));
            w.BaseStream.Position = end;
        }

        private static void WriteProperty(BinaryWriter w, FbxProperty p)
        {
            w.Write((byte)p.Type);
            switch (p.Type)
            {
                case FbxPropertyType.Bool: w.Write((byte)p.Value); break;
                case FbxPropertyType.Short: w.Write((short)p.Value); break;
                case FbxPropertyType.Int: w.Write((int)p.Value); break;
                case FbxPropertyType.Float: w.Write((float)p.Value); break;
                case FbxPropertyType.Double: w.Write((double)p.Value); break;
                case FbxPropertyType.Long: w.Write((long)p.Value); break;
                case FbxPropertyType.String:
                    {
                        var bytes = Encoding.UTF8.GetBytes((string)p.Value);
                        w.Write((uint)bytes.Length);
                        w.Write(bytes);
                        break;
                    }
                case FbxPropertyType.Raw:
                    {
                        var bytes = (byte[])p.Value;
                        w.Write((uint)bytes.Length);
                        w.Write(bytes);
                        break;
                    }
                case FbxPropertyType.BoolArray:
                    {
                        var values = (bool[])p.Value;
                        var bytes = new byte[values.Length];
                        for (int i = 0; i < bytes.Length; ++i)
                            bytes[i] = (byte)(values[i] ? 1 : 0);
                        WriteArray(w, values.Length, bytes);
                        break;
                    }
                case FbxPropertyType.IntArray: WriteArray(w, (Array)p.Value, 4); break;
                case FbxPropertyType.LongArray: WriteArray(w, (Array)p.Value, 8); break;
                case FbxPropertyType.FloatArray: WriteArray(w, (Array)p.Value, 4); break;
                case FbxPropertyType.DoubleArray: WriteArray(w, (Array)p.Value, 8); break;
                default:
                    throw new InvalidOperationException($"Unknown FBX property type {p.Type}.");
            }
        }

        private static void WriteArray(BinaryWriter w, Array values, int elementSize)
        {
            var bytes = new byte[values.Length * elementSize];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            if (!BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < bytes.Length; i += elementSize)
                    Array.Reverse(bytes, i, elementSize);
            }
            WriteArray(w, values.Length, bytes);
        }

        private static void WriteArray(BinaryWriter w, int count, byte[] bytes)
        {
            w.Write((uint)count);
            if (bytes.Length >= CompressionThreshold)
            {
                var compressed = Deflate(bytes);
                if (compressed.Length < bytes.Length)
                {
                    w.Write(1u);
                    w.Write((uint)compressed.Length);
                    w.Write(compressed);
                    return;
                }
            }
            w.Write(0u);
            w.Write((uint)bytes.Length);
            w.Write(bytes);
        }

        /// <summary>Produces a zlib stream: header, deflate data, Adler-32 checksum.</summary>
        private static byte[] Deflate(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                output.WriteByte(0x78);
                output.WriteByte(0x9C);
                using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
                {
                    deflate.Write(data, 0, data.Length);
                }
                uint adler = Adler32(data);
                output.WriteByte((byte)(adler >> 24));
                output.WriteByte((byte)(adler >> 16));
                output.WriteByte((byte)(adler >> 8));
                output.WriteByte((byte)adler);
                return output.ToArray();
            }
        }

        private static uint Adler32(byte[] data)
        {
            const uint Mod = 65521;
            uint a = 1, b = 0;
            int i = 0;
            while (i < data.Length)
            {
                int n = Math.Min(5552, data.Length - i);
                for (int k = 0; k < n; ++k)
                {
                    a += data[i + k];
                    b += a;
                }
                a %= Mod;
                b %= Mod;
                i += n;
            }
            return (b << 16) | a;
        }
    }
}
