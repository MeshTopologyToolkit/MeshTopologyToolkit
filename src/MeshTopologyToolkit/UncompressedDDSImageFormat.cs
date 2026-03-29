using System;
using System.Collections.Generic;
using System.IO;

namespace MeshTopologyToolkit
{
    public class UncompressedDDSImageFormat : IImageFormat
    {
        private const uint DdsMagic = 0x20534444;
        private const uint FourCcDx10 = 0x30315844;
        private const uint DdpfAlphaPixels = 0x00000001;
        private const uint DdpfFourCc = 0x00000004;
        private const uint DdpfRgb = 0x00000040;
        private const uint DdsdCaps = 0x00000001;
        private const uint DdsdHeight = 0x00000002;
        private const uint DdsdWidth = 0x00000004;
        private const uint DdsdPitch = 0x00000008;
        private const uint DdsdDepth = 0x00800000;
        private const uint DdsdMipMapCount = 0x00020000;
        private const uint DdsCapsTexture = 0x00001000;
        private const uint DdsCapsComplex = 0x00000008;
        private const uint DdsCapsMipMap = 0x00400000;
        private const uint DxgiFormatR8G8B8A8Unorm = 28;
        private const uint DxgiFormatB8G8R8A8Unorm = 87;
        private const uint DxgiFormatB8G8R8X8Unorm = 88;

        private readonly static IReadOnlyList<SupportedExtension> _supportedExtensions = new[] { new SupportedExtension("Direct Draw Surface", ".dds") };
        private readonly bool _preferDx10;

        public UncompressedDDSImageFormat(bool preferDx10 = false)
        {
            this._preferDx10 = preferDx10;
        }

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _supportedExtensions;

        public bool TryRead(IFileSystemEntry entry, out ImageContainer image)
        {
            using var stream = entry.OpenRead();
            if (stream == null)
            {
                throw new Exception($"Failed to open stream for entry: {entry?.Name}");
            }

            using var reader = new BinaryReader(stream);

            if (!TryReadHeader(reader, out var header, out var dxt10Header))
            {
                image = new ImageContainer();
                return false;
            }

            if (!TryGetReadablePixelFormat(header.PixelFormat, dxt10Header, out var pixelFormat))
            {
                image = new ImageContainer();
                return false;
            }

            if (!TryGetBytesPerPixel(pixelFormat, out var bytesPerPixel))
            {
                image = new ImageContainer();
                return false;
            }

            int width = checked((int)header.Width);
            int height = checked((int)header.Height);
            int depth = (header.Flags & DdsdDepth) != 0 && header.Depth > 0 ? checked((int)header.Depth) : 1;
            int mipCount = (header.Flags & DdsdMipMapCount) != 0 && header.MipMapCount > 0 ? checked((int)header.MipMapCount) : 1;

            var mipMaps = new IImageMipMap[mipCount];
            int mipWidth = Math.Max(1, width);
            int mipHeight = Math.Max(1, height);
            int mipDepth = Math.Max(1, depth);

            for (int mipIndex = 0; mipIndex < mipCount; mipIndex++)
            {
                var pixelCount = checked(mipWidth * mipHeight * mipDepth);
                var pixels = new Color32[pixelCount];
                for (int i = 0; i < pixelCount; i++)
                {
                    pixels[i] = ReadPixel(reader, pixelFormat, bytesPerPixel);
                }

                mipMaps[mipIndex] = new LDRImageMipMap(mipWidth, mipHeight, mipDepth, pixels);
                mipWidth = Math.Max(1, mipWidth >> 1);
                mipHeight = Math.Max(1, mipHeight >> 1);
                mipDepth = Math.Max(1, mipDepth >> 1);
            }

            image = new ImageContainer(mipMaps);
            return true;
        }

        public bool TryWrite(IFileSystemEntry entry, ImageContainer content)
        {
            if (content == null || content.Count == 0)
            {
                return false;
            }

            var baseMip = content[0];
            int width = baseMip.Width;
            int height = baseMip.Height;
            int depth = baseMip.Depth;

            if (width <= 0 || height <= 0 || depth <= 0)
            {
                return false;
            }

            ValidateMipMaps(content, width, height, depth);

            using var stream = entry.OpenWrite();
            using var writer = new BinaryWriter(stream);

            WriteHeader(writer, width, height, depth, content.Count, _preferDx10);

            for (int mipIndex = 0; mipIndex < content.Count; mipIndex++)
            {
                WritePixels(writer, content[mipIndex].GetPixels());
            }

            return true;
        }

        private static bool TryReadHeader(BinaryReader reader, out DDS_HEADER header, out DDS_HEADER_DXT10? dxt10Header)
        {
            header = default;
            dxt10Header = null;

            if (reader.BaseStream.Length - reader.BaseStream.Position < 4 + 124)
            {
                return false;
            }

            if (reader.ReadUInt32() != DdsMagic)
            {
                return false;
            }

            header = ReadHeader(reader);
            if (header.Size != 124 || header.PixelFormat.Size != 32)
            {
                return false;
            }

            if ((header.PixelFormat.Flags & DdpfFourCc) != 0 && header.PixelFormat.FourCC == FourCcDx10)
            {
                if (reader.BaseStream.Length - reader.BaseStream.Position < 20)
                {
                    return false;
                }

                dxt10Header = ReadHeaderDxt10(reader);
            }

            return true;
        }

        private static DDS_HEADER ReadHeader(BinaryReader reader)
        {
            return new DDS_HEADER
            {
                Size = reader.ReadUInt32(),
                Flags = reader.ReadUInt32(),
                Height = reader.ReadUInt32(),
                Width = reader.ReadUInt32(),
                PitchOrLinearSize = reader.ReadUInt32(),
                Depth = reader.ReadUInt32(),
                MipMapCount = reader.ReadUInt32(),
                Reserved1 = new uint[]
                {
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                    reader.ReadUInt32(),
                },
                PixelFormat = ReadPixelFormat(reader),
                Caps = reader.ReadUInt32(),
                Caps2 = reader.ReadUInt32(),
                Caps3 = reader.ReadUInt32(),
                Caps4 = reader.ReadUInt32(),
                Reserved2 = reader.ReadUInt32(),
            };
        }

        private static void WriteHeader(BinaryWriter writer, int width, int height, int depth, int mipCount, bool preferDx10)
        {
            uint flags = DdsdCaps | DdsdHeight | DdsdWidth | DdsdPitch;
            uint caps = DdsCapsTexture;

            if (depth > 1)
            {
                flags |= DdsdDepth;
            }

            if (mipCount > 1)
            {
                flags |= DdsdMipMapCount;
                caps |= DdsCapsComplex | DdsCapsMipMap;
            }

            writer.Write(DdsMagic);
            writer.Write(124u);
            writer.Write(flags);
            writer.Write((uint)height);
            writer.Write((uint)width);
            writer.Write((uint)(width * 4));
            writer.Write((uint)(depth > 1 ? depth : 0));
            writer.Write((uint)(mipCount > 1 ? mipCount : 0));

            for (int i = 0; i < 11; i++)
            {
                writer.Write(0u);
            }

            WritePixelFormat(writer, preferDx10
                ? new DDS_PIXELFORMAT
                {
                    Size = 32,
                    Flags = DdpfFourCc,
                    FourCC = FourCcDx10,
                }
                : new DDS_PIXELFORMAT
                {
                    Size = 32,
                    Flags = DdpfRgb | DdpfAlphaPixels,
                    RgbBitCount = 32,
                    RBitMask = 0x000000FF,
                    GBitMask = 0x0000FF00,
                    BBitMask = 0x00FF0000,
                    ABitMask = 0xFF000000,
                });

            writer.Write(caps);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);

            if (preferDx10)
            {
                WriteHeaderDxt10(writer, new DDS_HEADER_DXT10
                {
                    DxgiFormat = DxgiFormatR8G8B8A8Unorm,
                    ResourceDimension = 3,
                    ArraySize = 1,
                });
            }
        }

        private static void WritePixelFormat(BinaryWriter writer, DDS_PIXELFORMAT pixelFormat)
        {
            writer.Write(pixelFormat.Size);
            writer.Write(pixelFormat.Flags);
            writer.Write(pixelFormat.FourCC);
            writer.Write(pixelFormat.RgbBitCount);
            writer.Write(pixelFormat.RBitMask);
            writer.Write(pixelFormat.GBitMask);
            writer.Write(pixelFormat.BBitMask);
            writer.Write(pixelFormat.ABitMask);
        }

        private static void WriteHeaderDxt10(BinaryWriter writer, DDS_HEADER_DXT10 header)
        {
            writer.Write(header.DxgiFormat);
            writer.Write(header.ResourceDimension);
            writer.Write(header.MiscFlag);
            writer.Write(header.ArraySize);
            writer.Write(header.MiscFlags2);
        }

        private static void ValidateMipMaps(ImageContainer content, int width, int height, int depth)
        {
            int expectedWidth = width;
            int expectedHeight = height;
            int expectedDepth = depth;

            for (int mipIndex = 0; mipIndex < content.Count; mipIndex++)
            {
                var mip = content[mipIndex];
                if (mip.Width != expectedWidth || mip.Height != expectedHeight || mip.Depth != expectedDepth)
                {
                    throw new InvalidOperationException($"Mip {mipIndex} dimensions do not match the expected DDS mip chain.");
                }

                if (mip.GetPixels().Count != expectedWidth * expectedHeight * expectedDepth)
                {
                    throw new InvalidOperationException($"Mip {mipIndex} pixel count does not match its dimensions.");
                }

                expectedWidth = Math.Max(1, expectedWidth >> 1);
                expectedHeight = Math.Max(1, expectedHeight >> 1);
                expectedDepth = Math.Max(1, expectedDepth >> 1);
            }
        }

        private static void WritePixels(BinaryWriter writer, IReadOnlyList<Color32> pixels)
        {
            for (int i = 0; i < pixels.Count; i++)
            {
                var pixel = pixels[i];
                writer.Write(pixel.R);
                writer.Write(pixel.G);
                writer.Write(pixel.B);
                writer.Write(pixel.A);
            }
        }

        private static DDS_PIXELFORMAT ReadPixelFormat(BinaryReader reader)
        {
            return new DDS_PIXELFORMAT
            {
                Size = reader.ReadUInt32(),
                Flags = reader.ReadUInt32(),
                FourCC = reader.ReadUInt32(),
                RgbBitCount = reader.ReadUInt32(),
                RBitMask = reader.ReadUInt32(),
                GBitMask = reader.ReadUInt32(),
                BBitMask = reader.ReadUInt32(),
                ABitMask = reader.ReadUInt32(),
            };
        }

        private static DDS_HEADER_DXT10 ReadHeaderDxt10(BinaryReader reader)
        {
            return new DDS_HEADER_DXT10
            {
                DxgiFormat = reader.ReadUInt32(),
                ResourceDimension = reader.ReadUInt32(),
                MiscFlag = reader.ReadUInt32(),
                ArraySize = reader.ReadUInt32(),
                MiscFlags2 = reader.ReadUInt32(),
            };
        }

        private static bool TryGetReadablePixelFormat(DDS_PIXELFORMAT pixelFormat, DDS_HEADER_DXT10? dxt10Header, out DDS_PIXELFORMAT readablePixelFormat)
        {
            readablePixelFormat = pixelFormat;

            if ((pixelFormat.Flags & DdpfFourCc) != 0)
            {
                if (pixelFormat.FourCC != FourCcDx10 || !dxt10Header.HasValue)
                {
                    return false;
                }

                switch (dxt10Header.Value.DxgiFormat)
                {
                    case DxgiFormatR8G8B8A8Unorm:
                        readablePixelFormat = new DDS_PIXELFORMAT
                        {
                            Size = 32,
                            Flags = DdpfRgb | DdpfAlphaPixels,
                            RgbBitCount = 32,
                            RBitMask = 0x000000FF,
                            GBitMask = 0x0000FF00,
                            BBitMask = 0x00FF0000,
                            ABitMask = 0xFF000000,
                        };
                        return true;
                    case DxgiFormatB8G8R8A8Unorm:
                        readablePixelFormat = new DDS_PIXELFORMAT
                        {
                            Size = 32,
                            Flags = DdpfRgb | DdpfAlphaPixels,
                            RgbBitCount = 32,
                            RBitMask = 0x00FF0000,
                            GBitMask = 0x0000FF00,
                            BBitMask = 0x000000FF,
                            ABitMask = 0xFF000000,
                        };
                        return true;
                    case DxgiFormatB8G8R8X8Unorm:
                        readablePixelFormat = new DDS_PIXELFORMAT
                        {
                            Size = 32,
                            Flags = DdpfRgb,
                            RgbBitCount = 32,
                            RBitMask = 0x00FF0000,
                            GBitMask = 0x0000FF00,
                            BBitMask = 0x000000FF,
                            ABitMask = 0,
                        };
                        return true;
                    default:
                        return false;
                }
            }

            return (pixelFormat.Flags & DdpfRgb) != 0;
        }

        private static bool TryGetBytesPerPixel(DDS_PIXELFORMAT pixelFormat, out int bytesPerPixel)
        {
            bytesPerPixel = 0;
            if ((pixelFormat.Flags & DdpfRgb) == 0)
            {
                return false;
            }

            if (pixelFormat.RgbBitCount != 24 && pixelFormat.RgbBitCount != 32)
            {
                return false;
            }

            bytesPerPixel = checked((int)(pixelFormat.RgbBitCount / 8));
            return true;
        }

        private static Color32 ReadPixel(BinaryReader reader, DDS_PIXELFORMAT pixelFormat, int bytesPerPixel)
        {
            uint pixel = bytesPerPixel switch
            {
                3 => (uint)(reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16)),
                4 => reader.ReadUInt32(),
                _ => throw new NotSupportedException($"Unsupported pixel size: {bytesPerPixel}"),
            };

            return new Color32(
                ExtractComponent(pixel, pixelFormat.RBitMask, 0),
                ExtractComponent(pixel, pixelFormat.GBitMask, 0),
                ExtractComponent(pixel, pixelFormat.BBitMask, 0),
                ExtractComponent(pixel, pixelFormat.ABitMask, 255));
        }

        private static byte ExtractComponent(uint pixel, uint mask, byte defaultValue)
        {
            if (mask == 0)
            {
                return defaultValue;
            }

            int shift = 0;
            uint shiftedMask = mask;
            while ((shiftedMask & 1) == 0)
            {
                shiftedMask >>= 1;
                shift++;
            }

            uint value = (pixel & mask) >> shift;
            uint maxValue = shiftedMask;
            return (byte)((value * 255u + (maxValue / 2u)) / maxValue);
        }

        private struct DDS_HEADER
        {
            public uint Size { get; set; }
            public uint Flags { get; set; }
            public uint Height { get; set; }
            public uint Width { get; set; }
            public uint PitchOrLinearSize { get; set; }
            public uint Depth { get; set; }
            public uint MipMapCount { get; set; }
            public uint[] Reserved1 { get; set; }
            public DDS_PIXELFORMAT PixelFormat { get; set; }
            public uint Caps { get; set; }
            public uint Caps2 { get; set; }
            public uint Caps3 { get; set; }
            public uint Caps4 { get; set; }
            public uint Reserved2 { get; set; }
        }

        private struct DDS_PIXELFORMAT
        {
            public uint Size { get; set; }
            public uint Flags { get; set; }
            public uint FourCC { get; set; }
            public uint RgbBitCount { get; set; }
            public uint RBitMask { get; set; }
            public uint GBitMask { get; set; }
            public uint BBitMask { get; set; }
            public uint ABitMask { get; set; }
        }

        private struct DDS_HEADER_DXT10
        {
            public uint DxgiFormat { get; set; }
            public uint ResourceDimension { get; set; }
            public uint MiscFlag { get; set; }
            public uint ArraySize { get; set; }
            public uint MiscFlags2 { get; set; }
        }
    }
}
