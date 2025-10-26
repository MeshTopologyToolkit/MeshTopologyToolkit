using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.Linq;
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
                var meshRef = VisitMesh(mesh);
                if (meshRef?.Mesh != null)
                    _content.Meshes.Add(meshRef.Mesh);
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

            var mesh = new SeparatedIndexedMesh() { Name = sourceMesh.Name };

            meshRef = new MeshReference(mesh);

            Dictionary<string, AccessorAdapter> meshAdapters = new Dictionary<string, AccessorAdapter>();
            
            int numIndices = 0;

            foreach (var prim in sourceMesh.Primitives)
            {
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
                        throw new NotImplementedException("Inconsistent mesh attributes are not supported yet.");
                    }
                }

                int pimStartIndex = numIndices;
                var indexAccessor = prim.GetIndexAccessor().AsIndicesArray();
                foreach (var primIndex in indexAccessor)
                {
                    foreach (var meshAdapter in meshAdapters)
                    {
                        meshAdapter.Value.AddValueByIndex(primIndex);
                    }
                    ++numIndices;

                }
                mesh.DrawCalls.Add(new MeshDrawCall(topology, pimStartIndex, numIndices- pimStartIndex));


                //if (mesh.HasAttribute(MeshAttributeKey.Position))
                //{
                //    foreach (var accessor in prim.VertexAccessors)
                //    {
                //        if (!mesh.HasAttribute(VisitAccessorKey(accessor.Key)))
                //            throw new FormatException("Inconsistent set of attributes");
                //    }
                //}
                //else
                //{
                //    foreach (var accessor in prim.VertexAccessors)
                //    {
                //        mesh.AddAttribute(VisitAccessorKey(accessor.Key), VisitAccessor(accessor));
                //    }
                //}


                //var startIndex = mesh.Indices.Count;
                //meshRef.Materials.Add(VisitMaterial(prim.Material));
                //var indexAccessor = prim.GetIndexAccessor();
                //if (indexAccessor != null)
                //{
                //    var indices = indexAccessor.AsIndicesArray();
                //    foreach (var index in indices)
                //    {
                //        mesh.Indices.Add((int)index);
                //    }
                //}
                //else
                //{
                //    throw new NotImplementedException();
                //}
                //mesh.DrawCalls.Add(new MeshDrawCall(VisitTopology(prim.DrawPrimitiveType), startIndex, mesh.Indices.Count));
                //primIndex++;
            }

            _visitedMeshes.Add(sourceMesh, meshRef);
            return meshRef;
        }

        //private (IMeshVertexAttribute) VisitAccessor(Accessor accessor)
        //{
        //    switch (accessor.Dimensions)
        //    {
        //        case DimensionType.SCALAR:
        //            {
        //                switch (accessor.EncodingType)
        //                {
        //                    default:
        //                        {
        //                            var array = accessor.AsScalarArray();
        //                            var buf = new DictionaryMeshVertexAttribute<float>();
        //                            return (buf, (int index)=> );
        //                        }
        //                }
        //            }
        //            return new Dictionary<float>
        //    }
        //}

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

