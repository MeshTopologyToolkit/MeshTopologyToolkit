using System.IO.Enumeration;
using System.Numerics;
using System.Reflection.PortableExecutable;

namespace MeshTopologyToolkit.Stl
{
    public class StlFileFormat : IFileFormat
    {
        public bool TryRead(IFileSystemEntry entry, out FileContainer? content)
        {
            content = null;
            if (!entry.Exists)
                return false;

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
                    return TryReadText(stream, out content);
                }
                using (var reader = new BinaryReader(stream))
                {
                    return TryReadBinary(reader, out content);
                }
            }
        }

        private bool TryReadText(Stream stream, out FileContainer? content)
        {
            content = null;
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

            content = new FileContainer();
            var mesh = new SeparatedIndexedMesh();
            mesh.AddAttribute(MeshAttributeKey.Position, positions, positionIndices);
            mesh.AddAttribute(MeshAttributeKey.Normal, normals, normalIndices);
            content.Meshes.Add(mesh);

            var scene = new Scene();
            scene.Children.Add(new Node() { Mesh = new MeshReference(mesh) });
            return true;
        }

        private bool TryReadBinary(BinaryReader reader, out FileContainer? content)
        {
            reader.ReadBytes(80);
            var numTriangles = reader.ReadUInt32();

            content = new FileContainer();
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
            content.Meshes.Add(mesh);

            var scene = new Scene();
            scene.Children.Add(new Node() { Mesh = new MeshReference(mesh) });

            return true;
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            return false;
        }
    }
}
