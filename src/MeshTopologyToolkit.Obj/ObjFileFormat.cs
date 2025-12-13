using System.Globalization;
using System.Numerics;
using System.Text;

namespace MeshTopologyToolkit.Obj
{
    public class ObjFileFormat : IFileFormat
    {
        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Wavefront .obj", ".obj"),
        };

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            content = new FileContainer();

            if (!entry.Exists)
                return false;

            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;

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
                        positionIndices.Add(a.Position - 1);
                    }
                    if (normals.Count > 0)
                    {
                        normalIndices.Add(a.Normal - 1);
                    }
                    if (uvs.Count > 0)
                    {
                        uvIndices.Add(a.TexCoord - 1);
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
                mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, positionIndices.Count));
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
            WriteData data = new WriteData();
            string name = "Mesh";
            if (content.Scenes.Any())
            {
                Merge(data, content.Scenes[0]);
                var sceneName = content.Scenes[0].Name;
                name = string.IsNullOrWhiteSpace(sceneName) ? name : sceneName;
            }
            else if (content.Meshes.Count > 0)
            {
                foreach (var mesh in content.Meshes)
                {
                    Merge(data, mesh, MatrixTransform.Identity);
                }
                var sceneName = content.Meshes[0].Name;
                name = string.IsNullOrWhiteSpace(sceneName) ? name : sceneName;
            }

            using (var stream = entry.OpenWrite())
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.WriteLine($"# MeshTopologyToolkit Wavefront OBJ Exporter v{GetType().Assembly.GetName().Version}");
                    writer.WriteLine();
                    foreach (var pos in data.Positions)
                    {
                        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "v  {0} {1} {2}", pos.X, pos.Y, pos.Z));
                    }
                    if (data.Normals.Count > 0)
                    {
                        writer.WriteLine();
                        foreach (var normal in data.Normals)
                        {
                            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "vn {0} {1} {2}", normal.X, normal.Y, normal.Z));
                        }
                    }
                    if (data.TexCoords.Count > 0)
                    {
                        writer.WriteLine();
                        foreach (var texCoord in data.TexCoords)
                        {
                            writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "vt {0} {1} {2}", texCoord.X, texCoord.Y, texCoord.Z));
                        }
                    }

                    var indexFormatter = (int index) => { return string.Format(CultureInfo.InvariantCulture, "{0}", data.PositionIndices[index] + 1); };
                    if (data.TexCoords.Count > 0)
                    {
                        if (data.NormalIndices.Count > 0)
                        {
                            indexFormatter = (int index) => { return string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", data.PositionIndices[index] + 1, data.TexCoordIndices[index] + 1, data.NormalIndices[index] + 1); };
                        }
                        else
                        {
                            indexFormatter = (int index) => { return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", data.PositionIndices[index] + 1, data.TexCoordIndices[index] + 1); };
                        }
                    }
                    else
                    {
                        if (data.NormalIndices.Count > 0)
                        {
                            indexFormatter = (int index) => { return string.Format(CultureInfo.InvariantCulture, "{0}//{1}", data.PositionIndices[index] + 1, data.NormalIndices[index] + 1); };
                        }
                    }

                    writer.WriteLine();
                    writer.WriteLine("o Mesh");
                    writer.WriteLine("g Mesh");
                    for (int index = 0; index < data.PositionIndices.Count; index += 3)
                    {
                        writer.WriteLine("f {0} {1} {2}", indexFormatter(index), indexFormatter(index + 1), indexFormatter(index + 2));
                    }
                }
            }
            return true;
        }

        private void Merge(WriteData data, Node node)
        {
            if (node.Mesh != null)
            {
                Merge(data, node.Mesh.Mesh, node.GetWorldSpaceTransform());
            }

            foreach (var child in node.Children)
            {
                Merge(data, child);
            }
        }

        private void Merge(WriteData data, IMesh mesh, ITransform transform)
        {
            if (!mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var pos)) return;
            if (!mesh.TryGetAttributeIndices(MeshAttributeKey.Position, out var indices)) return;

            bool hasNormals = mesh.TryGetAttributeIndices(MeshAttributeKey.Normal, out var normalIndices);
            hasNormals = mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var normals) && hasNormals;
            bool needNormals = hasNormals || data.Normals.Count > 0;

            if (needNormals && (data.NormalIndices.Count < data.PositionIndices.Count))
            {
                var normalIndex = data.Normals.Add(Vector3.UnitZ);
                while (data.NormalIndices.Count < data.PositionIndices.Count)
                {
                    data.NormalIndices.Add(normalIndex);
                }
            }

            bool hasTexCoords = mesh.TryGetAttribute<Vector3>(MeshAttributeKey.TexCoord, out var texCoords);
            hasTexCoords = mesh.TryGetAttributeIndices(MeshAttributeKey.TexCoord, out var texCoordIndices) && hasTexCoords;
            bool needTexCoords = hasTexCoords || data.TexCoords.Count > 0;

            if (needNormals && (data.TexCoordIndices.Count < data.PositionIndices.Count))
            {
                var uvIndex = data.TexCoords.Add(Vector3.Zero);
                while (data.TexCoordIndices.Count < data.PositionIndices.Count)
                {
                    data.TexCoordIndices.Add(uvIndex);
                }
            }

            foreach (var drawCall in mesh.DrawCalls)
            {
                foreach (var face in drawCall.GetFaces(indices))
                {
                    data.PositionIndices.Add(data.Positions.Add(transform.TransformPosition(pos[face.A])));
                    data.PositionIndices.Add(data.Positions.Add(transform.TransformPosition(pos[face.B])));
                    data.PositionIndices.Add(data.Positions.Add(transform.TransformPosition(pos[face.C])));
                }
                if (needNormals)
                {
                    if (hasNormals)
                    {
                        foreach (var face in drawCall.GetFaces(normalIndices))
                        {
                            data.NormalIndices.Add(data.Normals.Add(transform.TransformDirection(normals[face.A])));
                            data.NormalIndices.Add(data.Normals.Add(transform.TransformDirection(normals[face.B])));
                            data.NormalIndices.Add(data.Normals.Add(transform.TransformDirection(normals[face.C])));
                        }
                    }
                }
                if (needTexCoords)
                {
                    if (hasTexCoords)
                    {
                        foreach (var face in drawCall.GetFaces(texCoordIndices))
                        {
                            data.TexCoordIndices.Add(data.TexCoords.Add(transform.TransformDirection(texCoords[face.A])));
                            data.TexCoordIndices.Add(data.TexCoords.Add(transform.TransformDirection(texCoords[face.B])));
                            data.TexCoordIndices.Add(data.TexCoords.Add(transform.TransformDirection(texCoords[face.C])));
                        }
                    }
                }
            }
        }
    }

    class WriteData
    {
        IMeshVertexAttribute<Vector3> _positions = new DictionaryMeshVertexAttribute<Vector3>();
        IMeshVertexAttribute<Vector3> _normals = new DictionaryMeshVertexAttribute<Vector3>();
        IMeshVertexAttribute<Vector3> _texCoords = new DictionaryMeshVertexAttribute<Vector3>();

        IList<int> _positionIndices = new List<int>();
        IList<int> _normalIndices = new List<int>();
        IList<int> _texCoordIndices = new List<int>();

        public IMeshVertexAttribute<Vector3> Positions => _positions;
        public IMeshVertexAttribute<Vector3> Normals => _normals;
        public IMeshVertexAttribute<Vector3> TexCoords => _texCoords;
        public IList<int> PositionIndices => _positionIndices;
        public IList<int> NormalIndices => _normalIndices;
        public IList<int> TexCoordIndices => _texCoordIndices;
    }
}

