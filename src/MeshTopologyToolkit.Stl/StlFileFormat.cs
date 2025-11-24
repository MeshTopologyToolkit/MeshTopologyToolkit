using System.Numerics;
using System.Text;

namespace MeshTopologyToolkit.Stl
{
    public class StlFileFormat : IFileFormat
    {
        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Stereolithography .STL", ".stl"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;


        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            content = new FileContainer();

            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;

                var initialPosition = stream.Position;
                var buf = new byte[5];
                var header = stream.Read(buf,0,buf.Length);
                stream.Position = initialPosition;

                if (new SpanTokenizer(buf).Expect("solid"))
                {
                    return TryReadText(stream, content);
                }
                using (var reader = new BinaryReader(stream))
                {
                    return TryReadBinary(reader, content);
                }
            }
        }

        private bool TryReadText(Stream stream, FileContainer content)
        {
            var tokenizer = new SpanTokenizer(stream);
            if (!tokenizer.Expect("solid"))
                return false;
            tokenizer.ConsumeToEndOfLine();

            IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
            IMeshVertexAttribute<Vector3> normals = new ListMeshVertexAttribute<Vector3>();

            var positionIndices = new List<int>();
            var normalIndices = new List<int>();

            for (; ; )
            {
                tokenizer.ConsumeWhitespace();
                if (tokenizer.IsEndOfStream)
                    break;

                if (!tokenizer.TryReadWordToken(out var token))
                {
                    return false;
                }
                if (token.Equals("endsolid", StringComparison.Ordinal))
                {
                    break;
                }
                if (!token.Equals("facet", StringComparison.Ordinal))
                {
                    return false;
                }
                if (!tokenizer.Expect("normal"))
                    return false;
                float x, y, z;
                if (!tokenizer.TryReadFloat(out x))
                    return false;
                if (!tokenizer.TryReadFloat(out y))
                    return false;
                if (!tokenizer.TryReadFloat(out z))
                    return false;
                Vector3 normal = new Vector3(x, y, z);

                var normalIndex = normals.Add(normal);

                if (!tokenizer.Expect("outer"))
                    return false;
                if (!tokenizer.Expect("loop"))
                    return false;

                for (; ; )
                {
                    if (!tokenizer.TryReadWordToken(out token))
                    {
                        return false;
                    }
                    if (token.Equals("vertex", StringComparison.Ordinal))
                    {
                        if (!tokenizer.TryReadFloat(out x))
                            return false;
                        if (!tokenizer.TryReadFloat(out y))
                            return false;
                        if (!tokenizer.TryReadFloat(out z))
                            return false;
                        Vector3 position = new Vector3(x, y, z);

                        positionIndices.Add(positions.Add(position));
                        normalIndices.Add(normalIndex);
                    }
                    else if (token.Equals("endloop", StringComparison.Ordinal))
                    {
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }
                if (!tokenizer.Expect("endfacet"))
                    return false;
            }

            var mesh = new SeparatedIndexedMesh();
            mesh.AddAttribute(MeshAttributeKey.Position, positions, positionIndices);
            mesh.AddAttribute(MeshAttributeKey.Normal, normals, normalIndices);
            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, positionIndices.Count));
            content.Meshes.Add(mesh);

            var scene = new Scene();
            scene.AddChild(new Node() { Mesh = new MeshReference(mesh) });
            content.Scenes.Add(scene);

            return true;
        }

        private bool TryReadBinary(BinaryReader reader, FileContainer content)
        {
            reader.ReadBytes(80);
            var numTriangles = reader.ReadUInt32();

            IMeshVertexAttribute<Vector3> positions = new ListMeshVertexAttribute<Vector3>();
            IMeshVertexAttribute<Vector3> normals = new ListMeshVertexAttribute<Vector3>();

            var positionIndices = new List<int>();
            var normalIndices = new List<int>();

            for (int i = 0; i < numTriangles; ++i)
            {
                var n = reader.ReadVector3();
                var a = reader.ReadVector3();
                var b = reader.ReadVector3();
                var c = reader.ReadVector3();
                var attr = reader.ReadUInt16();

                positionIndices.Add(positions.Add(a));
                positionIndices.Add(positions.Add(b));
                positionIndices.Add(positions.Add(c));

                var normalIndex = normals.Add(n);
                normalIndices.Add(normalIndex);
                normalIndices.Add(normalIndex);
                normalIndices.Add(normalIndex);
            }

            var mesh = new SeparatedIndexedMesh();
            mesh.AddAttribute(MeshAttributeKey.Position, positions, positionIndices);
            mesh.AddAttribute(MeshAttributeKey.Normal, normals, normalIndices);
            mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, positionIndices.Count));
            content.Meshes.Add(mesh);

            var scene = new Scene();
            scene.AddChild(new Node() { Mesh = new MeshReference(mesh) });
            content.Scenes.Add(scene);

            return true;
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            var position = new ListMeshVertexAttribute<Vector3>();

            string name = "Mesh";
            if (content.Scenes.Any())
            {
                Merge(position, content.Scenes[0]);
                var sceneName = content.Scenes[0].Name;
                name = string.IsNullOrWhiteSpace(sceneName) ?name: sceneName;
            }
            else if (content.Meshes.Count > 0)
            {
                foreach (var mesh in content.Meshes)
                {
                    Merge(position, mesh, MatrixTransform.Identity);
                }
                var sceneName = content.Meshes[0].Name;
                name = string.IsNullOrWhiteSpace(sceneName) ? name : sceneName;
            }

            using (var stream = entry.OpenWrite())
            {
                using (var binaryWriter = new BinaryWriter(stream))
                {
                    var start = new UTF8Encoding(false).GetBytes("STLEXP "+name);
                    binaryWriter.Write(start, 0, Math.Min(80, start.Length));
                    for (int i = start.Length; i < 80; ++i)
                    {
                        binaryWriter.Write((byte)0);
                    }
                    binaryWriter.Write((int)(position.Count / 3));
                    for (int i=0; i<position.Count; i+=3)
                    {
                        var a = position[i];
                        var b = position[i+1];
                        var c = position[i+2];
                        var n = Vector3.Cross(b - a, c - a);
                        var nLength = n.Length();
                        if (nLength < 1e-6f)
                        {
                            n = Vector3.UnitZ;
                        }
                        else
                        {
                            n = n / nLength;
                        }
                        binaryWriter.Write(n);
                        binaryWriter.Write(a);
                        binaryWriter.Write(b);
                        binaryWriter.Write(c);
                        binaryWriter.Write((ushort)0);
                    }
                }

            }

            return true;
        }

        private void Merge(IMeshVertexAttribute<Vector3> positions, Node node)
        {
            if (node.Mesh != null)
            {
                Merge(positions, node.Mesh.Mesh, node.GetWorldSpaceTransform());
            }

            foreach (var child in node.Children)
            {
                Merge(positions, child);
            }
        }

        private void Merge(IMeshVertexAttribute<Vector3> positions, IMesh mesh, ITransform transform)
        {
            if (!mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var pos)) return;
            if (!mesh.TryGetAttributeIndices(MeshAttributeKey.Position, out var indices)) return;

            foreach (var drawCall in mesh.DrawCalls)
            {
                foreach (var face in drawCall.GetFaces(indices))
                {
                    positions.Add(transform.TransformPosition(pos[face.A]));
                    positions.Add(transform.TransformPosition(pos[face.B]));
                    positions.Add(transform.TransformPosition(pos[face.C]));
                }
            }
        }
    }
}
