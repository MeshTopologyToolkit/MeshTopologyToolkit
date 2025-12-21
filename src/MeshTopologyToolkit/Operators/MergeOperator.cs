using System;
using System.Collections.Generic;
using System.Linq;

namespace MeshTopologyToolkit.Operators
{
    public class MergeOperator : IContentOperator
    {
        HashSet<Texture> _visitedTextures = new HashSet<Texture>();

        HashSet<Material> _visitedMaterials = new HashSet<Material>();

        FileContainer? _container;

        public FileContainer Transform(FileContainer container)
        {
            
            var result = new FileContainer();

            if (_container != null)
            {
                throw new NotImplementedException($"{this.GetType().Name} is not designed to be reused");
            }
            _container = result;

            foreach (var scene in container.Scenes)
            {
                _container.Add(Transform(scene));
            }

            _container = null;

            return result;
        }

        public Scene Transform(Scene scene)
        {
            Scene resultScene = new Scene(scene.Name);
            Node resultNode = new Node();
            resultScene.AddChild(resultNode);
            resultNode.Mesh = Merge(scene.VisitAllChildren().Where(n => n.Mesh?.Mesh != null).ToList());
            return resultScene;
        }

        private MeshReference Merge(List<Node> nodes)
        {
            Material? defaultMaterial = null;
            // TODO: This is incorrect. MeshAndTransform should also contain material mapping. 
            var materials = new DictionaryMeshVertexAttribute<Material>();
            foreach (var m in nodes.SelectMany(n => n.Mesh!.Materials))
            {
                var mat = m ?? defaultMaterial ?? (defaultMaterial = new Material("Default"));
                materials.Add(mat);
                _container!.Add(mat);
            }

            var mesh = UnifiedIndexedMesh.Merge(nodes.Select(node => new UnifiedIndexedMesh.MeshAndTransform { Mesh = node.Mesh!.Mesh.AsUnified(), Transform = node.GetWorldSpaceTransform().ToMatrix() }).ToList());
            var res = new MeshReference(mesh, materials);
            _container!.Add(mesh);

            return res;
        }

        public IMesh Transform(IMesh mesh)
        {
            throw new NotImplementedException($"{this.GetType().Name} is not designed to operate on individual meshes");
        }

        public Material Transform(Material material)
        {
            if (_container == null)
            {
                throw new NotImplementedException($"{this.GetType().Name} is not designed to operate on individual materials");
            }
            return Visit(_visitedMaterials, material, _container.Materials);
        }

        public Texture Transform(Texture texture)
        {
            if (_container == null)
            {
                throw new NotImplementedException($"{this.GetType().Name} is not designed to operate on individual textures");
            }
            return Visit(_visitedTextures, texture, _container.Textures);
        }

        private T Visit<T>(HashSet<T> cache, T value, IList<T> collection)
        {
            if (value == null)
                return value;

            if (cache.Add(value))
            {
                collection.Add(value);
            }

            return value;

        }
    }
}
