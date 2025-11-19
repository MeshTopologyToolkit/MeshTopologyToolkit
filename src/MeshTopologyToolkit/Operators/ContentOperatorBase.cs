using System;
using System.Collections.Generic;

namespace MeshTopologyToolkit.Operators
{
    public class ContentOperatorBase: IContentOperator
    {
        Dictionary<Texture, Texture> _visitedTextures = new Dictionary<Texture, Texture>();

        Dictionary<IMesh, IMesh> _visitedMeshes = new Dictionary<IMesh, IMesh>();

        Dictionary<Material, Material> _visitedMaterials = new Dictionary<Material, Material>();

        public ContentOperatorBase()
        {
        }

        public FileContainer Transform(FileContainer container)
        {
            var result = new FileContainer();
            foreach (var asset in container.Textures)
            {
                result.Textures.Add(Visit(asset));
            }

            foreach (var asset in container.Materials)
            {
                result.Materials.Add(Visit(asset));
            }

            foreach (var asset in container.Meshes)
            {
                result.Meshes.Add(Visit(asset));
            }

            foreach (var asset in container.Scenes)
            {
                result.Scenes.Add(Tramsform(asset));
            }

            return result;
        }

        protected Texture Visit(Texture texture) => Visit(_visitedTextures, texture, Visit);

        protected IMesh Visit(IMesh mesh) => Visit(_visitedMeshes, mesh, Visit);

        protected Material Visit(Material material) => Visit(_visitedMaterials, material, Visit);

        private T Visit<T>(Dictionary<T, T> cache, T value, Func<T,T> transform)
        {
            if (value == null)
                return value;

            if (!cache.TryGetValue(value, out var result))
            {
                result = transform(result);
                cache.Add(value, result);
            }
            return result;

        }

        public virtual Texture Transform(Texture texture) => texture;

        public virtual Material Material(Material material) => material;

        public virtual ITransform Transform(ITransform transform) => transform;

        public virtual MeshReference? Transform(MeshReference? meshReference)
        {
            if (meshReference == null)
                return null;
            var result = new MeshReference(Visit(meshReference.Mesh));
            return result;
        }

        public virtual IMesh? Tramsform(IMesh? mesh) => mesh;

        public virtual Node Tramsform(Node node)
        {
            var result = new Node();
            result.Name = node.Name;
            result.Transform = Transform(node.Transform);
            result.Mesh = Transform(node.Mesh);
            foreach (var child in node.Children)
            {
                result.AddChild(Tramsform(child));
            }
            return result;
        }

        public virtual Scene Tramsform(Scene scene)
        {
            var result = new Scene();
            result.Name = scene.Name;
            foreach (var child in scene.Children)
            {
                result.AddChild(Tramsform(child));
            }
            return result;
        }
    }
}
