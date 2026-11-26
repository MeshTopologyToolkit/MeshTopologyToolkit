using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Converts an <see cref="FbxDocument"/> to a <see cref="FileContainer"/>.
    /// The structure follows ufbx: objects are connected by OO/OP connections, models form the node hierarchy,
    /// geometries become meshes, deformers become skins, animation stacks become animations.
    /// </summary>
    internal sealed partial class FbxSceneReader
    {
        private readonly FbxObjectGraph _graph;
        private readonly IFileSystemEntry _entry;
        private readonly FileContainer _content = new FileContainer();

        private readonly Dictionary<FbxObject, Node> _nodes = new Dictionary<FbxObject, Node>();
        private readonly Dictionary<FbxObject, FbxMeshData> _meshes = new Dictionary<FbxObject, FbxMeshData>();
        private readonly Dictionary<FbxObject, Material> _materials = new Dictionary<FbxObject, Material>();
        private readonly Dictionary<FbxObject, Texture?> _textures = new Dictionary<FbxObject, Texture?>();
        private readonly Dictionary<FbxObject, Skin?> _skins = new Dictionary<FbxObject, Skin?>();

        public FbxSceneReader(FbxDocument doc, IFileSystemEntry entry)
        {
            _graph = new FbxObjectGraph(doc);
            _entry = entry;
        }

        public FileContainer Read()
        {
            _content.FileToGltfTransform = ReadGlobalSettings();

            var scene = new Scene(Path.GetFileNameWithoutExtension(_entry.Name));
            ReadModels(scene);
            _content.Add(scene);
            ReadAnimations();
            return _content;
        }

        // ---------------------------------------------------------------- Global settings

        private SpaceTransform ReadGlobalSettings()
        {
            var props = new FbxProps();
            props.Add(_graph.Document.Child("GlobalSettings"));
            // Legacy files keep the axes in a Settings node.
            props.Add(_graph.Document.Child("Settings"));

            int up = props.GetInt("UpAxis", 1), upSign = props.GetInt("UpAxisSign", 1);
            int front = props.GetInt("FrontAxis", 2), frontSign = props.GetInt("FrontAxisSign", 1);
            int coord = props.GetInt("CoordAxis", 0), coordSign = props.GetInt("CoordAxisSign", 1);
            double unit = props.GetDouble("UnitScaleFactor", 1.0);
            if (!(unit > 0.0))
                unit = 1.0;
            return CreateSpaceTransform(coord, coordSign, up, upSign, front, frontSign, (float)(unit * 0.01));
        }

        internal static SpaceTransform CreateSpaceTransform(int coord, int coordSign, int up, int upSign, int front, int frontSign, float scale)
        {
            if (coord < 0 || coord > 2 || up < 0 || up > 2 || front < 0 || front > 2 || coord == up || coord == front || up == front)
                return new SpaceTransform(SpaceTransform.XYZ, scale, true);

            static float Sign(int s) => s < 0 ? -1.0f : 1.0f;
            // Where the axes of the file end up in glTF space: right to +X, up to +Y, front to +Z.
            var c = AxisVector(coord, Sign(coordSign));
            var u = AxisVector(up, Sign(upSign));
            var f = AxisVector(front, Sign(frontSign));
            var rotation = SpaceTransform.CreateMatrix(
                new Vector3(c.X, u.X, f.X),
                new Vector3(c.Y, u.Y, f.Y),
                new Vector3(c.Z, u.Z, f.Z));
            // A file whose axes are left-handed is mirrored on its way to glTF, which turns the triangles inside out: their winding is flipped back.
            return new SpaceTransform(rotation, scale, true, rotation.GetDeterminant() < 0.0f);
        }

        private static Vector3 AxisVector(int axis, float sign)
        {
            switch (axis)
            {
                case 0: return new Vector3(sign, 0, 0);
                case 1: return new Vector3(0, sign, 0);
                default: return new Vector3(0, 0, sign);
            }
        }

        // ---------------------------------------------------------------- Models

        private void ReadModels(Scene scene)
        {
            var models = _graph.OfClass("Model").ToList();
            if (_graph.IsLegacy)
            {
                // Legacy files list default cameras and similar helper models that are not part of the scene, they have no connections.
                models = models.Where(m => m.ParentLinks.Any(l => l.Parent.Class == "Model" || l.Parent == _graph.Root)).ToList();
            }
            foreach (var model in models)
            {
                var node = new Node(model.Name);
                node.Transform = FbxNodeTransform.Read(model.Props).ToTransform();
                _nodes[model] = node;
            }

            foreach (var model in models)
            {
                var node = _nodes[model];
                Node parent = scene;
                foreach (var link in model.ParentLinks)
                {
                    if (link.Property == null && link.Parent.Class == "Model" && _nodes.TryGetValue(link.Parent, out var parentNode) && parentNode != node)
                    {
                        parent = parentNode;
                        break;
                    }
                }
                parent.AddChild(node);
            }

            foreach (var model in models)
                ReadModelAttributes(model, _nodes[model]);
        }

        private void ReadModelAttributes(FbxObject model, Node node)
        {
            // Geometry: a connected Geometry object, or the model itself for legacy files.
            FbxObject? geometry = null;
            FbxNode? geometryNode = null;
            foreach (var link in model.ChildLinks)
            {
                if (link.Property == null && link.Child.Class == "Geometry" && link.Child.Node.Child("Vertices") != null)
                {
                    geometry = link.Child;
                    geometryNode = link.Child.Node;
                    break;
                }
            }
            if (geometry == null && model.Node.Child("Vertices") != null)
            {
                geometry = model;
                geometryNode = model.Node;
            }
            if (geometry == null || geometryNode == null)
                return;

            if (!_meshes.TryGetValue(geometry, out var meshData))
            {
                var name = string.IsNullOrEmpty(geometry.Name) || geometry == model ? model.Name : geometry.Name;
                var data = FbxMeshReader.Read(geometryNode, name);
                if (data == null)
                    return;
                meshData = data;
                _meshes[geometry] = meshData;
                _content.Add(meshData.Mesh);
            }

            var materials = new List<Material>();
            foreach (var link in model.ChildLinks)
            {
                if (link.Property == null && link.Child.Class == "Material")
                    materials.Add(GetMaterial(link.Child));
            }

            // Geometric transforms apply to the geometry only, so they go to a helper node and the children of the model are not affected.
            var geometric = new FbxNodeTransform
            {
                Translation = model.Props.GetVector3("GeometricTranslation", Vector3.Zero),
                Rotation = model.Props.GetVector3("GeometricRotation", Vector3.Zero),
                Scale = model.Props.GetVector3("GeometricScaling", Vector3.One),
                RotationActive = true,
            };
            bool hasGeometricTransform = geometric.Translation != Vector3.Zero || geometric.Rotation != Vector3.Zero || geometric.Scale != Vector3.One;

            var reference = new MeshReference(meshData.Mesh, materials);
            var skin = ReadSkin(geometry, meshData, node, hasGeometricTransform ? geometric.ToMatrix() : Matrix4x4.Identity);
            if (skin != null)
                reference.Skin = skin;

            if (hasGeometricTransform)
            {
                var helper = new Node(model.Name + "_geometry");
                helper.Transform = geometric.ToTransform();
                helper.Mesh = reference;
                node.AddChild(helper);
            }
            else
            {
                node.Mesh = reference;
            }

            foreach (var material in materials)
            {
                _content.Add(material);
                foreach (var texture in material.TextureParams.Values)
                    _content.Add(texture);
            }
        }

        // ---------------------------------------------------------------- Materials

        private Material GetMaterial(FbxObject obj)
        {
            if (_materials.TryGetValue(obj, out var existing))
                return existing;

            var props = obj.Props;
            var material = new Material(obj.Name);

            var baseColor = Vector4.One;
            var pbrColor = props.FindNameWithSuffix("baseColor");
            if (pbrColor != null && props.TryGetVector4(pbrColor, out var pbr))
            {
                baseColor = pbr;
            }
            else if (props.TryGetVector4("DiffuseColor", out var diffuse))
            {
                float factor = (float)props.GetDouble("DiffuseFactor", 1.0);
                baseColor = new Vector4(diffuse.X * factor, diffuse.Y * factor, diffuse.Z * factor, 1.0f);
            }

            double opacity = 1.0;
            if (props.Has("Opacity"))
                opacity = props.GetDouble("Opacity", 1.0);
            else if (props.Has("TransparencyFactor"))
                opacity = 1.0 - props.GetDouble("TransparencyFactor", 0.0);
            if (opacity < 1.0)
            {
                baseColor.W *= (float)Math.Max(0.0, opacity);
                material.AlphaMode = AlphaMode.Blend;
            }
            material.SetVector4(MaterialParam.BaseColor, baseColor);

            var emissiveName = props.FindNameWithSuffix("emissionColor");
            Vector4 emissive;
            bool hasEmissive;
            if (emissiveName != null)
            {
                hasEmissive = props.TryGetVector4(emissiveName, out emissive);
            }
            else
            {
                hasEmissive = props.TryGetVector4("EmissiveColor", out emissive);
                float factor = (float)props.GetDouble("EmissiveFactor", 1.0);
                emissive = new Vector4(emissive.X * factor, emissive.Y * factor, emissive.Z * factor, 1.0f);
            }
            if (hasEmissive && (emissive.X != 0.0f || emissive.Y != 0.0f || emissive.Z != 0.0f))
                material.SetVector4(MaterialParam.Emissive, new Vector4(emissive.X, emissive.Y, emissive.Z, 1.0f));

            var metalness = props.FindNameWithSuffix("metalness");
            if (metalness != null)
                material.SetScalar(MaterialParam.MetallicFactor, (float)props.GetDouble(metalness));
            var roughness = props.FindNameWithSuffix("roughness");
            if (roughness != null)
                material.SetScalar(MaterialParam.RoughnessFactor, (float)props.GetDouble(roughness));
            // Lambert and Phong materials describe a diffuse surface, not a metal: the roughness follows the Blinn-Phong exponent as in FBX2glTF.
            if (metalness == null)
                material.SetScalar(MaterialParam.MetallicFactor, 0.0f);
            if (roughness == null)
            {
                var shininess = props.Has("Shininess") ? "Shininess" : props.Has("ShininessExponent") ? "ShininessExponent" : null;
                float rough = 1.0f;
                if (shininess != null)
                    rough = (float)Math.Sqrt(2.0 / (2.0 + Math.Max(0.0, props.GetDouble(shininess, 0.0))));
                material.SetScalar(MaterialParam.RoughnessFactor, rough);
            }
            var doubleSided = props.FindNameWithSuffix("DoubleSided");
            if (doubleSided != null)
                material.DoubleSided = props.GetBool(doubleSided);

            foreach (var link in obj.ChildLinks)
            {
                if (link.Property == null || link.Child.Class != "Texture")
                    continue;
                var slot = GetTextureSlot(link.Property);
                if (slot == null || material.TextureParams.ContainsKey(slot))
                    continue;
                var texture = GetTexture(link.Child);
                if (texture != null)
                    material.SetTexture(slot, texture);
            }

            _materials[obj] = material;
            return material;
        }

        private static string? GetTextureSlot(string property)
        {
            int bar = property.LastIndexOf('|');
            var name = (bar >= 0 ? property.Substring(bar + 1) : property).ToLowerInvariant();
            switch (name)
            {
                case "diffusecolor":
                case "basecolor":
                case "basecolormap":
                case "color":
                    return MaterialParam.BaseColor;
                case "normalmap":
                case "normal":
                case "bump":
                case "normalcamera":
                    return MaterialParam.Normal;
                case "emissivecolor":
                case "emission":
                case "emissionmap":
                case "emissioncolor":
                    return MaterialParam.Emissive;
                case "ambientocclusionmap":
                case "ambientocclusion":
                case "occlusionmap":
                    return MaterialParam.Occlusion;
                case "specularcolor":
                    return MaterialParam.SpecularColor;
                default:
                    return null;
            }
        }

        private Texture? GetTexture(FbxObject obj)
        {
            if (_textures.TryGetValue(obj, out var existing))
                return existing;

            Texture? texture = null;
            FbxObject? video = null;
            foreach (var link in obj.ChildLinks)
            {
                if (link.Child.Class == "Video")
                {
                    video = link.Child;
                    break;
                }
            }

            // Embedded content has priority.
            var content = video?.Node.Child("Content");
            var embedded = content?.Properties.FirstOrDefault(p => p.Type == FbxPropertyType.Raw)?.Value as byte[];
            var relative = FirstNonEmpty(obj.Node.Child("RelativeFilename")?.GetString(0), video?.Node.Child("RelativeFilename")?.GetString(0));
            var absolute = FirstNonEmpty(obj.Node.Child("FileName")?.GetString(0), video?.Node.Child("Filename")?.GetString(0), video?.Node.Child("FileName")?.GetString(0));
            var fileName = GetLeafName(FirstNonEmpty(relative, absolute) ?? obj.Name);
            if (embedded != null && embedded.Length > 0)
            {
                texture = new Texture(fileName ?? "texture", embedded);
            }
            else
            {
                foreach (var candidate in new[] { relative, GetLeafName(relative), GetLeafName(absolute) })
                {
                    if (string.IsNullOrEmpty(candidate))
                        continue;
                    IFileSystemEntry? neighbour = null;
                    try
                    {
                        neighbour = _entry.GetNeigbourEntry(candidate!.Replace('\\', '/'));
                    }
                    catch (Exception)
                    {
                        // Invalid file names are skipped.
                    }
                    if (neighbour != null && neighbour.Exists)
                    {
                        texture = new Texture(neighbour);
                        break;
                    }
                }
            }

            if (texture != null)
            {
                // WrapModeU/V: 0 repeats, 1 clamps.
                var textureProps = obj.Props;
                if (textureProps.Has("WrapModeU") && textureProps.GetInt("WrapModeU", 0) == 1)
                    texture.WrapU = TextureWrap.Clamp;
                if (textureProps.Has("WrapModeV") && textureProps.GetInt("WrapModeV", 0) == 1)
                    texture.WrapV = TextureWrap.Clamp;
            }

            _textures[obj] = texture;
            return texture;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
                if (!string.IsNullOrEmpty(v))
                    return v;
            return null;
        }

        private static string? GetLeafName(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            int idx = path!.LastIndexOfAny(new[] { '/', '\\' });
            return idx >= 0 ? path.Substring(idx + 1) : path;
        }

        // ---------------------------------------------------------------- Skins

        /// <param name="geometry">The geometry object (or the model for legacy files) the skin deformer is connected to.</param>
        /// <param name="meshData">The mesh the skin is applied to.</param>
        /// <param name="geometryMatrix">
        /// The geometric transform of the model. The cluster matrices do not include it, so it has to be applied to the vertices before the bind pose matrices.
        /// </param>
        private Skin? ReadSkin(FbxObject geometry, FbxMeshData meshData, Node meshNode, Matrix4x4 geometryMatrix)
        {
            // Deformer objects are connected to the geometry. Legacy files connect them to the model, which is the geometry as well.
            FbxObject? skinObject = null;
            foreach (var link in geometry.ChildLinks)
            {
                if (link.Child.Class == "Deformer" && link.Child.SubClass == "Skin")
                {
                    skinObject = link.Child;
                    break;
                }
            }
            if (skinObject == null)
                return null;
            if (_skins.TryGetValue(skinObject, out var cached))
                return cached;

            var skin = new Skin(skinObject.Name);
            var perVertex = new List<(int joint, float weight)>?[meshData.ControlPointCount];
            foreach (var clusterLink in skinObject.ChildLinks)
            {
                var cluster = clusterLink.Child;
                if (cluster.Class != "Deformer" || cluster.SubClass != "Cluster")
                    continue;
                Node? joint = null;
                foreach (var link in cluster.ChildLinks)
                {
                    if (link.Child.Class == "Model" && _nodes.TryGetValue(link.Child, out var jointNode))
                    {
                        joint = jointNode;
                        break;
                    }
                }
                if (joint == null)
                    continue;

                int jointIndex = skin.Joints.Count;
                skin.Joints.Add(joint);
                // Like ufbx, the "Transform" matrix of the cluster is taken as the matrix from the mesh node space to the bone space at the bind pose.
                // When it is missing, it is computed from the bind pose of the bone ("TransformLink") and the mesh node.
                var transformNode = cluster.Node.Child("Transform");
                Matrix4x4 meshToBone;
                if (transformNode != null && transformNode.GetDoubles().Length >= 16)
                {
                    meshToBone = ReadMatrix(transformNode);
                }
                else
                {
                    Matrix4x4.Invert(ReadMatrix(cluster.Node.Child("TransformLink")), out var linkInverse);
                    meshToBone = GetWorldMatrix(meshNode) * linkInverse;
                }
                skin.InverseBindMatrices.Add(geometryMatrix * meshToBone);

                var indexes = cluster.Node.Child("Indexes")?.GetInts() ?? Array.Empty<int>();
                var weights = cluster.Node.Child("Weights")?.GetDoubles() ?? Array.Empty<double>();
                for (int i = 0; i < Math.Min(indexes.Length, weights.Length); ++i)
                {
                    int cp = indexes[i];
                    if (cp < 0 || cp >= perVertex.Length || weights[i] <= 0.0)
                        continue;
                    var list = perVertex[cp] ??= new List<(int, float)>(4);
                    list.Add((jointIndex, (float)weights[i]));
                }
            }

            if (skin.Joints.Count == 0)
            {
                _skins[skinObject] = null;
                return null;
            }

            var joints = new ListMeshVertexAttribute<Vector4>(perVertex.Length);
            var skinWeights = new ListMeshVertexAttribute<Vector4>(perVertex.Length);
            for (int v = 0; v < perVertex.Length; ++v)
            {
                var list = perVertex[v];
                var j = new float[4];
                var w = new float[4];
                if (list != null)
                {
                    list.Sort((a, b) => b.weight.CompareTo(a.weight));
                    int n = Math.Min(4, list.Count);
                    float sum = 0;
                    for (int k = 0; k < n; ++k)
                        sum += list[k].weight;
                    for (int k = 0; k < n; ++k)
                    {
                        j[k] = list[k].joint;
                        w[k] = list[k].weight / sum;
                    }
                }
                else
                {
                    w[0] = 1.0f;
                }
                joints.Add(new Vector4(j[0], j[1], j[2], j[3]));
                skinWeights.Add(new Vector4(w[0], w[1], w[2], w[3]));
            }
            meshData.Mesh.SetAttribute(MeshAttributeKey.Joints, joints, meshData.CornerControlPoints);
            meshData.Mesh.SetAttribute(MeshAttributeKey.Weights, skinWeights, meshData.CornerControlPoints);
            skin.Skeleton = FindCommonAncestor(skin.Joints);

            _content.Add(skin);
            _skins[skinObject] = skin;
            return skin;
        }

        /// <summary>Reads a 4x4 matrix stored as 16 doubles. FBX stores the column vector matrix column by column, which reads as the row vector matrix.</summary>
        private static Matrix4x4 ReadMatrix(FbxNode? node)
        {
            var m = node?.GetDoubles();
            if (m == null || m.Length < 16)
                return Matrix4x4.Identity;
            return new Matrix4x4(
                (float)m[0], (float)m[1], (float)m[2], (float)m[3],
                (float)m[4], (float)m[5], (float)m[6], (float)m[7],
                (float)m[8], (float)m[9], (float)m[10], (float)m[11],
                (float)m[12], (float)m[13], (float)m[14], (float)m[15]);
        }

        private static Matrix4x4 GetWorldMatrix(Node node)
        {
            var world = Matrix4x4.Identity;
            for (var n = node; n != null; n = n.Parent)
                world *= n.Transform.ToMatrix();
            return world;
        }

        private static Node? FindCommonAncestor(IList<Node> joints)
        {
            Node? candidate = joints[0];
            foreach (var joint in joints.Skip(1))
            {
                var ancestors = new HashSet<Node>();
                for (var n = joint; n != null; n = n.Parent)
                    ancestors.Add(n);
                while (candidate != null && !ancestors.Contains(candidate))
                    candidate = candidate.Parent;
                if (candidate == null)
                    return null;
            }
            return candidate is Scene ? null : candidate;
        }
    }
}
