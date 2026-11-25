using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Converts a <see cref="FileContainer"/> to an <see cref="FbxDocument"/> (FBX 7.4).
    /// The content is expected to be in the space of <see cref="FbxFileFormat.FormatToGltfTransform"/>:
    /// Y up, Z front, centimeters and V axis pointing up.
    /// </summary>
    internal sealed partial class FbxSceneWriter
    {
        private readonly FileContainer _content;

        private readonly FbxDocument _doc = new FbxDocument { Version = 7400, IsBinary = true };
        private readonly FbxNode _objects = new FbxNode("Objects");
        private readonly FbxNode _connections = new FbxNode("Connections");
        private readonly Dictionary<string, int> _objectCounts = new Dictionary<string, int>();

        private long _nextId = 1000000;
        private readonly Dictionary<Node, long> _nodeIds = new Dictionary<Node, long>();
        private readonly Dictionary<IMesh, long> _geometryIds = new Dictionary<IMesh, long>();
        private readonly Dictionary<Material, long> _materialIds = new Dictionary<Material, long>();
        private readonly Dictionary<Texture, long> _textureIds = new Dictionary<Texture, long>();
        private readonly HashSet<Node> _joints = new HashSet<Node>();
        private long _defaultMaterialId;

        public FbxSceneWriter(FileContainer content)
        {
            _content = content;
        }

        // ---------------------------------------------------------------- Helpers

        private static FbxProperty S(string value) => FbxProperty.FromString(value);
        private static FbxProperty D(double value) => FbxProperty.FromDouble(value);
        private static FbxProperty I(int value) => FbxProperty.FromInt(value);
        private static FbxProperty L(long value) => FbxProperty.FromLong(value);

        private long NewId() => _nextId++;

        private static string ObjectName(string? name, string cls) => (name ?? "") + "\0\u0001" + cls;

        private void Count(string type) => _objectCounts[type] = _objectCounts.TryGetValue(type, out var n) ? n + 1 : 1;

        private FbxNode AddObject(string type, long id, string? name, string subClass, string? className = null)
        {
            Count(type);
            if (className == null)
            {
                switch (type)
                {
                    case "AnimationStack": className = "AnimStack"; break;
                    case "AnimationLayer": className = "AnimLayer"; break;
                    case "AnimationCurveNode": className = "AnimCurveNode"; break;
                    case "AnimationCurve": className = "AnimCurve"; break;
                    default: className = type; break;
                }
            }
            var node = _objects.AddChild(type, L(id), S(ObjectName(name, className)), S(subClass));
            return node;
        }

        private void Connect(long child, long parent)
        {
            _connections.AddChild("C", S("OO"), L(child), L(parent));
        }

        private void ConnectProperty(long child, long parent, string property)
        {
            _connections.AddChild("C", S("OP"), L(child), L(parent), S(property));
        }

        private static FbxNode Properties(FbxNode owner)
        {
            return owner.AddChild("Properties70");
        }

        private static void AddProperty(FbxNode properties, string name, string type, string label, string flags, params FbxProperty[] values)
        {
            var p = properties.AddChild("P", S(name), S(type), S(label), S(flags));
            p.Properties.AddRange(values);
        }

        private static void AddVector(FbxNode properties, string name, string type, Vector3 v, string flags = "A")
        {
            AddProperty(properties, name, type, "", flags, D(v.X), D(v.Y), D(v.Z));
        }

        // ---------------------------------------------------------------- Build

        public FbxDocument Build()
        {
            WriteHeader();

            var rootNodes = new List<Node>();
            foreach (var scene in _content.Scenes)
                rootNodes.AddRange(scene.Children);

            // Find the joints first, they become limb nodes.
            foreach (var node in EnumerateNodes(rootNodes))
            {
                if (node.Mesh?.Skin != null)
                    foreach (var joint in node.Mesh.Skin.Joints)
                        _joints.Add(joint);
            }
            foreach (var skin in _content.Skins)
                foreach (var joint in skin.Joints)
                    _joints.Add(joint);

            foreach (var node in EnumerateNodes(rootNodes))
                _nodeIds[node] = NewId();

            foreach (var root in rootNodes)
                WriteModelTree(root, 0);

            WriteSkins(rootNodes);
            WriteAnimations(rootNodes);

            WriteDefinitions();
            _doc.Roots.Add(_objects);
            _doc.Roots.Add(_connections);
            var takes = new FbxNode("Takes");
            takes.AddChild("Current", S(""));
            _doc.Roots.Add(takes);
            return _doc;
        }

        private static IEnumerable<Node> EnumerateNodes(IEnumerable<Node> roots)
        {
            foreach (var root in roots)
            {
                yield return root;
                foreach (var child in root.VisitAllChildren())
                    yield return child;
            }
        }

        private void WriteHeader()
        {
            var header = new FbxNode("FBXHeaderExtension");
            header.AddChild("FBXHeaderVersion", I(1003));
            header.AddChild("FBXVersion", I(7400));
            header.AddChild("EncryptionType", I(0));
            var stamp = header.AddChild("CreationTimeStamp");
            var now = DateTime.UtcNow;
            stamp.AddChild("Version", I(1000));
            stamp.AddChild("Year", I(now.Year));
            stamp.AddChild("Month", I(now.Month));
            stamp.AddChild("Day", I(now.Day));
            stamp.AddChild("Hour", I(now.Hour));
            stamp.AddChild("Minute", I(now.Minute));
            stamp.AddChild("Second", I(now.Second));
            stamp.AddChild("Millisecond", I(now.Millisecond));
            header.AddChild("Creator", S("MeshTopologyToolkit.Fbx"));
            var sceneInfo = header.AddChild("SceneInfo", S(ObjectName("GlobalInfo", "SceneInfo")), S("UserData"));
            sceneInfo.AddChild("Type", S("UserData"));
            sceneInfo.AddChild("Version", I(100));
            var meta = sceneInfo.AddChild("MetaData");
            meta.AddChild("Version", I(100));
            foreach (var field in new[] { "Title", "Subject", "Author", "Keywords", "Revision", "Comment" })
                meta.AddChild(field, S(""));
            var sceneProps = Properties(sceneInfo);
            AddProperty(sceneProps, "DocumentUrl", "KString", "Url", "", S("scene.fbx"));
            AddProperty(sceneProps, "SrcDocumentUrl", "KString", "Url", "", S("scene.fbx"));
            AddProperty(sceneProps, "Original", "Compound", "", "");
            AddProperty(sceneProps, "Original|ApplicationVendor", "KString", "", "", S("MeshTopologyToolkit"));
            AddProperty(sceneProps, "Original|ApplicationName", "KString", "", "", S("MeshTopologyToolkit.Fbx"));
            AddProperty(sceneProps, "LastSaved", "Compound", "", "");
            AddProperty(sceneProps, "LastSaved|ApplicationVendor", "KString", "", "", S("MeshTopologyToolkit"));
            AddProperty(sceneProps, "LastSaved|ApplicationName", "KString", "", "", S("MeshTopologyToolkit.Fbx"));
            _doc.Roots.Add(header);

            _doc.Roots.Add(new FbxNode("FileId") { Properties = { FbxProperty.FromRaw(new byte[] { 0x28, 0xb3, 0x2a, 0xeb, 0xb6, 0x24, 0xcc, 0xc2, 0xbf, 0xc8, 0xb0, 0x2a, 0xa9, 0x2b, 0xfc, 0xf1 }) } });
            _doc.Roots.Add(new FbxNode("CreationTime") { Properties = { S(now.ToString("yyyy-MM-dd HH:mm:ss:fff", CultureInfo.InvariantCulture)) } });
            _doc.Roots.Add(new FbxNode("Creator") { Properties = { S("MeshTopologyToolkit.Fbx") } });

            var settings = new FbxNode("GlobalSettings");
            settings.AddChild("Version", I(1000));
            var props = Properties(settings);
            AddProperty(props, "UpAxis", "int", "Integer", "", I(1));
            AddProperty(props, "UpAxisSign", "int", "Integer", "", I(1));
            AddProperty(props, "FrontAxis", "int", "Integer", "", I(2));
            AddProperty(props, "FrontAxisSign", "int", "Integer", "", I(1));
            AddProperty(props, "CoordAxis", "int", "Integer", "", I(0));
            AddProperty(props, "CoordAxisSign", "int", "Integer", "", I(1));
            AddProperty(props, "OriginalUpAxis", "int", "Integer", "", I(-1));
            AddProperty(props, "OriginalUpAxisSign", "int", "Integer", "", I(1));
            AddProperty(props, "UnitScaleFactor", "double", "Number", "", D(1.0));
            AddProperty(props, "OriginalUnitScaleFactor", "double", "Number", "", D(1.0));
            AddProperty(props, "AmbientColor", "ColorRGB", "Color", "", D(0), D(0), D(0));
            AddProperty(props, "DefaultCamera", "KString", "", "", S("Producer Perspective"));
            AddProperty(props, "TimeMode", "enum", "", "", I(0));
            AddProperty(props, "TimeProtocol", "enum", "", "", I(2));
            AddProperty(props, "SnapOnFrameMode", "enum", "", "", I(0));
            AddProperty(props, "TimeSpanStart", "KTime", "Time", "", L(0));
            AddProperty(props, "TimeSpanStop", "KTime", "Time", "", L(ToTicks(_content.Animations.Count == 0 ? 0.0f : _content.Animations.Max(a => a.EndTime))));
            AddProperty(props, "CustomFrameRate", "double", "Number", "", D(-1));
            AddProperty(props, "TimeMarker", "Compound", "", "");
            AddProperty(props, "CurrentTimeMarker", "int", "Integer", "", I(-1));
            _doc.Roots.Add(settings);

            var documents = new FbxNode("Documents");
            documents.AddChild("Count", I(1));
            var document = documents.AddChild("Document", L(NewId()), S(""), S("Scene"));
            var documentProps = Properties(document);
            AddProperty(documentProps, "SourceObject", "object", "", "");
            AddProperty(documentProps, "ActiveAnimStackName", "KString", "", "", S(_content.Animations.Count > 0 ? _content.Animations[0].Name ?? "Take 001" : ""));
            document.AddChild("RootNode", L(0));
            _doc.Roots.Add(documents);
            _doc.Roots.Add(new FbxNode("References"));
        }

        private void WriteDefinitions()
        {
            var definitions = new FbxNode("Definitions");
            definitions.AddChild("Version", I(100));
            definitions.AddChild("Count", I(_objectCounts.Values.Sum() + 1));
            var global = definitions.AddChild("ObjectType", S("GlobalSettings"));
            global.AddChild("Count", I(1));
            foreach (var kv in _objectCounts.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                var type = definitions.AddChild("ObjectType", S(kv.Key));
                type.AddChild("Count", I(kv.Value));
            }
            // Definitions go before the objects.
            int objectsIndex = _doc.Roots.FindIndex(n => n.Name == "References") + 1;
            _doc.Roots.Insert(objectsIndex, definitions);
        }

        // ---------------------------------------------------------------- Models

        private void WriteModelTree(Node node, long parentId)
        {
            long id = _nodeIds[node];
            bool isJoint = _joints.Contains(node);
            string subClass = isJoint ? "LimbNode" : node.Mesh?.Mesh != null ? "Mesh" : "Null";
            var model = AddObject("Model", id, node.Name ?? "Node", subClass);
            model.AddChild("Version", I(232));

            var props = Properties(model);
            var trs = node.Transform.ToTRS();
            AddProperty(props, "InheritType", "enum", "", "", I(1));
            AddProperty(props, "DefaultAttributeIndex", "int", "Integer", "", I(0));
            AddVector(props, "Lcl Translation", "Lcl Translation", trs.Translation);
            AddVector(props, "Lcl Rotation", "Lcl Rotation", QuaternionToEuler(trs.Rotation));
            AddVector(props, "Lcl Scaling", "Lcl Scaling", trs.Scale);
            model.AddChild("Shading", FbxProperty.FromBool(true));
            model.AddChild("Culling", S("CullingOff"));

            Connect(id, parentId);

            if (isJoint)
            {
                long attributeId = NewId();
                var attribute = AddObject("NodeAttribute", attributeId, node.Name ?? "Node", "LimbNode");
                attribute.AddChild("TypeFlags", S("Skeleton"));
                Connect(attributeId, id);
            }

            if (node.Mesh?.Mesh != null)
                WriteMeshInstance(node.Mesh, id);

            foreach (var child in node.Children)
                WriteModelTree(child, id);
        }

        /// <summary>Quaternion to Euler angles in degrees for the XYZ rotation order, the inverse of <see cref="FbxNodeTransform.EulerToMatrix"/>.</summary>
        internal static Vector3 QuaternionToEuler(Quaternion q)
        {
            q = Quaternion.Normalize(q);
            var m = Matrix4x4.CreateFromQuaternion(q);
            // The row vector matrix is the transpose of the column vector one: R = Rz * Ry * Rx.
            double r20 = m.M13, r21 = m.M23, r22 = m.M33, r10 = m.M12, r00 = m.M11;
            double x, y, z;
            if (Math.Abs(r20) < 1.0 - 1e-6)
            {
                y = -Math.Asin(r20);
                x = Math.Atan2(r21, r22);
                z = Math.Atan2(r10, r00);
            }
            else
            {
                // Gimbal lock: put everything to X.
                y = r20 > 0 ? -Math.PI / 2 : Math.PI / 2;
                x = Math.Atan2(-m.M32, m.M22);
                z = 0.0;
            }
            const double ToDeg = 180.0 / Math.PI;
            return new Vector3((float)(x * ToDeg), (float)(y * ToDeg), (float)(z * ToDeg));
        }

        // ---------------------------------------------------------------- Meshes and materials

        private void WriteMeshInstance(MeshReference reference, long modelId)
        {
            var mesh = reference.Mesh;
            if (!_geometryIds.TryGetValue(mesh, out var geometryId))
            {
                geometryId = WriteGeometry(mesh);
                _geometryIds[mesh] = geometryId;
            }
            if (geometryId != 0)
                Connect(geometryId, modelId);

            foreach (var material in reference.Materials)
            {
                if (material == null)
                {
                    if (_defaultMaterialId == 0)
                    {
                        _defaultMaterialId = NewId();
                        WriteMaterial(new Material("Default", new Vector4(0.8f, 0.8f, 0.8f, 1.0f)), _defaultMaterialId);
                    }
                    Connect(_defaultMaterialId, modelId);
                    continue;
                }
                if (!_materialIds.TryGetValue(material, out var materialId))
                {
                    materialId = NewId();
                    _materialIds[material] = materialId;
                    WriteMaterial(material, materialId);
                }
                Connect(materialId, modelId);
            }
        }

        private void WriteMaterial(Material material, long id)
        {
            var node = AddObject("Material", id, material.Name ?? "Material", "");
            node.AddChild("Version", I(102));
            node.AddChild("ShadingModel", S("phong"));
            node.AddChild("MultiLayer", I(0));
            var props = Properties(node);
            AddProperty(props, "ShadingModel", "KString", "", "", S("phong"));

            var baseColor = material.Vector4Params.TryGetValue(MaterialParam.BaseColor, out var c) ? c : Vector4.One;
            AddProperty(props, "DiffuseColor", "Color", "", "A", D(baseColor.X), D(baseColor.Y), D(baseColor.Z));
            AddProperty(props, "DiffuseFactor", "Number", "", "A", D(1.0));
            AddProperty(props, "TransparencyFactor", "Number", "", "A", D(1.0 - baseColor.W));
            AddProperty(props, "Opacity", "Number", "", "A", D(baseColor.W));
            if (material.Vector4Params.TryGetValue(MaterialParam.Emissive, out var emissive))
            {
                AddProperty(props, "EmissiveColor", "Color", "", "A", D(emissive.X), D(emissive.Y), D(emissive.Z));
                AddProperty(props, "EmissiveFactor", "Number", "", "A", D(1.0));
            }
            if (material.ScalarParams.TryGetValue(MaterialParam.MetallicFactor, out var metallic))
                AddProperty(props, "metalness", "Number", "", "A", D(metallic));
            if (material.ScalarParams.TryGetValue(MaterialParam.RoughnessFactor, out var roughness))
                AddProperty(props, "roughness", "Number", "", "A", D(roughness));

            foreach (var kv in material.TextureParams)
            {
                var property = GetTextureProperty(kv.Key);
                if (property == null || kv.Value.FileSystemEntry == null)
                    continue;
                if (!_textureIds.TryGetValue(kv.Value, out var textureId))
                {
                    textureId = NewId();
                    _textureIds[kv.Value] = textureId;
                    WriteTexture(kv.Value, textureId);
                }
                ConnectProperty(textureId, id, property);
            }
        }

        private static string? GetTextureProperty(string slot)
        {
            switch (slot)
            {
                case MaterialParam.BaseColor: return "DiffuseColor";
                case MaterialParam.Normal: return "NormalMap";
                case MaterialParam.Emissive: return "EmissiveColor";
                case MaterialParam.Occlusion: return "AmbientOcclusion";
                case MaterialParam.SpecularColor: return "SpecularColor";
                default: return null;
            }
        }

        private void WriteTexture(Texture texture, long id)
        {
            var entry = texture.FileSystemEntry!;
            var fileName = entry.Name ?? "texture";
            int slash = fileName.LastIndexOfAny(new[] { '/', '\\' });
            var leaf = slash >= 0 ? fileName.Substring(slash + 1) : fileName;

            var node = AddObject("Texture", id, leaf, "");
            node.AddChild("Type", S("TextureVideoClip"));
            node.AddChild("Version", I(202));
            node.AddChild("TextureName", S(ObjectName(leaf, "Texture")));
            var props = Properties(node);
            AddProperty(props, "UseMaterial", "bool", "", "", I(1));
            node.AddChild("Media", S(ObjectName(leaf, "Video")));
            node.AddChild("FileName", S(fileName));
            node.AddChild("RelativeFilename", S(leaf));
            node.AddChild("ModelUVTranslation", D(0), D(0));
            node.AddChild("ModelUVScaling", D(1), D(1));
            node.AddChild("Texture_Alpha_Source", S("None"));
            node.AddChild("Cropping", I(0), I(0), I(0), I(0));

            long videoId = NewId();
            var video = AddObject("Video", videoId, leaf, "Clip");
            video.AddChild("Type", S("Clip"));
            var videoProps = Properties(video);
            AddProperty(videoProps, "Path", "KString", "XRefUrl", "", S(fileName));
            video.AddChild("UseMipMap", I(0));
            video.AddChild("Filename", S(fileName));
            video.AddChild("RelativeFilename", S(leaf));
            var bytes = entry.ReadAllBytes();
            if (bytes != null && bytes.Length > 0)
                video.AddChild("Content", FbxProperty.FromRaw(bytes));
            Connect(videoId, id);
        }

        private static long ToTicks(double seconds) => (long)Math.Round(seconds * FbxCurve.TicksPerSecond);
    }
}
