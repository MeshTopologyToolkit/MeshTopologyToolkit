using Avalonia;
using Cocona;
using MeshTopologyToolkit.Ase;
using MeshTopologyToolkit.Collada;
using MeshTopologyToolkit.Gltf;
using MeshTopologyToolkit.Stl;
using MeshTopologyToolkit.Urho3D;

namespace MeshTopologyToolkit.Viewer;

class Program
{
    public static void Main(string[] args)
    {
        CoconaLiteApp.Run((string input) =>
        {
            var fileFormat = new FileFormatCollection(true, 
                new GltfFileFormat(),
                new AseFileFormat(),
                new StlFileFormat(),
                new ColladaFileFormat(),
                new Urho3DFileFormat());

            if (!fileFormat.TryRead(new FileSystemEntry(input), out FileContainer content))
            {
                Console.Error.WriteLine($"Failed to read file {input}.");
                return -1;
            }

            return AppBuilder.Configure(()=>new App(content))
                .UsePlatformDetect()
                .With(content)
                .With(new Win32PlatformOptions
                {
                    RenderingMode = new List<Win32RenderingMode>() { Win32RenderingMode.Wgl, Win32RenderingMode.Software }
                })
                .StartWithClassicDesktopLifetime(args);
        });
    }
}