using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using GltfMaterial = SharpGLTF.Schema2.Material;
using GltfMesh = SharpGLTF.Schema2.Mesh;
using GltfTexture = SharpGLTF.Schema2.Texture;

namespace MeshTopologyToolkit.Gltf
{
    public class GltfVisitor
    {
        public Material? _defaultMaterial;

        private FileContainer _content;

        private Dictionary<GltfMesh, MeshReference> _visitedMeshes = new Dictionary<GltfMesh, MeshReference>();
        private Dictionary<GltfMaterial, Material> _visitedMaterials = new Dictionary<GltfMaterial, Material>();
        private Dictionary<GltfTexture, Texture> _visitedTextures = new Dictionary<GltfTexture, Texture>();
        private Dictionary<SharpGLTF.Schema2.Node, Node> _visitedNodes = new Dictionary<SharpGLTF.Schema2.Node, Node>();
        private Dictionary<SharpGLTF.Schema2.Skin, Skin> _visitedSkins = new Dictionary<SharpGLTF.Schema2.Skin, Skin>();
        private List<(Node Node, SharpGLTF.Schema2.Skin Skin)> _skinnedNodes = new List<(Node, SharpGLTF.Schema2.Skin)>();

        public GltfVisitor(FileContainer content)
        {
            _content = content;
        }

        public void Visit(SharpGLTF.Schema2.ModelRoot? modelRoot)
        {
            if (modelRoot == null)
                return;

            foreach (var mesh in modelRoot.LogicalMeshes)
            {
                var meshRef = VisitMesh(mesh);
                if (meshRef?.Mesh != null)
                    _content.Add(meshRef.Mesh);
            }

            foreach (var gltfMaterial in modelRoot.LogicalMaterials)
            {
                var material = VisitMaterial(gltfMaterial);
                if (material != null)
                    _content.Add(material);
            }

            foreach (var gltfTextures in modelRoot.LogicalTextures)
            {
                var texture = VisitTexture(gltfTextures);
                if (texture != null)
                    _content.Add(texture);
            }

            foreach (var sourceScene in modelRoot.LogicalScenes)
            {
                var scene = new Scene(sourceScene.Name);
                _content.Add(scene);
                VisitVisualChildren(scene, sourceScene.VisualChildren);
            }

            foreach (var (node, gltfSkin) in _skinnedNodes)
            {
                node.Mesh!.Skin = VisitSkin(gltfSkin);
            }

            foreach (var gltfSkin in modelRoot.LogicalSkins)
            {
                _content.Add(VisitSkin(gltfSkin));
            }

            foreach (var gltfAnimation in modelRoot.LogicalAnimations)
            {
                _content.Add(VisitAnimation(gltfAnimation));
            }
        }

        private void VisitVisualChildren(Node parent, IEnumerable<SharpGLTF.Schema2.Node> visualChildren)
        {
            foreach (var child in visualChildren)
            {
                parent.AddChild(VisitNode(child));
            }
        }

        private Node VisitNode(SharpGLTF.Schema2.Node gltfNode)
        {
            var node = new Node(gltfNode.Name) { Transform = VisitTransform(gltfNode.LocalTransform) };
            _visitedNodes[gltfNode] = node;
            VisitVisualChildren(node, gltfNode.VisualChildren);

            if (gltfNode.Mesh != null)
            {
                node.Mesh = VisitMesh(gltfNode.Mesh);
                if (gltfNode.Skin != null && node.Mesh != null)
                {
                    // The mesh reference is shared between nodes, but a skin belongs to the node.
                    node.Mesh = new MeshReference(node.Mesh.Mesh, node.Mesh.Materials);
                    _skinnedNodes.Add((node, gltfNode.Skin));
                }
            }

            return node;
        }

        /// <summary>
        /// Gets a node that was already visited. Nodes that are not a part of any scene are created on demand.
        /// </summary>
        private Node GetNode(SharpGLTF.Schema2.Node gltfNode)
        {
            if (_visitedNodes.TryGetValue(gltfNode, out var node))
                return node;

            node = new Node(gltfNode.Name) { Transform = VisitTransform(gltfNode.LocalTransform) };
            _visitedNodes[gltfNode] = node;
            if (gltfNode.VisualParent != null)
                GetNode(gltfNode.VisualParent).AddChild(node);
            return node;
        }

        private Skin VisitSkin(SharpGLTF.Schema2.Skin gltfSkin)
        {
            if (_visitedSkins.TryGetValue(gltfSkin, out var skin))
                return skin;

            skin = new Skin(gltfSkin.Name);
            for (int i = 0; i < gltfSkin.JointsCount; ++i)
            {
                var (joint, inverseBindMatrix) = gltfSkin.GetJoint(i);
                skin.Joints.Add(GetNode(joint));
                skin.InverseBindMatrices.Add(inverseBindMatrix);
            }
            if (gltfSkin.Skeleton != null)
                skin.Skeleton = GetNode(gltfSkin.Skeleton);
            _visitedSkins.Add(gltfSkin, skin);
            return skin;
        }

        private Animation VisitAnimation(SharpGLTF.Schema2.Animation gltfAnimation)
        {
            var animation = new Animation(gltfAnimation.Name);
            // Morph target weight and animation pointer channels are not supported and are skipped.
            var targets = gltfAnimation.Channels
                .Where(c => c.TargetNode != null && (c.TargetNodePath == PropertyPath.translation || c.TargetNodePath == PropertyPath.rotation || c.TargetNodePath == PropertyPath.scale))
                .Select(c => c.TargetNode)
                .Distinct();
            foreach (var gltfNode in targets)
            {
                var samplers = gltfNode.GetCurveSamplers(gltfAnimation);
                var track = new AnimationTrack(GetNode(gltfNode))
                {
                    Translation = VisitCurve(samplers.Translation, AnimationCurve.CreateVector3),
                    Rotation = VisitCurve(samplers.Rotation, AnimationCurve.CreateQuaternion),
                    Scale = VisitCurve(samplers.Scale, AnimationCurve.CreateVector3),
                };
                if (track.Translation != null || track.Rotation != null || track.Scale != null)
                    animation.Tracks.Add(track);
            }
            return animation;
        }

        private AnimationCurve<T>? VisitCurve<T>(IAnimationSampler<T>? sampler, Func<AnimationInterpolation, AnimationCurve<T>> createCurve) where T : struct
        {
            if (sampler == null)
                return null;

            AnimationCurve<T> curve;
            switch (sampler.InterpolationMode)
            {
                case AnimationInterpolationMode.STEP:
                    curve = createCurve(AnimationInterpolation.Step);
                    foreach (var (time, value) in sampler.GetLinearKeys())
                        curve.Add(time, value);
                    break;
                case AnimationInterpolationMode.CUBICSPLINE:
                    curve = createCurve(AnimationInterpolation.CubicSpline);
                    foreach (var (time, (inTangent, value, outTangent)) in sampler.GetCubicKeys())
                        curve.Add(time, value, inTangent, outTangent);
                    break;
                default:
                    curve = createCurve(AnimationInterpolation.Linear);
                    foreach (var (time, value) in sampler.GetLinearKeys())
                        curve.Add(time, value);
                    break;
            }
            return curve.Keys.Count > 0 ? curve : null;
        }

        private ITransform VisitTransform(AffineTransform localTransform)
        {
            if (localTransform.IsSRT)
                return new TRSTransform(localTransform.Translation, localTransform.Rotation, localTransform.Scale);
            if (localTransform.IsMatrix)
                return new MatrixTransform(localTransform.Matrix);
            throw new NotImplementedException();
        }

        private Material? VisitMaterial(GltfMaterial sourceMaterial)
        {
            bool throwOnUnknownProperties = true;

            if (sourceMaterial == null)
                return null;

            if (_visitedMaterials.TryGetValue(sourceMaterial, out var material))
                return material;

            material = new Material()
            {
                Name = sourceMaterial.Name,
                AlphaMode = VisitAlphaMode(sourceMaterial.Alpha),
                AlphaCutoff = sourceMaterial.AlphaCutoff,
                DoubleSided = sourceMaterial.DoubleSided,
            };
            foreach (MaterialChannel channel in sourceMaterial.Channels)
            {
                switch (channel.Key)
                {
                    case "BaseColor":
                        {
                            material.SetVector4(MaterialParam.BaseColor, channel.Color);
                            if (channel.Texture != null)
                            {
                                material.SetTexture(MaterialParam.BaseColor, VisitTexture(channel.Texture));
                            }
                            foreach (var parameter in channel.Parameters)
                            {
                                if (parameter.Name == "RGBA" && parameter.Value is Vector4 color)
                                {
                                    material.SetVector4(MaterialParam.BaseColor, color);
                                }
                                else
                                {
                                    if (throwOnUnknownProperties)
                                        throw new NotImplementedException($"{channel.Key}:{parameter.Name} of {parameter.Value?.GetType()?.Name}");
                                }
                            }
                        }
                        break;
                    case "Normal":
                        {
                            if (channel.Texture != null)
                            {
                                material.SetTexture(MaterialParam.Normal, VisitTexture(channel.Texture));
                            }
                            foreach (var parameter in channel.Parameters)
                            {
                                if (parameter.Name == "NormalScale" && parameter.Value is float normalScale)
                                {
                                    material.SetScalar(MaterialParam.NormalScale, normalScale);
                                }
                                else
                                {
                                    if (throwOnUnknownProperties)
                                        throw new NotImplementedException($"{channel.Key}:{parameter.Name} of {parameter.Value?.GetType()?.Name}");
                                }
                            }
                        }
                        break;
                    case "MetallicRoughness":
                        {
                            foreach (var parameter in channel.Parameters)
                            {
                                if (parameter.Name == "MetallicFactor" && parameter.Value is float metallicFactor)
                                {
                                    material.SetScalar(MaterialParam.MetallicFactor, metallicFactor);
                                }
                                else if (parameter.Name == "RoughnessFactor" && parameter.Value is float roughnessFactor)
                                {
                                    material.SetScalar(MaterialParam.RoughnessFactor, roughnessFactor);
                                }
                                else
                                {
                                    if (throwOnUnknownProperties)
                                        throw new NotImplementedException($"{channel.Key}:{parameter.Name} of {parameter.Value?.GetType()?.Name}");
                                }
                            }
                        }
                        break;
                    case "Occlusion":
                        {
                            foreach (var parameter in channel.Parameters)
                            {
                                if (parameter.Name == "OcclusionStrength" && parameter.Value is float occlusionStrength)
                                {
                                    material.SetScalar(MaterialParam.OcclusionStrength, occlusionStrength);
                                }
                                else
                                {
                                    if (throwOnUnknownProperties)
                                        throw new NotImplementedException($"{channel.Key}:{parameter.Name} of {parameter.Value?.GetType()?.Name}");
                                }
                            }
                        }
                        break;
                    case "Emissive":
                        {
                            foreach (var parameter in channel.Parameters)
                            {
                                if (parameter.Name == "RGB" && parameter.Value is Vector3 color)
                                {
                                    material.SetVector4(MaterialParam.Emissive, new Vector4(color, 1.0f));
                                }
                                else if (parameter.Name == "EmissiveStrength" && parameter.Value is float emissiveStrength)
                                {
                                    material.SetScalar(MaterialParam.EmissiveStrength, emissiveStrength);
                                }
                                else
                                {
                                    if (throwOnUnknownProperties)
                                        throw new NotImplementedException($"{channel.Key}:{parameter.Name} of {parameter.Value?.GetType()?.Name}");
                                }
                            }
                        }
                        break;
                    case "SpecularColor":
                        {
                            foreach (var parameter in channel.Parameters)
                            {
                                if (parameter.Name == "RGB" && parameter.Value is Vector3 color)
                                {
                                    material.SetVector4(MaterialParam.SpecularColor, new Vector4(color, 1.0f));
                                }
                                else
                                {
                                    if (throwOnUnknownProperties)
                                        throw new NotImplementedException($"{channel.Key}:{parameter.Name} of {parameter.Value?.GetType()?.Name}");
                                }
                            }
                        }
                        break;
                    case "SpecularFactor":
                        {
                            foreach (var parameter in channel.Parameters)
                            {
                                if (parameter.Name == "SpecularFactor" && parameter.Value is float specularFactor)
                                {
                                    material.SetScalar(MaterialParam.SpecularFactor, specularFactor);
                                }
                                else
                                {
                                    if (throwOnUnknownProperties)
                                        throw new NotImplementedException($"{channel.Key}:{parameter.Name} of {parameter.Value?.GetType()?.Name}");
                                }
                            }
                        }
                        break;
                    default:
                        if (throwOnUnknownProperties)
                            throw new NotImplementedException($"{channel.Key}");
                        else
                            break;
                }
            }


            _visitedMaterials.Add(sourceMaterial, material);
            return material;
        }

        private static AlphaMode VisitAlphaMode(SharpGLTF.Schema2.AlphaMode mode)
        {
            switch (mode)
            {
                case SharpGLTF.Schema2.AlphaMode.MASK:
                    return AlphaMode.Mask;
                case SharpGLTF.Schema2.AlphaMode.BLEND:
                    return AlphaMode.Blend;
                default:
                    return AlphaMode.Opaque;
            }
        }

        private Texture? VisitTexture(GltfTexture gltfTexture)
        {
            if (gltfTexture == null)
                return null;

            if (_visitedTextures.TryGetValue(gltfTexture, out var texture))
                return texture;

            texture = new Texture(new InMemoryFileSystemEntry(gltfTexture.Name ?? $"{gltfTexture.LogicalIndex}.png", gltfTexture.PrimaryImage.Content.Content));

            _visitedTextures.Add(gltfTexture, texture);
            return texture;
        }

        private MeshReference? VisitMesh(GltfMesh? sourceMesh)
        {
            if (sourceMesh == null)
                return null;

            if (_visitedMeshes.TryGetValue(sourceMesh, out var meshRef))
                return meshRef;

            var mesh = new SeparatedIndexedMesh() { Name = sourceMesh.Name };

            meshRef = new MeshReference(mesh);

            Dictionary<string, AccessorAdapter> meshAdapters = new Dictionary<string, AccessorAdapter>();

            int numIndices = 0;

            for (int primitiveIndex = 0; primitiveIndex < sourceMesh.Primitives.Count; primitiveIndex++)
            {
                MeshPrimitive? prim = sourceMesh.Primitives[primitiveIndex];
                if (prim.Material != null)
                {
                    meshRef.Materials.Add(VisitMaterial(prim.Material)!);
                }
                else
                {
                    meshRef.Materials.Add(VisitDefaultMaterial());
                }
                var topology = VisitTopology(prim.DrawPrimitiveType);

                var primAdapters = new Dictionary<string, AccessorAdapter>();

                foreach (var accessor in prim.VertexAccessors)
                {
                    var adapter = new AccessorAdapter(accessor.Key, accessor.Value);
                    primAdapters[adapter.OriginalKey] = adapter;
                    if (meshAdapters.TryGetValue(adapter.OriginalKey, out var existingAdapter))
                    {
                        if (existingAdapter.Accessor.Format != accessor.Value.Format)
                        {
                            throw new NotImplementedException("Inconsistent mesh attributes are not supported yet.");
                        }
                        adapter.MeshVertexAttribute = existingAdapter.MeshVertexAttribute;
                        adapter.MeshVertexAttributeIndices = existingAdapter.MeshVertexAttributeIndices;
                        meshAdapters[adapter.OriginalKey] = adapter;
                    }
                    else
                    {
                        if (numIndices > 0)
                        {
                            throw new NotImplementedException("Inconsistent mesh attributes are not supported yet.");
                        }
                        adapter.CreateMeshVertexAttribute();
                        mesh.AddAttribute(adapter.AttributeKey, adapter.MeshVertexAttribute!, adapter.MeshVertexAttributeIndices!);
                        meshAdapters[adapter.OriginalKey] = adapter;
                    }
                }

                foreach (var accessor in meshAdapters)
                {
                    if (!primAdapters.ContainsKey(accessor.Key))
                    {
                        accessor.Value.SwitchToDefaultAdapter();
                    }
                }

                int pimStartIndex = numIndices;
                var primIndexAccessor = prim.GetIndexAccessor();
                IReadOnlyList<uint> indexAccessor = (primIndexAccessor != null) ? primIndexAccessor.AsIndicesArray() : Enumerable.Range(0, prim.GetVertexAccessor("POSITION").Count).Select(_ => (uint)_).ToList();
                foreach (var primIndex in indexAccessor)
                {
                    foreach (var meshAdapter in meshAdapters)
                    {
                        meshAdapter.Value.AddValueByIndex(primIndex);
                    }
                    ++numIndices;

                }
                mesh.DrawCalls.Add(new MeshDrawCall(0, primitiveIndex, topology, pimStartIndex, numIndices - pimStartIndex));

            }

            _visitedMeshes.Add(sourceMesh, meshRef);
            return meshRef;
        }

        private Material VisitDefaultMaterial()
        {
            if (_defaultMaterial == null)
            {
                _defaultMaterial = new Material("Default");
                _content.Add(_defaultMaterial);
            }

            return _defaultMaterial;
        }

        private MeshTopology VisitTopology(PrimitiveType drawPrimitiveType)
        {
            switch (drawPrimitiveType)
            {
                case PrimitiveType.POINTS:
                    return MeshTopology.Points;
                case PrimitiveType.TRIANGLES:
                    return MeshTopology.TriangleList;
                case PrimitiveType.TRIANGLE_STRIP:
                    return MeshTopology.TriangleStrip;
                case PrimitiveType.TRIANGLE_FAN:
                    return MeshTopology.TriangleFan;
                case PrimitiveType.LINES:
                    return MeshTopology.LineList;
                case PrimitiveType.LINE_LOOP:
                    return MeshTopology.LineLoop;
                case PrimitiveType.LINE_STRIP:
                    return MeshTopology.LineStrip;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

