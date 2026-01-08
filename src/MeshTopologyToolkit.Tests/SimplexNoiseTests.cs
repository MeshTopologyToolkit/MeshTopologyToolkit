using MeshTopologyToolkit.TextureFormats;
using Newtonsoft.Json.Linq;
using System.Numerics;

namespace MeshTopologyToolkit.Tests;

public class SimplexNoiseTests
{
    [Fact]
    public void Noise2D()
    {
        int w = 256;
        int h = 256;

        var colors = new Color32[w * h];

        var frequency = 16.0f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float value = SimplexNoise.Noise(new Vector2(x, y)*(frequency/w));

                byte r = (byte)MathF.Round((value * 0.5f + 0.5f) * 255f);
                colors[x + y * w] = new Color32(r, r, r, 255);
                Assert.InRange(value, -1.0f, 1.0f);
            }
        }

        Converter.SaveAs("SimplexNoise2D.png", colors, w, h);
    }

    [Fact]
    public void Turbulence()
    {
        int w = 256;

        var colors = new Vector4[w * w];

        var min = float.MaxValue;
        var max = float.MinValue;
        for (int y = 0; y < w; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float value = SimplexNoise.Turbulence(new Vector2(x, y) * (1.0f / w), 5);
                min = MathF.Min(min, value);
                max = MathF.Max(max, value);
                colors[x + y * w] = new Vector4(value, value, value, 255);
                //Assert.InRange(value, -1.0f, 1.0f);
            }
        }

        for (int x = 0; x < colors.Length; x++)
        {
            var value = colors[x].X;
            value = (value - min) / (max - min);
            colors[x] = new Vector4(value, value, value, 1.0f);
        }

        Converter.SaveAs("Turbulence.png", colors, w, w);
    }

    [Fact]
    public void FractalNoise2D()
    {
        int w = 256;

        var colors = new Color32[w * w];

        var min = float.MaxValue;
        var max = float.MinValue;
        for (int y = 0; y < w; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float value = SimplexNoise.FractalNoise(new Vector2(x, y) * (1.0f / w), 5);
                Assert.InRange(value, -1.0f, 1.0f);
                min = MathF.Min(min, value);
                max = MathF.Max(max, value);
                byte r = (byte)MathF.Round((value * 0.5f + 0.5f) * 255f);
                colors[x + y * w] = new Color32(r, r, r, 255);
            }
        }

        Converter.SaveAs("FractalNoise2D.png", colors, w, w);
    }

    [Fact]
    public void Noise3D()
    {
        int w = 256;
        int h = 256;

        var colors = new Color32[w * h];

        var frequency = 16.0f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float value = SimplexNoise.Noise(new Vector3(x, y, 1) * (frequency / w));

                byte r = (byte)MathF.Round((value * 0.5f + 0.5f) * 255f);
                colors[x + y * w] = new Color32(r, r, r, 255);
                Assert.InRange(value, -1.0f, 1.0f);
            }
        }

        Converter.SaveAs("SimplexNoise3D.png", colors, w, h);
    }

    [Fact]
    public void Noise4D()
    {
        int w = 256;
        int h = 256;

        var colors = new Color32[w * h];

        var frequency = 16.0f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float value = SimplexNoise.Noise(new Vector4(x, y, 1, 2) * (frequency / w));

                byte r = (byte)MathF.Round((value * 0.5f + 0.5f) * 255f);
                colors[x + y * w] = new Color32(r, r, r, 255);
                Assert.InRange(value, -1.0f, 1.0f);
            }
        }

        Converter.SaveAs("SimplexNoise4D.png", colors, w, h);
    }

    [Fact]
    public void FractalNoise4D()
    {
        int w = 256;

        var colors = new Color32[w * w];

        var min = float.MaxValue;
        var max = float.MinValue;
        for (int y = 0; y < w; y++)
        {
            var ay = MathF.PI * 2.0f * y / w;
            for (int x = 0; x < w; x++)
            {
                var ax = MathF.PI * 2.0f * x / w;

                var pos = new Vector4(
                    MathF.Cos(ax),
                    MathF.Sin(ax),
                    MathF.Cos(ay),
                    MathF.Sin(ay)
                );

                float value = SimplexNoise.FractalNoise(pos, 5);
                Assert.InRange(value, -1.0f, 1.0f);
                min = MathF.Min(min, value);
                max = MathF.Max(max, value);
                byte r = (byte)MathF.Round((value * 0.5f + 0.5f) * 255f);
                colors[x + y * w] = new Color32(r, r, r, 255);
            }
        }

        Converter.SaveAs("FractalNoise4D.png", colors, w, w);
    }
}
