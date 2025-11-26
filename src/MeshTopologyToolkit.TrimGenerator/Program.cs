using Cocona;
namespace MeshTopologyToolkit.TrimGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            CoconaLiteApp.Run(new[] {
                typeof(GenerateBoxCommand),
                typeof(GenerateBoxPaletteCommand),
                typeof(GenerateNormalMapCommand), 
                typeof(GenerateCheckerMapCommand) });

            return 0;
        }

    }
}