using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Operators
{
    public class ContentOperatorBase : IContentOperator
    {
        Dictionary<Texture, Texture> _visitedTextures = new Dictionary<Texture, Texture>();

        Dictionary<IMesh, IMesh> _visitedMeshes = new Dictionary<IMesh, IMesh>();

        Dictionary<Material, Material> _visitedMaterials = new Dictionary<Material, Material>();

        Dictionary<Skin, Skin> _visitedSkins = new Dictionary<Skin, Skin>();

        Dictionary<Node, Node> _visitedNodes = new Dictionary<Node, Node>();

        List<(MeshReference Result, Skin Source)> _pendingSkins = new List<(MeshReference, Skin)>();

        public ContentOperatorBase()
        {
        }

        /// <summary>
        /// Gets the result node for a source node that was already transformed, or the node itself if it was not visited.
        /// </summary>
        protected Node? MapNode(Node? source)
        {
            if (source == null)
                return null;
            return _visitedNodes.TryGetValue(source, out var result) ? result : source;
        }

        public FileContainer Transform(FileContainer container)
        {
            var result = new FileContainer() { FileToGltfTransform = container.FileToGltfTransform };
            foreach (var asset in container.Textures)
            {
                result.Add(Visit(asset));
            }

            foreach (var asset in container.Materials)
            {
                result.Add(Visit(asset));
            }

            foreach (var asset in container.Meshes)
            {
                result.Add(Visit(asset));
            }

            foreach (var asset in container.Scenes)
            {
                result.Add(Transform(asset));
            }

            // Skins refer to nodes, so they can only be resolved when all the scenes are transformed.
            foreach (var asset in container.Skins)
            {
                result.Add(Visit(asset));
            }

            foreach (var pending in _pendingSkins)
            {
                pending.Result.Skin = Visit(pending.Source);
                result.Add(pending.Result.Skin);
            }
            _pendingSkins.Clear();

            foreach (var asset in container.Animations)
            {
                result.Add(Transform(asset));
            }

            return result;
        }

        protected Skin Visit(Skin skin) => Visit(_visitedSkins, skin, Transform);

        public virtual Skin Transform(Skin skin)
        {
            var result = new Skin(skin.Name);
            foreach (var joint in skin.Joints)
            {
                result.Joints.Add(MapNode(joint)!);
            }
            foreach (var matrix in skin.InverseBindMatrices)
            {
                result.InverseBindMatrices.Add(TransformInverseBindMatrix(matrix));
            }
            result.Skeleton = MapNode(skin.Skeleton);
            return result;
        }

        protected virtual Matrix4x4 TransformInverseBindMatrix(Matrix4x4 matrix) => matrix;

        public virtual Animation Transform(Animation animation)
        {
            var result = new Animation(animation.Name);
            foreach (var track in animation.Tracks)
            {
                result.Tracks.Add(Transform(track));
            }
            return result;
        }

        public virtual AnimationTrack Transform(AnimationTrack track)
        {
            return new AnimationTrack(MapNode(track.Target), track.TargetName)
            {
                Translation = track.Translation,
                Rotation = track.Rotation,
                Scale = track.Scale,
            };
        }

        protected Texture Visit(Texture texture) => Visit(_visitedTextures, texture, Transform);

        protected IMesh Visit(IMesh mesh) => Visit(_visitedMeshes, mesh, Transform);

        protected Material Visit(Material material) => Visit(_visitedMaterials, material, Transform);

        private T Visit<T>(Dictionary<T, T> cache, T value, Func<T, T> transform)
        {
            if (value == null)
                return value;

            if (!cache.TryGetValue(value, out var result))
            {
                result = transform(value);
                cache.Add(value, result);
            }
            return result;

        }

        public virtual Texture Transform(Texture texture) => texture;

        public virtual Material Transform(Material material) => material;

        public virtual ITransform Transform(ITransform transform) => transform;

        public virtual MeshReference? Transform(MeshReference? meshReference)
        {
            if (meshReference == null)
                return null;
            var result = new MeshReference(Visit(meshReference.Mesh));
            foreach (var material in meshReference.Materials)
            {
                result.Materials.Add(Visit(material));
            }
            if (meshReference.Skin != null)
            {
                _pendingSkins.Add((result, meshReference.Skin));
            }
            return result;
        }

        public virtual IMesh Transform(IMesh mesh) => mesh;

        public virtual Node Transform(Node node)
        {
            var result = new Node();
            _visitedNodes[node] = result;
            result.Name = node.Name;
            result.Transform = Transform(node.Transform);
            result.Mesh = Transform(node.Mesh);
            foreach (var child in node.Children)
            {
                result.AddChild(Transform(child));
            }
            return result;
        }

        public virtual Scene Transform(Scene scene)
        {
            var result = new Scene();
            _visitedNodes[scene] = result;
            result.Name = scene.Name;
            // The root can carry a transform and a mesh of its own, like any other node.
            result.Transform = Transform(scene.Transform);
            result.Mesh = Transform(scene.Mesh);
            foreach (var child in scene.Children)
            {
                result.AddChild(Transform(child));
            }
            return result;
        }
    }
}
