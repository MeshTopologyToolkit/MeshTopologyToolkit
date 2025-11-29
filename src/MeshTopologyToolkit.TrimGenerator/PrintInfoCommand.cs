using Cocona;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class PrintInfoCommand
    {
        [Command("info", Description = "Print trim info.")]
        public int Build(
            [Option('t', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option(Description = "Full trim width in world units")] float widthInUnits = 5.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8)
        {

            var args = new TrimGenerationArguments(trimHeight, width: width, bevelInPixels: bevelWidth);

            Console.WriteLine($"Trims:");
            for (int i = 0; i < args.TrimRecepies.Count; i++)
            {
                TrimRecepie? trim = args.TrimRecepies[i];
                Console.WriteLine($"  {i}:");
                Console.WriteLine($"    Size : {trim.SizeInUnits.X} x {trim.SizeInUnits.Y}");
            }
            return 0;
        }
    }
}