using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Reads and writes Autodesk FBX files.
    /// </summary>
    /// <remarks>
    /// Reading supports binary and ASCII files, FBX 7.x and legacy 6.x: meshes, materials, textures, node hierarchy, skins and animations.
    /// The reference for the format details is the ufbx library. Writing produces binary FBX 7.4.
    /// </remarks>
    public class FbxFileFormat : IFileFormat
    {
        private static readonly SupportedExtension[] _extensions = new[]
        {
            new SupportedExtension("Autodesk FBX", ".fbx"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        /// <summary>
        /// Files written by this format use Y up, Z front, centimeters, and V axis pointing up.
        /// </summary>
        public SpaceTransform FormatToGltfTransform => new SpaceTransform(SpaceTransform.XYZ, 0.01f, true);

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            content = new FileContainer();
            byte[] data;
            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;
                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    data = memory.ToArray();
                }
            }

            FbxDocument doc;
            try
            {
                doc = Parse(data);
            }
            catch (FormatException)
            {
                return false;
            }
            // FBX 5.x and older use a different layout that is not supported.
            if (doc.EffectiveVersion < 6000)
                return false;
            content = new FbxSceneReader(doc, entry).Read();
            return true;
        }

        /// <summary>
        /// Parses the raw node tree of a binary or ASCII FBX file.
        /// </summary>
        public static FbxDocument Parse(byte[] data)
        {
            if (FbxBinaryReader.HasBinaryHeader(data))
                return FbxBinaryReader.Parse(data);
            return FbxAsciiReader.Parse(new UTF8Encoding(false).GetString(data));
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            var doc = new FbxSceneWriter(content).Build();
            var bytes = FbxBinaryWriter.Write(doc);
            using (var stream = entry.OpenWrite())
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            return true;
        }
    }
}
