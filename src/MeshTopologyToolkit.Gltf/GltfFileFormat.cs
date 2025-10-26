using SharpGLTF.Schema2;
using System;
using System.IO;


namespace MeshTopologyToolkit.Gltf
{
    public class GltfFileFormat : IFileFormat
    {
        public bool TryRead(IFileSystemEntry entry, out FileContainer? content)
        {
            content = null;

            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;
                var fileName = entry.Name;
                var readContext = ReadContext.Create(_=> ReadBytes((_ == fileName) ? entry.OpenRead():entry.GetNeigbourEntry(Uri.UnescapeDataString(_)).OpenRead()));
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
            return false;
        }
    }
}

