using MeshTopologyToolkit.Operators;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MeshTopologyToolkit.Collada
{
    /// <summary>
    /// Provides functionality to read and write Stereolithography (STL) files.
    /// </summary>
    /// <remarks>This class supports both binary and ASCII STL file formats. It implements the <see
    /// cref="IFileFormat"/> interface to provide methods for reading from and writing to STL files.
    /// </remarks>

    public class ColladaFileFormat : IFileFormat
    {
        // COLLADA namespace
        static readonly XNamespace c = "http://www.collada.org/2005/11/COLLADASchema";

        /// <summary>
        /// Represents a collection of supported file extensions for the file format.
        /// </summary>
        /// <remarks>This static readonly field contains an array of <see cref="SupportedExtension"/>
        /// objects, each representing a specific file extension that is supported by the class.</remarks>
        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Collada .DAE", ".dae"),
        };

        private ColladaUpAxis _upAxis;

        /// <summary>
        /// Represents a collection of supported file extensions for the file format.
        /// </summary>
        /// <remarks>This property contains an array of <see cref="SupportedExtension"/>
        /// objects, each representing a specific file extension that is supported by the class.</remarks>
        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public ColladaFileFormat(ColladaUpAxis upAxis = ColladaUpAxis.Y)
        {
            _upAxis = upAxis;
        }

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            content = new FileContainer();
            return false;
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            content = new EnsureUniqueNames().Transform(content);

            var up = _upAxis switch
            {
                ColladaUpAxis.X => "X_UP",
                ColladaUpAxis.Y => "Y_UP",
                ColladaUpAxis.Z => "Z_UP",
                _ => "Y_UP",
            };
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "no"));
            var root = new XElement(c + "COLLADA",
                new XAttribute("version", "1.4.1"),
                // asset
                new XElement(c + "asset",
                    new XElement(c + "contributor",
                        new XElement(c + "authoring_tool", "MeshTopologyToolkit.Collada")
                    ),
                    new XElement(c + "created", DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture)),
                    new XElement(c + "modified", DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture)),
                    new XElement(c + "unit", new XAttribute("name", "meter"), new XAttribute("meter", "1")),
                    new XElement(c + "up_axis", up)
                ),
                new XElement(c + "library_images", CreateLibraryImages(content.Textures)),
                new XElement(c + "library_materials", CreateLibraryMaterials(content.Materials)),
                new XElement(c + "library_effects", CreateLibraryEffects(content.Materials)),
                new XElement(c + "library_geometries", CreateLibraryGeometries(content.Meshes))
            );
            doc.Add(root);
            var scenes = new XElement(c + "library_visual_scenes");
            var sceneElement = new XElement(c + "scene");
            root.Add(scenes);
            for (int i = 0; i < content.Scenes.Count; i++)
            {
                Scene? scene = content.Scenes[i];
                var visualScene = new XElement(c + "visual_scene",
                    new XAttribute("id", ToColladaId(scene.Name)),
                    new XAttribute("name", scene.Name),
                    CreateNodes(scene.Children)
                );
                if (i == 0)
                    sceneElement.Add(new XElement(c + "instance_visual_scene", new XAttribute("url", "#" + ToColladaId(scene.Name))));
                scenes.Add(visualScene);
            }
            root.Add(sceneElement);

            using (var stream = entry.OpenWrite())
            {
                doc.Save(stream);
            }
            return true;
        }

        private IEnumerable<XElement> CreateLibraryMaterials(IList<Material> materials)
        {
            foreach (var material in materials)
            {
                yield return new XElement(c + "material",
                    new XAttribute("id", ToColladaId(material.Name)),
                    new XAttribute("name", material.Name),
                    new XElement(c + "instance_effect",
                        new XAttribute("url", "#" + ToColladaId("effect_" + material.Name))
                    )
                );
            }
        }

        private IEnumerable<XElement> CreateLibraryImages(IList<Texture> textures)
        {
            foreach (var texture in textures)
            {
                if (texture?.FileSystemEntry == null)
                    continue;
                var mime = Path.GetExtension(texture.FileSystemEntry.Name).ToLowerInvariant().TrimStart('.');
                yield return new XElement(c + "image",
                    new XAttribute("id", ToColladaId(Path.GetFileName(texture.FileSystemEntry.Name))),
                    new XAttribute("name", texture.FileSystemEntry.Name),
                    new XElement(c + "init_from",
                        "data:image/" + mime + ";base64," + Convert.ToBase64String(texture.FileSystemEntry.ReadAllBytes())
                    )
                );
            }
        }

        private IEnumerable<XElement> CreateLibraryEffects(IList<Material> materials)
        {
            foreach (var material in materials)
            {
                yield return new XElement(c + "effect",
                    new XAttribute("id", ToColladaId("effect_" +material.Name)),
                    new XAttribute("name", "effect_" + material.Name),
                    new XElement(c + "profile_COMMON",
                            new XElement(c + "technique", new XAttribute("sid", "common"),
                                new XElement(c + "lambert",
                                    new XElement(c + "diffuse",
                                        new XElement(c + "color", "0.8 0.8 0.8 1")
                                    )
                                )
                            )
                        )
                );
            }
        }

        private IEnumerable<XElement> CreateLibraryGeometries(IList<IMesh> meshes)
        {
            foreach (var mesh in meshes)
            {
                var geometry = new XElement(c + "geometry", new XAttribute("id", ToColladaId(mesh.Name)), new XAttribute("name", mesh.Name));
                var meshElement = new XElement(c + "mesh");
                geometry.Add(meshElement);
                var inputs = new List<Input>();
                foreach (var key in mesh.GetAttributeKeys())
                {
                    if (mesh.TryGetAttribute(key, out var attribute))
                    {
                        var semantic = GetSematic(key);
                        if (semantic == null)
                            continue;

                        (var data, var components) = BuildArray(key, mesh.GetAttribute(key));
                        if (data == null)
                            continue;

                        inputs.Add(new Input(semantic, inputs.Count, "#" + ToColladaId(mesh.Name + "_" + key.Name), (key == MeshAttributeKey.TexCoord)?key.Channel:null, mesh.GetAttributeIndices(key)));

                        var source = new XElement(c + "source", new XAttribute("id", ToColladaId(mesh.Name + "_" + key.Name)));
                        var floatArray = new XElement(c + "float_array",
                            new XAttribute("id", mesh.Name + "_" + key.Name + "_array"),
                            new XAttribute("count", data.Count),
                            string.Join(" ", data)
                        );
                        source.Add(floatArray);
                        var techniqueCommon = new XElement(c + "technique_common");
                        var accessor = new XElement(c + "accessor",
                            new XAttribute("source", "#" + ToColladaId(mesh.Name + "_" + key.Name + "_array")),
                            new XAttribute("count", attribute.Count),
                            new XAttribute("stride", components.Count)
                        );
                        for (int i = 0; i < components.Count; i++)
                        {
                            accessor.Add(new XElement(c + "param", new XAttribute("name", components[i].Name), new XAttribute("type", components[i].Type)));
                        }
                        techniqueCommon.Add(accessor);
                        source.Add(techniqueCommon);
                        meshElement.Add(source);
                    }
                }

                {
                    var vertices = new XElement(c + "vertices", new XAttribute("id", ToColladaId(mesh.Name + "_vertices")),
                        new XElement(c + "input", new XAttribute("semantic", "POSITION"), new XAttribute("source", "#" + ToColladaId(mesh.Name + "_" + MeshAttributeKey.Position))));
                    meshElement.Add(vertices);
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        Input? input = inputs[i];
                        if (input.Semantic == "POSITION")
                        {
                            inputs[i] = new Input("VERTEX", input.Offset, "#" + ToColladaId(mesh.Name + "_vertices"), input.Set, input.Indices);
                        }
                    }
                }

                foreach (var drawCall in mesh.DrawCalls)
                {
                    if (drawCall.Type == MeshTopology.TriangleList || drawCall.Type == MeshTopology.TriangleStrip || drawCall.Type == MeshTopology.TriangleFan)
                    {
                        var faces = drawCall.GetFaces().ToList();
                        var primitive = new XElement(c + "triangles", new XAttribute("count", faces.Count));
                        foreach (var input in inputs)
                        {
                            var inp = new XElement(c + "input",
                                new XAttribute("semantic", input.Semantic),
                                new XAttribute("source", input.Source),
                                new XAttribute("offset", input.Offset)
                            );
                            if (input.Set.HasValue)
                                inp.Add(new XAttribute("set", input.Set.Value));
                            primitive.Add(inp);
                        }
                        var p = new XElement(c + "p");
                        var allIndices = new List<int>();
                        foreach (var face in faces)
                        {
                            foreach (var index in face)
                            {
                                foreach (var input in inputs)
                                {
                                    allIndices.Add(input.Indices[index]);
                                }
                            }
                        }
                        p.Value = string.Join(" ", allIndices);
                        primitive.Add(p);
                        meshElement.Add(primitive);
                    }
                }
                yield return geometry;
            }
        }

        private (IReadOnlyList<float>?, IReadOnlyList<StreamParam>) BuildArray(MeshAttributeKey key, IMeshVertexAttribute meshVertexAttribute)
        {
            var elementType = meshVertexAttribute.GetElementType();
            if (meshVertexAttribute is IMeshVertexAttribute<Vector3> vec3)
                return BuildArrayVec3(key, vec3);
            if (meshVertexAttribute is IMeshVertexAttribute<Vector2> vec2)
                return BuildArrayVec2(key, vec2);
            return (null, Array.Empty<StreamParam>());
        }

        private (IReadOnlyList<float>, IReadOnlyList<StreamParam>) BuildArrayVec3(MeshAttributeKey key, IMeshVertexAttribute<Vector3> meshVertexAttribute)
        {
            var data = new float[meshVertexAttribute.Count * 3];
            for (int i = 0; i < meshVertexAttribute.Count; i++)
            {
                var v = meshVertexAttribute[i];
                data[i * 3 + 0] = v.X;
                data[i * 3 + 1] = v.Y;
                data[i * 3 + 2] = v.Z;
            }

            if (key.Name == MeshAttributeNames.TexCoord)
            {
                return (data, new[] { new StreamParam("S"), new StreamParam("T"), new StreamParam("U") });
            }
            else
            {
                return (data, new[] { new StreamParam("X"), new StreamParam("Y"), new StreamParam("Z") });
            }

        }

        private (IReadOnlyList<float>, IReadOnlyList<StreamParam>) BuildArrayVec2(MeshAttributeKey key, IMeshVertexAttribute<Vector2> meshVertexAttribute)
        {
            var data = new float[meshVertexAttribute.Count * 2];
            for (int i = 0; i < meshVertexAttribute.Count; i++)
            {
                var v = meshVertexAttribute[i];
                data[i * 2 + 0] = v.X;
                data[i * 2 + 1] = v.Y;
            }

            if (key.Name == MeshAttributeNames.TexCoord)
            {
                return (data, new[] { new StreamParam("S"), new StreamParam("T") });
            }
            else
            {
                return (data, new[] { new StreamParam("X"), new StreamParam("Y") });
            }

        }
        private string? GetSematic(MeshAttributeKey key)
        {
            switch (key.Name)
            {
                case MeshAttributeNames.Position:
                    return "POSITION";
                case MeshAttributeNames.Normal:
                    return "NORMAL";
                case MeshAttributeNames.TexCoord:
                    return "TEXCOORD";
                case MeshAttributeNames.Joints:
                    return "JOINT";
                case MeshAttributeNames.Weights:
                    return "WEIGHT";
                case MeshAttributeNames.Color:
                    return "COLOR";
                default:
                    return null;
            }
        }

        private IEnumerable<XElement> CreateNodes(IReadOnlyList<Node> children)
        {
            foreach (var child in children)
            {
                yield return new XElement(c + "node",
                    new XAttribute("id", ToColladaId(child.Name)),
                    new XAttribute("name", child.Name),
                    new XElement(c+"matrix", new XAttribute("sid", "matrix"), GetMatrixComponentString(child.Transform.ToMatrix())),
                    CreateMesh(child.Mesh),
                    CreateNodes(child.Children)
                );
            }
        }

        private string GetMatrixComponentString(Matrix4x4 matrix4x4)
        {
            var sb = new StringBuilder();
            sb.Append(matrix4x4.M11);
            sb.Append(" ");
            sb.Append(matrix4x4.M12);
            sb.Append(" ");
            sb.Append(matrix4x4.M13);
            sb.Append(" ");
            sb.Append(matrix4x4.M14);
            sb.Append(" ");

            sb.Append(matrix4x4.M21);
            sb.Append(" ");
            sb.Append(matrix4x4.M22);
            sb.Append(" ");
            sb.Append(matrix4x4.M23);
            sb.Append(" ");
            sb.Append(matrix4x4.M24);
            sb.Append(" ");

            sb.Append(matrix4x4.M31);
            sb.Append(" ");
            sb.Append(matrix4x4.M32);
            sb.Append(" ");
            sb.Append(matrix4x4.M33);
            sb.Append(" ");
            sb.Append(matrix4x4.M34);
            sb.Append(" ");

            sb.Append(matrix4x4.M41);
            sb.Append(" ");
            sb.Append(matrix4x4.M42);
            sb.Append(" ");
            sb.Append(matrix4x4.M43);
            sb.Append(" ");
            sb.Append(matrix4x4.M44);

            return sb.ToString();
        }

        private IEnumerable<XElement> CreateMesh(MeshReference? mesh)
        {
            if (mesh?.Mesh == null)
                yield break;

            yield return new XElement(c + "instance_geometry", 
                new XAttribute("url", "#" + ToColladaId(mesh.Mesh.Name)),
                CreateMaterials(mesh.Materials));
        }

        private IEnumerable<XElement> CreateMaterials(IEnumerable<Material> materials)
        {
            foreach (var material in materials)
            {
                if (material != null)
                {
                    yield return new XElement(c + "bind_material",
                        new XElement(c + "technique_common",
                            new XElement(c + "instance_material",
                                new XAttribute("symbol", material.Name),
                                new XAttribute("target", "#" + ToColladaId(material.Name))
                            )
                        )
                    );
                }
            }
        }

        /// <summary>
        /// Convert an arbitrary string into a valid COLLADA element ID (an XML NCName).
        /// This:
        ///  - encodes illegal XML characters,
        ///  - ensures the name starts with a valid NCName start char (prefixing if necessary),
        ///  - avoids names beginning with "xml" (case-insensitive).
        /// </summary>
        /// <param name="input">The input string to convert.</param>
        /// <param name="fallbackPrefix">Prefix to add when the result would be invalid as an NCName start. Defaults to "id".</param>
        /// <returns>A safe string that can be used as a COLLADA element id attribute.</returns>
        public static string ToColladaId(string? input, string fallbackPrefix = "id")
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(fallbackPrefix)) fallbackPrefix = "id";

            // Trim whitespace and reduce runs of whitespace to single space
            var trimmed = System.Text.RegularExpressions.Regex.Replace(input.Trim(), @"\s+", " ");

            // Use XmlConvert to encode characters not allowed in XML local names.
            // This converts e.g. spaces -> _x0020_, etc. It does NOT include a ':'.
            string encoded = XmlConvert.EncodeLocalName(trimmed);

            // Ensure it is a valid NCName (start char must be letter or underscore).
            // XmlConvert.VerifyNCName throws if it's not a valid NCName.
            bool valid = true;
            try
            {
                XmlConvert.VerifyNCName(encoded);
            }
            catch
            {
                valid = false;
            }

            // If not valid, prefix with fallbackPrefix_ and re-encode to be sure.
            if (!valid)
            {
                // If encoded already starts with fallbackPrefix, keep to avoid long duplication.
                string candidate = encoded.StartsWith(fallbackPrefix + "_", StringComparison.OrdinalIgnoreCase)
                    ? encoded
                    : (fallbackPrefix + "_" + encoded);

                // Re-encode (EncodeLocalName won't change already-encoded sequences but ensures safety)
                encoded = XmlConvert.EncodeLocalName(candidate);

                // If still invalid (very unlikely), forcibly create simple id with hex hash
                try
                {
                    XmlConvert.VerifyNCName(encoded);
                }
                catch
                {
                    // fallback to deterministic hex-based id to guarantee NCName safety
                    string hex = BitConverter.ToString(System.Security.Cryptography.SHA1.Create()
                                              .ComputeHash(System.Text.Encoding.UTF8.GetBytes(input))).Replace("-", "");
                    encoded = $"{fallbackPrefix}_{hex}";
                }
            }

            // Avoid names starting with "xml" (case-insensitive) per XML recommendations
            if (encoded.Length >= 3 && encoded.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
            {
                encoded = fallbackPrefix + "_" + encoded;
                // ensure still valid (should be)
                encoded = XmlConvert.EncodeLocalName(encoded);
            }

            return encoded;
        }
    }
}