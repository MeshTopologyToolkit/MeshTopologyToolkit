using System;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class FileContainer
    {
        public IList<IMesh> Meshes { get; } = new List<IMesh>();

        public IList<Material> Materials { get; } = new List<Material>();

        public IList<Scene> Scenes { get; } = new List<Scene>();

        public IList<Texture> Textures { get; } = new List<Texture>();

        public MeshReference AddSingleMeshScene(IMesh mesh)
        {
            Meshes.Add(mesh);
            var scene = new Scene(mesh.Name);
            var node = new Node(mesh.Name);
            node.Mesh = new MeshReference(mesh);
            scene.AddChild(node);
            Scenes.Add(scene);
            return node.Mesh;
        }
    }
}
