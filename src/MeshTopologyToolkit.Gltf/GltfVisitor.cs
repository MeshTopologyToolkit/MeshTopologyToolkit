using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using GltfMaterial = SharpGLTF.Schema2.Material;
using GltfMesh = SharpGLTF.Schema2.Mesh;

namespace MeshTopologyToolkit.Gltf
{
    public class GltfVisitor
    {
        private FileContainer _content;

        private Dictionary<GltfMesh, MeshReference> _visitedMeshes = new Dictionary<GltfMesh, MeshReference>();
        private Dictionary<GltfMaterial, Material> _visitedMaterials = new Dictionary<GltfMaterial, Material>();

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
                _content.Meshes.Add(VisitMesh(mesh).Mesh);
            }
            foreach (var material in modelRoot.LogicalMaterials)
            {
                _content.Materials.Add(VisitMaterial(material));
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
                VisitMesh(gltfNode.Mesh);
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

        private Material VisitMaterial(GltfMaterial sourceMaterial)
        {
            if (sourceMaterial == null)
                return null;

            if (_visitedMaterials.TryGetValue(sourceMaterial, out var material))
                return material;

            material = new Material() { Name = sourceMaterial.Name };

            _visitedMaterials.Add(sourceMaterial, material);
            return material;
        }


        private MeshReference? VisitMesh(GltfMesh? sourceMesh)
        {
            if (sourceMesh == null)
                return null;

            if (_visitedMeshes.TryGetValue(sourceMesh, out var meshRef))
                return meshRef;

            var mesh = new UnifiedIndexedMesh() { Name = sourceMesh.Name };
            meshRef = new MeshReference(mesh);

            int primIndex = 0;
            foreach (var prim in sourceMesh.Primitives)
            {
                var startIndex = mesh.Indices.Count;
                meshRef.Materials.Add(VisitMaterial(prim.Material));
                var indexAccessor = prim.GetIndexAccessor();
                if (indexAccessor != null)
                {
                    var indices = indexAccessor.AsIndicesArray();
                    foreach (var index in indices)
                    {
                        mesh.Indices.Add((int)index);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
                mesh.DrawCalls.Add(new MeshDrawCall(VisitTopology(prim.DrawPrimitiveType), startIndex, mesh.Indices.Count));
                primIndex++;
            }

            _visitedMeshes.Add(sourceMesh, meshRef);
            return meshRef;
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

