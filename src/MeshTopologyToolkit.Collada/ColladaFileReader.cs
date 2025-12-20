using System;
using System.Globalization;
using System.Numerics;
using System.Xml.Linq;

namespace MeshTopologyToolkit.Collada
{
    public class ColladaFileReader
    {
        static readonly XNamespace Ns = "http://www.collada.org/2005/11/COLLADASchema";

        private FileContainer _content;

        Dictionary<string, IMesh> _meshMap = new Dictionary<string, IMesh>();

        public ColladaFileReader(FileContainer content)
        {
            _content = content;
        }

        public void Parse(XDocument doc)
        {
            ParseLibraryGeometries(doc);
            ParseLibraryVisualScenes(doc);
        }

        private void ParseLibraryVisualScenes(XDocument doc)
        {
            // Find all <visual_scene> elements inside the <library_visual_scenes>
            var visualScenes = doc.Root.Element(Ns + "library_visual_scenes")?
                                     .Elements(Ns + "visual_scene") ?? Enumerable.Empty<XElement>();
            foreach (var visualScene in visualScenes)
            {
                var scene = new Scene(visualScene.Attribute("name")?.Value);
                ParseChildNodes(visualScene, scene);
                _content.Add(scene);
            }
        }

        private void ParseChildNodes(XElement parentElement, Node parentNode)
        {
            var nodes = parentElement.Elements(Ns + "node") ?? Enumerable.Empty<XElement>();
            foreach (var nodeElement in nodes)
            {
                var node = new Node(nodeElement.Attribute("name")?.Value);
                var matrixElement = nodeElement.Element(Ns + "matrix");
                if (matrixElement != null)
                {
                    var matrixValues = BuildMeshAttribute<float>(matrixElement.Value, 1, ParseFloat);
                    var m = new Matrix4x4(
                        matrixValues[0], matrixValues[1], matrixValues[2], matrixValues[3],
                        matrixValues[4], matrixValues[5], matrixValues[6], matrixValues[7],
                        matrixValues[8], matrixValues[9], matrixValues[10], matrixValues[11],
                        matrixValues[12], matrixValues[13], matrixValues[14], matrixValues[15]
                        );
                    node.Transform = new MatrixTransform(Matrix4x4.Transpose(m));
                }
                var instanceGeometry = nodeElement.Element(Ns + "instance_geometry");
                if (instanceGeometry != null)
                {
                    var meshName = instanceGeometry.Attribute("url")?.Value?.TrimStart('#');
                    if (meshName != null && _meshMap.TryGetValue(meshName, out var mesh))
                    {
                        node.Mesh = new MeshReference(mesh);
                    }
                }
                parentNode.AddChild(node);
                ParseChildNodes(nodeElement, node);
            }
        }

        private void ParseLibraryGeometries(XDocument doc)
        {
            // Find all <geometry> elements inside the <library_geometries>
            var geometries = doc.Root.Element(Ns + "library_geometries")?
                                     .Elements(Ns + "geometry") ?? Enumerable.Empty<XElement>();

            foreach (var geometry in geometries)
            {
                string meshId = geometry.Attribute("id")?.Value ?? "";
                string meshName = geometry.Attribute("name")?.Value ?? meshId;
                var mesh = new SeparatedIndexedMesh { Name = meshName };
                if (_meshMap.ContainsKey(meshId))
                {
                    throw new FormatException($"Duplicate mesh id \"{meshId}\"");
                }
                _meshMap.Add(meshId, mesh);
                _content.Add(mesh);

                // Find the <mesh> element inside the <geometry>
                var meshElement = geometry.Element(Ns + "mesh");
                if (meshElement == null) continue;

                // Find the <vertices> element to get the POSITION source ID
                string? positionSourceId = meshElement.Element(Ns + "vertices")?
                                                     .Element(Ns + "input")?
                                                     .Attribute("source")?.Value.TrimStart('#');

                if (positionSourceId == null)
                {
                    throw new FormatException($"Vertex position element id is not found for mesh \"{meshId}\" (\"{meshName}\")");
                }

                // Iterate over all <source> elements to find data arrays
                var sources = meshElement.Elements(Ns + "source");

                // 5. Parse Indices (Faces)
                var triangles = meshElement.Element(Ns + "triangles");
                if (triangles != null)
                {
                    var allIndices = BuildMeshAttribute<int>(triangles.Element(Ns + "p")?.Value, 1, ParseInt);

                    var indices = new List<List<int>>();
                    
                    var inputs = triangles.Elements(Ns + "input").ToList();
                    int stride = 0;
                    foreach (var input in inputs)
                    {
                        var semantic = input.Attribute("semantic")?.Value;
                        var offset = ParseInt(input.Attribute("offset")?.Value);
                        var setStr = input.Attribute("set")?.Value;
                        int set = 0;
                        if (setStr != null)
                            set = ParseInt(setStr);
                        var source = input.Attribute("source")?.Value?.TrimStart('#');

                        MeshAttributeKey attributeKey = MeshAttributeKey.Position;
                        switch (semantic)
                        {
                            case "VERTEX":
                                attributeKey = new MeshAttributeKey(MeshAttributeNames.Position, set);
                                source = positionSourceId;
                                break;
                            case "NORMAL":
                                attributeKey = new MeshAttributeKey(MeshAttributeNames.Normal, set);
                                break;
                            case "TEXCOORD":
                                attributeKey = new MeshAttributeKey(MeshAttributeNames.TexCoord, set);
                                break;
                            case "JOINT":
                                attributeKey = new MeshAttributeKey(MeshAttributeNames.Joints, set);
                                break;
                            case "WEIGHT":
                                attributeKey = new MeshAttributeKey(MeshAttributeNames.Weights, set);
                                break;
                            case "COLOR":
                                attributeKey = new MeshAttributeKey(MeshAttributeNames.Color, set);
                                break;
                            default:
                                attributeKey = new MeshAttributeKey(semantic!, set);
                                break;
                        }

                        while (indices.Count() <= offset)
                        {
                            indices.Add(new List<int>());
                        }

                        var sourceElement = sources.First(s => s.Attribute("id")?.Value == source);
                        var sourceStride = ParseInt(sourceElement.Element(Ns + "technique_common")?.Element(Ns + "accessor")?.Attribute("stride")?.Value);
                        var sourceValue = sourceElement.Element(Ns + "float_array")?.Value;
                        if (sourceValue != null)
                        {
                            mesh.SetAttribute(attributeKey, BuildFloatMeshAttribute(sourceValue, sourceStride), indices[offset]);
                        }
                        else
                        {
                            throw new NotImplementedException($"Source {source} type is not supported");
                        }

                        stride = Math.Max(offset + 1, stride);
                    }

                    // Only store the POSITION index (at offset 0)
                    for (int i = 0; i < allIndices.Count;)
                    {
                        foreach (var indexCollection in indices)
                        {
                            indexCollection.Add(allIndices[i]);
                            ++i;
                        }
                    }

                    mesh.WithTriangleList();
                }
            }
        }
        
        private int ParseInt(string? input)
        {
            return int.Parse(input, CultureInfo.InvariantCulture);
        }
        private int ParseInt(ArraySegment<string> input)
        {
            return int.Parse(input[0], CultureInfo.InvariantCulture);
        }
        private float ParseFloat(ArraySegment<string> input)
        {
            return float.Parse(input[0], CultureInfo.InvariantCulture);
        }
        private Vector2 ParseVector2(ArraySegment<string> input)
        {
            return new Vector2(float.Parse(input[0], CultureInfo.InvariantCulture), float.Parse(input[1], CultureInfo.InvariantCulture));
        }
        private Vector3 ParseVector3(ArraySegment<string> input)
        {
            return new Vector3(float.Parse(input[0], CultureInfo.InvariantCulture)
                , float.Parse(input[1], CultureInfo.InvariantCulture)
                , float.Parse(input[2], CultureInfo.InvariantCulture));
        }
        private Vector4 ParseVector4(ArraySegment<string> input)
        {
            return new Vector4(float.Parse(input[0], CultureInfo.InvariantCulture)
                , float.Parse(input[1], CultureInfo.InvariantCulture)
                , float.Parse(input[2], CultureInfo.InvariantCulture)
                , float.Parse(input[3], CultureInfo.InvariantCulture));
        }
        private IMeshVertexAttribute<T> BuildMeshAttribute<T>(string? stringValue, int stride, Func<ArraySegment<string>, T> factory) where T: notnull
        {
            var res = new ListMeshVertexAttribute<T>();
            if (string.IsNullOrEmpty(stringValue))
                return res;
            var valueArray = stringValue.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i< valueArray.Length; i+=stride)
            {
                res.Add(factory(new ArraySegment<string>(valueArray, i, stride)));
            }
            return res;
        }

        private IMeshVertexAttribute BuildFloatMeshAttribute(string? stringValue, int stride)
        {
            switch (stride)
            {
                case 1:
                    return BuildMeshAttribute<float>(stringValue, stride, ParseFloat);
                case 2:
                    return BuildMeshAttribute<Vector2>(stringValue, stride, ParseVector2);
                case 3:
                    return BuildMeshAttribute<Vector3>(stringValue, stride, ParseVector3);
                case 4:
                    return BuildMeshAttribute<Vector4>(stringValue, stride, ParseVector4);
                default:
                    throw new NotSupportedException($"Float array source with stride {stride} is not supported.");
            }
        }
    }
}