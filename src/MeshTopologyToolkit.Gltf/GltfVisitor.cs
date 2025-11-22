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
                    _content.Meshes.Add(meshRef.Mesh);
            }

            foreach (var gltfMaterial in modelRoot.LogicalMaterials)
            {
                var material = VisitMaterial(gltfMaterial);
                if (material != null)
                    _content.Materials.Add(material);
            }

            foreach (var gltfTextures in modelRoot.LogicalTextures)
            {
                var texture = VisitTexture(gltfTextures);
                if (texture != null)
                    _content.Textures.Add(texture);
            }

            foreach (var sourceScene in modelRoot.LogicalScenes)
            {
                var scene = new Scene(sourceScene.Name);
                _content.Scenes.Add(scene);
                VisitVisualChildren(scene, sourceScene.VisualChildren);
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
            VisitVisualChildren(node, gltfNode.VisualChildren);

            if (gltfNode.Mesh != null)
            {
                node.Mesh = VisitMesh(gltfNode.Mesh);
            }

            return node;
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
            if (sourceMaterial == null)
                return null;

            if (_visitedMaterials.TryGetValue(sourceMaterial, out var material))
                return material;

            material = new Material() { Name = sourceMaterial.Name };
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
                                    throw new NotImplementedException($"{channel.Key}:{parameter.Name}");
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
                                    throw new NotImplementedException($"{channel.Key}:{parameter.Name}");
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
                                    throw new NotImplementedException($"{channel.Key}:{parameter.Name}");
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
                                    throw new NotImplementedException($"{channel.Key}:{parameter.Name}");
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
                                    throw new NotImplementedException($"{channel.Key}:{parameter.Name}");
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
                                    throw new NotImplementedException($"{channel.Key}:{parameter.Name}");
                                }
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException($"{channel.Key}");
                        break;
                }
            }


            _visitedMaterials.Add(sourceMaterial, material);
            return material;
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

            foreach (var prim in sourceMesh.Primitives)
            {
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
                IReadOnlyList<uint> indexAccessor = (primIndexAccessor != null)? primIndexAccessor.AsIndicesArray() : Enumerable.Range(0, prim.GetVertexAccessor("POSITION").Count).Select(_=>(uint)_).ToList();
                foreach (var primIndex in indexAccessor)
                {
                    foreach (var meshAdapter in meshAdapters)
                    {
                        meshAdapter.Value.AddValueByIndex(primIndex);
                    }
                    ++numIndices;

                }
                mesh.DrawCalls.Add(new MeshDrawCall(topology, pimStartIndex, numIndices- pimStartIndex));

            }

            _visitedMeshes.Add(sourceMesh, meshRef);
            return meshRef;
        }

        private Material VisitDefaultMaterial()
        {
            if (_defaultMaterial == null)
            {
                _defaultMaterial = new Material("Default");
                _content.Materials.Add(_defaultMaterial);
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

