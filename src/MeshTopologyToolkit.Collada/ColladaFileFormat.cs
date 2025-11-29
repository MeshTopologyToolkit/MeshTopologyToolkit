using MeshTopologyToolkit.Operators;
using System.Globalization;
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
                new XElement(c + "library_geometries", CreateLibraryGeometries(content.Meshes)),
                new XElement(c + "library_effects", CreateLibraryEffects(content.Materials)),
                new XElement(c + "library_materials", CreateLibraryMaterials(content.Materials))
            );
            doc.Add(root);
            var scenes = new XElement(c + "library_visual_scenes");
            root.Add(scenes);
            for (int i = 0; i < content.Scenes.Count; i++)
            {
                Scene? scene = content.Scenes[i];
                var visualScene = new XElement(c + "visual_scene",
                    new XAttribute("id", scene.Name),
                    new XAttribute("name", scene.Name),
                    CreateNodes(scene.Children)
                );
                scenes.Add(visualScene);
            }

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
                    new XAttribute("id", material.Name),
                    new XAttribute("name", material.Name),
                    new XElement(c + "instance_effect",
                        new XAttribute("url", "#effect_" + material.Name)
                    )
                );
            }
        }

        private IEnumerable<XElement> CreateLibraryEffects(IList<Material> materials)
        {
            foreach (var material in materials)
            {
                yield return new XElement(c + "effect",
                    new XAttribute("id", "effect_"+material.Name),
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
                var geometry = new XElement(c + "geometry", new XAttribute("id", mesh.Name), new XAttribute("name", mesh.Name));
                yield return geometry;
            }
        }

        private IEnumerable<XElement> CreateNodes(IReadOnlyList<Node> children)
        {
            foreach (var child in children)
            {
                yield return new XElement(c + "node",
                    new XAttribute("id", child.Name),
                    new XAttribute("name", child.Name),
                    CreateMesh(child.Mesh),
                    CreateNodes(child.Children)
                );
            }
        }

        private IEnumerable<XElement> CreateMesh(MeshReference? mesh)
        {
            if (mesh?.Mesh == null)
                yield break;

            yield return new XElement(c + "instance_geometry", 
                new XAttribute("url", "#" + mesh.Mesh.Name),
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
                                new XAttribute("target", "#" + material.Name)
                            )
                        )
                    );
                }
            }
        }
    }
}