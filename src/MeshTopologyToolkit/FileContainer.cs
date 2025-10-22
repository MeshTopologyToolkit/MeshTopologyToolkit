using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class FileContainer
    {
        public IList<IMesh> Meshes { get; } = new List<IMesh>();

        public IList<Material> Materials { get; } = new List<Material>();

        public IList<Scene> Scenes { get; } = new List<Scene>();
    }
}
