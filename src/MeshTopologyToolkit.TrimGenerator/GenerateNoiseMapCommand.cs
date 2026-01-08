using Cocona;
using MeshTopologyToolkit.TextureFormats;
using SharpGLTF.Schema2;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class GenerateNoiseMapCommand
    {
        [Command("noisemap", Description = "Generate noise map.")]
        public int Build(
            [Option('w', Description = "Texture width in pixels")] int width = 1024,
            [Option('h', Description = "Texture height in pixels")] int height = 1024,
            [Option(Description = "Number of octaves")] int octaves = 8,
            [Option('f', Description = "First octave frequency")] float frequency = 1.0f,
            [Option('o', Description = "Output file name")] string output = "noise.png")
        {
            var colors = new Color32[width * height];

            var min = float.MaxValue;
            var max = float.MinValue;
            for (int y = 0; y < height; y++)
            {
                var ay = MathF.PI * 2.0f * y / height;
                for (int x = 0; x < width; x++)
                {
                    var ax = MathF.PI * 2.0f * x / width;

                    var pos = new Vector4(
                        MathF.Cos(ax),
                        MathF.Sin(ax),
                        MathF.Cos(ay),
                        MathF.Sin(ay)
                    );

                    float value = SimplexNoise.FractalNoise(pos * frequency, octaves);
                    min = MathF.Min(min, value);
                    max = MathF.Max(max, value);
                    byte r = (byte)MathF.Round((value * 0.5f + 0.5f) * 255f);
                    colors[x + y * width] = new Color32(r, r, r, 255);
                }
            }

            Converter.SaveAs(output, colors, width, height);

            return 0;
        }
    }
}