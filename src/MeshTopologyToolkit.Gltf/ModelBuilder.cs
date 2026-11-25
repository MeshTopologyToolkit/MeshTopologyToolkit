using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    internal class ModelBuilder
    {
        MaterialBuilder _defaultMaterial = new MaterialBuilder("Default");
        Dictionary<Material, MaterialBuilder> _materials = new Dictionary<Material, MaterialBuilder>();
        Dictionary<Node, NodeBuilder> _nodeBuilders = new Dictionary<Node, NodeBuilder>();
        List<(SceneBuilder Scene, Node Node, NodeBuilder Builder)> _meshNodes = new List<(SceneBuilder, Node, NodeBuilder)>();

        public ModelBuilder()
        {
        }

        internal ModelRoot BuildModel(FileContainer content)
        {
            SceneBuilderSchema2Settings settings = new SceneBuilderSchema2Settings();
            var sceneBuilders = content.Scenes.Select(_ => VisitScene(_)).ToList();

            // Meshes are added when all the nodes exist, because skins refer to nodes that can be anywhere in the hierarchy.
            foreach (var (sceneBuilder, node, nodeBuilder) in _meshNodes)
            {
                AddMesh(sceneBuilder, node, nodeBuilder);
            }

            foreach (var animation in content.Animations)
            {
                VisitAnimation(animation);
            }

            if (sceneBuilders.Count > 0)
                return SceneBuilder.ToGltf2(sceneBuilders, settings);
            return ModelRoot.CreateModel();
        }

        private void AddMesh(SceneBuilder sceneBuilder, Node node, NodeBuilder nodeBuilder)
        {
            var meshReference = node.Mesh!;
            var skin = meshReference.Skin;
            var unified = meshReference.Mesh.AsUnified();
            bool skinned = skin != null && skin.Joints.Count > 0 && unified.HasAttribute(MeshAttributeKey.Joints) && unified.HasAttribute(MeshAttributeKey.Weights);

            var meshBuilder = VisitMesh(meshReference, skinned);
            if (skinned)
            {
                var joints = new (NodeBuilder Joint, Matrix4x4 InverseBindMatrix)[skin!.Joints.Count];
                for (int i = 0; i < joints.Length; i++)
                {
                    var inverseBindMatrix = i < skin.InverseBindMatrices.Count ? skin.InverseBindMatrices[i] : Matrix4x4.Identity;
                    joints[i] = (GetNodeBuilder(skin.Joints[i]), inverseBindMatrix);
                }
                EnsureUniqueJointNames(joints.Select(_ => _.Joint));
                sceneBuilder.AddSkinnedMesh(meshBuilder, joints);
            }
            else
            {
                sceneBuilder.AddRigidMesh(meshBuilder, nodeBuilder);
            }
        }

        /// <summary>
        /// SharpGLTF only accepts skins with uniquely named joints (many files have unnamed or duplicated joint names).
        /// Duplicates get a numeric suffix.
        /// </summary>
        private static void EnsureUniqueJointNames(IEnumerable<NodeBuilder> joints)
        {
            var used = new HashSet<string>();
            foreach (var joint in joints)
            {
                var name = joint.Name ?? "Node";
                if (!used.Add(name))
                {
                    int suffix = 1;
                    string candidate;
                    do
                    {
                        candidate = name + "_" + suffix++;
                    }
                    while (!used.Add(candidate));
                    joint.Name = candidate;
                }
            }
        }

        /// <summary>
        /// Gets a builder of a node. Nodes that are not reachable from a scene (like skin joints) get a builder on demand.
        /// </summary>
        private NodeBuilder GetNodeBuilder(Node node)
        {
            if (_nodeBuilders.TryGetValue(node, out var nodeBuilder))
                return nodeBuilder;

            nodeBuilder = new NodeBuilder() { Name = node.Name ?? "Node" };
            nodeBuilder.LocalTransform = VisitTransform(node.Transform);
            _nodeBuilders.Add(node, nodeBuilder);
            if (node.Parent != null && !(node.Parent is Scene))
                GetNodeBuilder(node.Parent).AddNode(nodeBuilder);
            return nodeBuilder;
        }

        private void VisitAnimation(Animation animation)
        {
            var name = animation.Name ?? "Animation";
            foreach (var track in animation.Tracks)
            {
                if (track.Target == null || !_nodeBuilders.TryGetValue(track.Target, out var nodeBuilder))
                    continue;

                if (track.Translation != null && track.Translation.Keys.Count > 0)
                    VisitCurve(track.Translation, nodeBuilder.UseTranslation(name));
                if (track.Rotation != null && track.Rotation.Keys.Count > 0)
                    VisitCurve(track.Rotation, nodeBuilder.UseRotation(name));
                if (track.Scale != null && track.Scale.Keys.Count > 0)
                    VisitCurve(track.Scale, nodeBuilder.UseScale(name));
            }
        }

        private static void VisitCurve<T>(AnimationCurve<T> curve, SharpGLTF.Animations.CurveBuilder<T> curveBuilder) where T : struct
        {
            foreach (var key in curve.Keys)
            {
                curveBuilder.SetPoint(key.Time, key.Value, curve.Interpolation != AnimationInterpolation.Step);
                if (curve.Interpolation == AnimationInterpolation.CubicSpline)
                {
                    curveBuilder.SetIncomingTangent(key.Time, key.InTangent);
                    curveBuilder.SetOutgoingTangent(key.Time, key.OutTangent);
                }
            }
        }

        private SceneBuilder VisitScene(Scene scene)
        {
            var sceneBuilder = new SceneBuilder();
            sceneBuilder.Name = scene.Name ?? "Scene";
            foreach (var child in scene.Children)
            {
                var childNodeBuilder = VisitNode(sceneBuilder, child);
                sceneBuilder.AddNode(childNodeBuilder);
            }

            return sceneBuilder;
        }

        private NodeBuilder VisitNode(SceneBuilder sceneBuilder, Node node)
        {
            var nodeBuilder = GetNodeBuilder(node);

            if (node.Mesh?.Mesh != null)
            {
                _meshNodes.Add((sceneBuilder, node, nodeBuilder));
            }

            foreach (var child in node.Children)
            {
                var childNodeBuilder = VisitNode(sceneBuilder, child);
                if (childNodeBuilder.Parent != nodeBuilder)
                    nodeBuilder.AddNode(childNodeBuilder);
            }
            return nodeBuilder;
        }

        private IMeshBuilder<MaterialBuilder> VisitMesh(MeshReference mesh, bool skinned)
        {
            var materials = mesh.Materials.Select(VisitMaterial).ToList();
            while (materials.Count < mesh.Mesh.DrawCalls.Count)
            {
                materials.Add(VisitMaterial(null));
            }
            var meshBuilder = new CustomMeshBuilder(mesh.Mesh.Name ?? "Mesh", mesh.Mesh.AsUnified(), materials, skinned);
            return meshBuilder;
        }

        private MaterialBuilder VisitMaterial(Material? material)
        {
            if (material == null)
            {
                return _defaultMaterial;
            }
            if (_materials.TryGetValue(material, out var materialBuilder))
            {
                return materialBuilder;
            }
            materialBuilder = new MaterialBuilder(material.Name ?? "Material");
            materialBuilder = materialBuilder.WithMetallicRoughnessShader();
            materialBuilder.WithDoubleSide(material.DoubleSided);
            switch (material.AlphaMode)
            {
                case AlphaMode.Mask:
                    materialBuilder.WithAlpha(SharpGLTF.Materials.AlphaMode.MASK, material.AlphaCutoff);
                    break;
                case AlphaMode.Blend:
                    materialBuilder.WithAlpha(SharpGLTF.Materials.AlphaMode.BLEND);
                    break;
            }
            foreach (var scalarParam in material.ScalarParams)
            {
                switch (scalarParam.Key)
                {
                    case MaterialParam.MetallicFactor:
                        materialBuilder.WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.MetallicFactor, scalarParam.Value);
                        break;
                    case MaterialParam.RoughnessFactor:
                        materialBuilder.WithChannelParam(KnownChannel.MetallicRoughness, KnownProperty.RoughnessFactor, scalarParam.Value);
                        break;
                    case MaterialParam.NormalScale:
                        materialBuilder.WithChannelParam(KnownChannel.Normal, KnownProperty.NormalScale, scalarParam.Value);
                        break;
                    case MaterialParam.OcclusionStrength:
                        if (material.TextureParams.ContainsKey(MaterialParam.Occlusion))
                            materialBuilder.WithChannelParam(KnownChannel.Occlusion, KnownProperty.OcclusionStrength, scalarParam.Value);
                        break;
                    case MaterialParam.EmissiveStrength:
                        if (material.TextureParams.ContainsKey(MaterialParam.Emissive))
                            materialBuilder.WithChannelParam(KnownChannel.Emissive, KnownProperty.EmissiveStrength, scalarParam.Value);
                        break;
                    case MaterialParam.SpecularFactor:
                        materialBuilder.WithChannelParam(KnownChannel.SpecularFactor, KnownProperty.SpecularFactor, scalarParam.Value);
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled scalar material parameter: {scalarParam.Key}");
                }
            }
            foreach (var vec4Param in material.Vector4Params)
            {
                switch (vec4Param.Key)
                {
                    case MaterialParam.BaseColor:
                        materialBuilder.WithChannelParam(KnownChannel.BaseColor, KnownProperty.RGBA, vec4Param.Value);
                        break;
                    case MaterialParam.Emissive:
                        materialBuilder.WithChannelParam(KnownChannel.Emissive, KnownProperty.RGB, new Vector3(vec4Param.Value.X, vec4Param.Value.Y, vec4Param.Value.Z));
                        break;
                    case MaterialParam.SpecularColor:
                        materialBuilder.WithChannelParam(KnownChannel.SpecularColor, KnownProperty.RGB, new Vector3(vec4Param.Value.X, vec4Param.Value.Y, vec4Param.Value.Z));
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled vec4 material parameter: {vec4Param.Key}");
                }
            }
            foreach (var texParam in material.TextureParams)
            {
                switch (texParam.Key)
                {
                    case MaterialParam.BaseColor:
                        materialBuilder.WithChannelImage(KnownChannel.BaseColor, VisitTexture(texParam.Value));
                        break;
                    case MaterialParam.SpecularColor:
                        materialBuilder.WithChannelImage(KnownChannel.SpecularColor, VisitTexture(texParam.Value));
                        break;
                    case MaterialParam.Emissive:
                        materialBuilder.WithChannelImage(KnownChannel.Emissive, VisitTexture(texParam.Value));
                        break;
                    case MaterialParam.Occlusion:
                        materialBuilder.WithChannelImage(KnownChannel.Occlusion, VisitTexture(texParam.Value));
                        break;
                    case MaterialParam.Normal:
                        materialBuilder.WithChannelImage(KnownChannel.Normal, VisitTexture(texParam.Value));
                        break;
                    case MaterialParam.MetallicRoughness:
                        materialBuilder.WithChannelImage(KnownChannel.MetallicRoughness, VisitTexture(texParam.Value));
                        break;
                    default:
                        throw new NotImplementedException($"Unhandled texture material parameter: {texParam.Key}");
                }
            }
            return materialBuilder;
        }

        private ImageBuilder? VisitTexture(Texture? texture)
        {
            if (texture?.FileSystemEntry == null)
            {
                return null;
            }

            var imageBuilder = ImageBuilder.From(texture.FileSystemEntry.ReadAllBytes(), Path.GetFileName(texture.FileSystemEntry.Name));
            return imageBuilder;
        }

        private AffineTransform VisitTransform(ITransform transform)
        {
            if (transform is TRSTransform trs)
            {
                return new AffineTransform(trs.Scale, trs.Rotation, trs.Translation);
            }
            return new AffineTransform(transform.ToMatrix());

        }
    }
}