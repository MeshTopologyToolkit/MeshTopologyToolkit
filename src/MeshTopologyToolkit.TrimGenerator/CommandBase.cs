using MeshTopologyToolkit.Collada;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Obj;
using MeshTopologyToolkit.Stl;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class CommandBase
    {
        public static readonly FileFormatCollection FileFormats = new FileFormatCollection(
            new GltfFileFormat(), 
            new ObjFileFormat(),
            new ColladaFileFormat(),
            new StlFileFormat());
    }
}