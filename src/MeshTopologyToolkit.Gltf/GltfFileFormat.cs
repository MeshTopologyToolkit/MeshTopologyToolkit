using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;


namespace MeshTopologyToolkit.Gltf
{
    public class GltfFileFormat : IFileFormat
    {
        static readonly SupportedExtension[] _extensions = new[] {  
            new SupportedExtension("glTF (Text)", ".gltf"),
            new SupportedExtension("glTF (Binary)", ".glb"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public bool TryRead(IFileSystemEntry entry, out FileContainer? content)
        {
            content = null;

            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;
                var fileName = entry.Name;
                var readContext = ReadContext
                    .Create(_=> ReadBytes((_ == fileName) ? entry.OpenRead():entry.GetNeigbourEntry(Uri.UnescapeDataString(_)).OpenRead()))
                    .WithSettingsFrom(new ReadSettings() { Validation = SharpGLTF.Validation.ValidationMode.TryFix });
                var model = readContext.ReadSchema2(fileName);

                content = new FileContainer();

                var visitor  = new GltfVisitor(content);
                visitor.Visit(model);

                return true;
            }
        }

        private ArraySegment<byte> ReadBytes(Stream? value)
        {
            if (value == null)
                return default;

            var buf = new MemoryStream();
            value.CopyTo(buf);
            value.Dispose();
            return buf.ToArray();
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            var model = new ModelBuilder().BuildModel(content);

            var isBinary = !entry.Name.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);
            using (var stream = entry.OpenWrite())
            {
                if (stream == null)
                    return false;
                var writeContext = WriteContext.Create((assetName, assetData) => WriteFile(entry, Uri.UnescapeDataString(assetName), assetData));
                writeContext.WithBinarySettings();
                writeContext.WriteBinarySchema2(entry.Name, model);
            }
            return true;
        }

        private void WriteFile(IFileSystemEntry entry, string assetName, ArraySegment<byte> assetData)
        {
            if (entry.Name != assetName)
            {
                entry = entry.GetNeigbourEntry(assetName);
            }
            using (var stream = entry.OpenWrite())
            {
                if (stream == null)
                    throw new IOException($"Could not open file {assetName} for writing.");
                stream.Write(assetData.Array!, assetData.Offset, assetData.Count);
            }
        }
    }
}

