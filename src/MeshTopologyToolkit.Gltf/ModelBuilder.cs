using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System.Linq;

namespace MeshTopologyToolkit.Gltf
{
    internal class ModelBuilder
    {
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
                return new MaterialBuilder("DefaultMaterial");
            }
            return new MaterialBuilder(material.Name ?? "Material");
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