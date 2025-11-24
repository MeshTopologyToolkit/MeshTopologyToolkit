using Cocona;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateNormalMapCommand
    {
        [Command("normalmap", Description = "Generate trim normal map from trim height data.")]
        public int Build(
            [Option('h', Description = "Trim height in pixels")] int[] trimHeight,
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            float widthInUnits = 5.0f,
            [Option('b', Description = "Bevel width in pixels")] int bevelWidth = 8,
            [Option('o', Description = "Output file name")] string? output = null)
        {
            var args = new TrimGenerationArguments(trimHeight, width, bevelWidth);

            return 0;
        }
    }
}