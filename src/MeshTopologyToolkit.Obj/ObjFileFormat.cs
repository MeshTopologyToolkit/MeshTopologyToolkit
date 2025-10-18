using System.Numerics;

namespace MeshTopologyToolkit.Obj
{
    public class ObjFileFormat : IFileFormat
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

                content = new FileContainer();

                var positions = new ListMeshVertexAttribute<Vector3>();
                var normals = new ListMeshVertexAttribute<Vector3>();
                var uvs = new ListMeshVertexAttribute<Vector3>();

                var positionIndices = new List<int>();
                var normalIndices = new List<int>();
                var uvIndices = new List<int>();

                var addIndices = (Indices a) =>
                {
                    if (positions.Count > 0)
                    {
                        positionIndices.Add(a.Position-1);
                    }
                    if (normals.Count > 0)
                    {
                        normalIndices.Add(a.Normal-1);
                    }
                    if (uvs.Count > 0)
                    {
                        uvIndices.Add(a.TexCoord-1);
                    }
                };

                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var tokenizer = new SpanTokenizer(line);
                            if (tokenizer.TryReadWordToken(out var key))
                            {
                                switch (key)
                                {
                                    case "mtllib":
                                        break;
                                    case "v":
                                        positions.Add(ReadVector3(ref tokenizer));
                                        break;
                                    case "vn":
                                        normals.Add(ReadVector3(ref tokenizer));
                                        break;
                                    case "vt":
                                        uvs.Add(ReadVector3(ref tokenizer));
                                        break;
                                    case "o":
                                        break;
                                    case "g":
                                        break;
                                    case "usemtl":
                                        break;
                                    case "s":
                                        break;
                                    case "f":
                                        {
                                            if (TryReadIndices(ref tokenizer, out var A))
                                            {
                                                if (TryReadIndices(ref tokenizer, out var B))
                                                {
                                                    while (TryReadIndices(ref tokenizer, out var C))
                                                    {
                                                        addIndices(A);
                                                        addIndices(B);
                                                        addIndices(C);
                                                        B = C;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }

                var mesh = new SeparatedIndexedMesh();
                mesh.AddAttribute(MeshAttributeKey.Position, positions, positionIndices);
                if (normals.Count > 0)
                    mesh.AddAttribute(MeshAttributeKey.Normal, normals, normalIndices);
                if (uvs.Count > 0)
                    mesh.AddAttribute(MeshAttributeKey.TexCoord, uvs, uvIndices);
                mesh.DrawCalls.Add(new MeshDrawCall(MeshTopology.TriangleList, 0, positionIndices.Count));
                content.Meshes.Add(mesh);

                var scene = new Scene();
                scene.AddChild(new Node() { Mesh = new MeshReference(mesh) });
                content.Scenes.Add(scene);

                return true;
            }
        }

        private Vector3 ReadVector3(ref SpanTokenizer tokenizer)
        {
            Vector3 result = Vector3.Zero;
            if (tokenizer.TryReadFloat(out var x))
            {
                result.X = x;
                if (tokenizer.TryReadFloat(out var y))
                {
                    result.Y = y;
                    if (tokenizer.TryReadFloat(out var z))
                    {
                        result.Z = z;
                    }
                }
            }
            return result;
        }

        private bool TryReadIndices(ref SpanTokenizer tokenizer, out Indices result)
        {
            result = new Indices();
            if (tokenizer.TryReadWordToken(out var val))
            {
                var valTokenizer = new SpanTokenizer(val);
                if (valTokenizer.TryReadInt(out var pos))
                {
                    result.Position = pos;
                    if (valTokenizer.Expect("/"))
                    {
                        if (valTokenizer.TryReadInt(out var uv))
                        {
                            result.TexCoord = uv;
                        }
                        if (valTokenizer.Expect("/"))
                        {
                            if (valTokenizer.TryReadInt(out var n))
                            {
                                result.Normal = n;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            return false;
        }
    }

    struct Indices
    {
        public int Position;
        public int TexCoord;
        public int Normal;
    }
}
