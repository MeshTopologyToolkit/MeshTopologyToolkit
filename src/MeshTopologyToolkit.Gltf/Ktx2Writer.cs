using System;
using System.IO;

namespace MeshTopologyToolkit.Gltf
{
    public static class Ktx2Writer
    {
        // VK_FORMAT_R8G8B8A8_UNORM numeric value (as in many Vulkan headers) -- confirm for your Vulkan headers if you need exact enum.
        // Verified in Vulkan reference pages; value commonly used: 37. If you wish, you may expose vkFormat as a parameter.
        private const uint VK_FORMAT_R8G8B8A8_UNORM = 37u;

        // KTX2 file identifier (12 bytes)
        private static readonly byte[] Ktx2Identifier = new byte[] {
            0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A
        };

        /// <summary>
        /// Write a very simple KTX2 file for an uncompressed RGBA8 texture with mipmaps.
        /// Limitations: does not write a full Data Format Descriptor (dfd) or KVD/SGD blocks. Some loaders require a DFD and will reject the file.
        /// </summary>
        /// <param name="outputStream">Output .ktx2 stream</param>
        /// <param name="baseWidth">base level width</param>
        /// <param name="baseHeight">base level height</param>
        /// <param name="mipBuffers">array of mip buffers ordered LOD0..LODn-1, each buffer is raw RGBA8 (width*height*4 bytes)</param>
        public static void WriteKtx2_SimpleUncompressed(Stream outputStream, int baseWidth, int baseHeight, byte[][] mipBuffers)
        {
            long streamStartPosition = outputStream.Position;

            if (mipBuffers == null || mipBuffers.Length == 0) throw new ArgumentException("Provide at least one mip level.");
            int levelCount = mipBuffers.Length;

            // Basic checks (sizes)
            int w = baseWidth, h = baseHeight;
            for (int i = 0; i < levelCount; i++)
            {
                long expected = (long)Math.Max(1, w) * Math.Max(1, h) * 4L;
                if (mipBuffers[i] == null || mipBuffers[i].LongLength != expected)
                    throw new ArgumentException($"mipBuffers[{i}] size mismatch: expected {expected} bytes for {w}x{h} RGBA8 but got {mipBuffers[i]?.LongLength ?? 0}.");
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
            }

            using var bw = new BinaryWriter(outputStream);

            // 1) identifier (12 bytes)
            bw.Write(Ktx2Identifier);

            // 2) header fields (all 32-bit little endian)
            // Fields according to KTX2 spec (after identifier):
            // UInt32 vkFormat
            // UInt32 typeSize
            // UInt32 pixelWidth
            // UInt32 pixelHeight
            // UInt32 pixelDepth
            // UInt32 layerCount
            // UInt32 faceCount
            // UInt32 levelCount
            // UInt32 supercompressionScheme
            // Then index: (dfdByteOffset, dfdByteLength, kvdByteOffset, kvdByteLength, sgdByteOffset, sgdByteLength)
            // Then levelIndex entries (levelCount entries): struct { UInt64 byteOffset; UInt64 byteLength; UInt64 uncompressedByteLength; }
            // See: KTX2 spec for details.
            // We'll write a minimal header: vkFormat set to VK_FORMAT_R8G8B8A8_UNORM, typeSize=1, pixelWidth/Height set.
            // We'll set pixelDepth=0, layerCount=0, faceCount=1 (single 2D image), levelCount=levelCount, supercompressionScheme=0 (none).
            // We'll set dfd/kvd/sgd offsets & lengths to 0 (no DFD written) — note: some loaders require a DFD; files created by this function may be rejected by stricter validators.
            bw.Write((uint)VK_FORMAT_R8G8B8A8_UNORM); // vkFormat
            bw.Write((uint)1);                      // typeSize (bytes per component)
            bw.Write((uint)baseWidth);              // pixelWidth
            bw.Write((uint)baseHeight);             // pixelHeight
            bw.Write((uint)0);                      // pixelDepth (0 for 2D)
            bw.Write((uint)0);                      // layerCount (0 means not an array)
            bw.Write((uint)1);                      // faceCount (1 for 2D; 6 for cubemap)
            bw.Write((uint)levelCount);             // levelCount
            bw.Write((uint)0);                      // supercompressionScheme: 0 = none

            // Index fields: dfdByteOffset, dfdByteLength, kvdByteOffset, kvdByteLength, sgdByteOffset, sgdByteLength
            // We'll set all offsets/lengths to 0 (no DFD, no KVD, no SGD).
            // NOTE: Spec allows dfdByteOffset = 0 only when dfdByteLength = 0. Many loaders expect a DFD when vkFormat=VK_FORMAT_UNDEFINED or to validate shapes.
            bw.Write((ulong)0); // dfdByteOffset
            bw.Write((uint)0);  // dfdByteLength (must be 0 to match offset=0)
            bw.Write((ulong)0); // kvdByteOffset
            bw.Write((uint)0);  // kvdByteLength
            bw.Write((ulong)0); // sgdByteOffset
            bw.Write((uint)0);  // sgdByteLength

            // Now write the level index entries. Each level entry in KTX2 levelIndex is:
            // UInt64 byteOffset; UInt64 byteLength; UInt64 uncompressedByteLength;
            // byteOffset is from start of file to first byte of that level's image data (after any mipPadding)
            // We must compute offsets; current stream position is after header + index placeholders + level index table; we'll compute the offset base and then write index entries then image data.
            long headerEnd = outputStream.Position - streamStartPosition;
            // levelIndex table size:
            long levelIndexSize = (long)levelCount * (8 + 8 + 8); // three UInt64s per level

            // We'll prepare level offsets: first data will be placed immediately after header + levelIndex table.
            long dataStart = headerEnd + levelIndexSize;

            // However KTX2 requires each level's data to be 4-byte aligned (mipPadding). We'll align each level start to 4 bytes. (Spec says rows/blocks aligned based on format; for uncompressed RGBA8, we align to 4).
            // Build level entries
            var levelEntries = new (ulong byteOffset, ulong byteLength, ulong uncompressedByteLength)[levelCount];
            long nextOffset = dataStart;
            for (int i = 0; i < levelCount; i++)
            {
                // Align start to 4
                long aligned = (nextOffset + 3) & ~3L;
                nextOffset = aligned;

                ulong byteLen = (ulong)mipBuffers[i].LongLength;
                levelEntries[i] = ((ulong)nextOffset, byteLen, byteLen); // uncompressed==byteLen when no supercompression
                nextOffset = (long)((ulong)nextOffset + byteLen);
            }

            // Write level index table
            for (int i = 0; i < levelCount; i++)
            {
                bw.Write(levelEntries[i].byteOffset);
                bw.Write(levelEntries[i].byteLength);
                bw.Write(levelEntries[i].uncompressedByteLength);
            }

            // Now write image data per level, taking care to add mipPadding so that each level's first byte begins at the recorded offset.
            for (int i = 0; i < levelCount; i++)
            {
                long desired = (long)levelEntries[i].byteOffset;
                long cur = outputStream.Position - streamStartPosition;
                if (cur > desired)
                    throw new InvalidOperationException("calculated offsets are inconsistent (internal error).");

                // padding to desired offset
                while (outputStream.Position < desired) bw.Write((byte)0);

                // write level bytes
                bw.Write(mipBuffers[i]);
            }

            // Done
            bw.Flush();
        }
    }
}


