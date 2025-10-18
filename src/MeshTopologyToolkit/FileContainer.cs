namespace MeshTopologyToolkit
{
    public class FileContainer
    {
        public IList<IMesh> Meshes { get; } = new List<IMesh>();

        public IList<Scene> Scenes { get; } = new List<Scene>();
    }
}
