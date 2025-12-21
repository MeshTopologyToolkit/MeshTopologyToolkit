using MeshTopologyToolkit.Ase;
using MeshTopologyToolkit.Collada;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Obj;
using MeshTopologyToolkit.Operators;
using MeshTopologyToolkit.Stl;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class CommandBase
    {
        public static readonly FileFormatCollection FileFormats = new FileFormatCollection(
            new AseFileFormat(),
            new ColladaFileFormat(),
            new GltfFileFormat(),
            new ObjFileFormat(),
            new StlFileFormat()
            );

        public static bool SaveOutputModel(FileContainer container, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            switch (ext)
            {
                case ".dae":
                    container = new SpaceTransformOperator(new SpaceTransform(Matrix4x4.Identity, flipV: true)).Transform(container);
                    return new ColladaFileFormat(ColladaUpAxis.Y).TryWrite(fileName, container);
            }

            if (!FileFormats.TryWrite(fileName, container))
            {
                Console.Error.WriteLine($"Failed to save file {fileName}");
                return false;
            }
            return true;
        }

    }
}