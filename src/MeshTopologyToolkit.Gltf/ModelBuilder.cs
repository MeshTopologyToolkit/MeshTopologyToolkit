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

        public ModelBuilder()
        {
        }

        internal ModelRoot BuildModel(FileContainer content)
        {
            SceneBuilderSchema2Settings settings = new SceneBuilderSchema2Settings();
            var sceneBuilders = content.Scenes.Select(_ => VisitScene(_)).ToList();

            if (sceneBuilders.Count > 0)
                return SceneBuilder.ToGltf2(sceneBuilders, settings);
            return ModelRoot.CreateModel();
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
            var nodeBuilder = new NodeBuilder() { Name = node.Name ?? "Node" };
            nodeBuilder.LocalTransform = VisitTransform(node.Transform);
            
            if (node.Mesh?.Mesh != null)
            {
                var meshBuilder = VisitMesh(node.Mesh);
                sceneBuilder.AddRigidMesh(meshBuilder, nodeBuilder);
            }

            foreach (var child in node.Children)
            {
                var childNodeBuilder = VisitNode(sceneBuilder, child);
                nodeBuilder.AddNode(childNodeBuilder);
            }
            return nodeBuilder;
        }

        private IMeshBuilder<MaterialBuilder> VisitMesh(MeshReference mesh)
        {
            var materials = mesh.Materials.Select(VisitMaterial).ToList();
            while (materials.Count < mesh.Mesh.DrawCalls.Count)
            {
                materials.Add(VisitMaterial(null));
            }
            var meshBuilder = new CustomMeshBuilder(mesh.Mesh.Name ?? "Mesh", mesh.Mesh.AsUnified(), materials);
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
            foreach (var scalarParam in material.ScalarParams)
            {
                switch (scalarParam.Key) {
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
                        materialBuilder.WithChannelParam(KnownChannel.Occlusion, KnownProperty.OcclusionStrength, scalarParam.Value);
                        break;
                    case MaterialParam.EmissiveStrength:
                        materialBuilder.WithChannelParam(KnownChannel.Emissive, KnownProperty.EmissiveStrength, scalarParam.Value);
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
                    case MaterialParam.Normal:
                        materialBuilder.WithChannelImage(KnownChannel.Normal, VisitTexture(texParam.Value));
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