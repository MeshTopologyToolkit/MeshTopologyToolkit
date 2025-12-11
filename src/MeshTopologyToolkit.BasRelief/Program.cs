using Cocona;
namespace MeshTopologyToolkit.BasRelief
{
    class Program
    {
        static int Main(string[] args)
        {
            CoconaLiteApp.Run(new[] {
                typeof(GenerateBasRelief),
                typeof(GenerateRing),
            });

            return 0;
        }
    }
}