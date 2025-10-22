using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
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
                var readContext = ReadContext.Create(_=> ReadBytes((_ == fileName) ? entry.OpenRead():entry.GetNeigbourEntry(_).OpenRead()));
                var model = readContext.ReadSchema2(fileName);

                content = new FileContainer();

                foreach (var sourceScene in model.LogicalScenes)
                {
                    var scene = new Scene(sourceScene.Name);
                    content.Scenes.Add(scene);
                    VisitVisualChildren(scene, sourceScene.VisualChildren);
                }

                return true;
            }
        }

        private void VisitVisualChildren(Node parent, IEnumerable<SharpGLTF.Schema2.Node> visualChildren)
        {
            foreach (var child in visualChildren)
            {
                parent.AddChild(VisitNode(child));
            }
        }

        private Node VisitNode(SharpGLTF.Schema2.Node gltfNode)
        {
            var node = new Node(gltfNode.Name);
            VisitVisualChildren(node, gltfNode.VisualChildren);

            if (gltfNode.Mesh != null)
            {

            }

            return node;
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

