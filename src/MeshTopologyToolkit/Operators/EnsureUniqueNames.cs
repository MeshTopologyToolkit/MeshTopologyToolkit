using System.Collections.Generic;

namespace MeshTopologyToolkit.Operators
{
    public class EnsureUniqueNames: ContentOperatorBase
    {
        HashSet<string> _existingNames = new HashSet<string>();

        Material? _defaultMaterial = null;
        

        public override Scene Transform(Scene scene)
        {
            scene = base.Transform(scene);
            scene.Name = GetUniqueName(_existingNames, string.IsNullOrEmpty(scene.Name) ? "Scene" : scene.Name);
            return scene;
        }

        public override Node Transform(Node node)
        {
            node = base.Transform(node);
            node.Name = GetUniqueName(_existingNames, string.IsNullOrEmpty(node.Name) ? "Node" : node.Name);
            return node;
        }


        public override Texture Transform(Texture texture)
        {
            if (texture?.FileSystemEntry != null)
            {
                //texture.FileSystemEntry.Name = GetUniqueName(existingNames, string.IsNullOrEmpty(texture.FileSystemEntry.Name) ? "Texture" : texture.FileSystemEntry.Name);
            }
            return texture;
        }

        public override Material Transform(Material material)
        {
            if (material == null)
                return (_defaultMaterial ?? (_defaultMaterial = new Material() { Name = GetUniqueName(_existingNames, "DefaultMaterial") }));
            material.Name = GetUniqueName(_existingNames, string.IsNullOrEmpty(material.Name) ? "Material" : material.Name);
            return material;
        }

        public override IMesh Transform(IMesh mesh)
        {
            mesh.Name = GetUniqueName(_existingNames, string.IsNullOrEmpty(mesh.Name)? "Mesh": mesh.Name);
            return mesh;
        }

        private string GetUniqueName(HashSet<string> existingNames, string baseName)
        {
            if (existingNames.Add(baseName))
                return baseName;
            for (int i=0; ; ++i)
            {
                var newName = $"{baseName}_{i}";
                if (existingNames.Add(newName))
                    return newName;
            }
        }
    }
}
