using Cocona;
namespace MeshTopologyToolkit.TrimGenerator
{
    class Program
    {
        static int Main(string[] args)
        {
            CoconaLiteApp.Run(new[] { typeof(GenerateNormalMapCommand), typeof(GenerateCheckersCommand) });

            return 0;
        }

    }
}