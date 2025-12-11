using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
namespace MeshTopologyToolkit.BasRelief
{
    public class Heightmap
    {
        private float[] _data;
        private int _width;
        private int _height;

        public int Height => _height;

        public int Width => _width;

        public Heightmap(int width, int height)
        {
            _width = width;
            _height = height;
            _data = new float[_width * _height];
        }

        public Heightmap(Image<Rgba32> image):this(image.Width, image.Height)
        {
            image.ProcessPixelRows(accessor => {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        ref Rgba32 pixel = ref row[x];
                        this[x, y] = GetLuminocity(pixel);
                    }
                }
            });
        }

        private float GetLuminocity(Rgba32 pixel)
        {
            const float scale = 1.0f / 255.0f;
            float luminosity = pixel.R * 0.2126f +
                   pixel.G * 0.7152f +
                   pixel.B * 0.0722f;
            return (luminosity * scale) * (pixel.A * scale);
        }

        public float this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0 || x >= _width || y >= _height)
                    return 0.0f;
                return _data[x + y * _width];
            }
            set
            {
                _data[x + y*_width] = value;
            }
        }

        public Heightmap WithBlur(float radius)
        {
            if (_height == 0 || _width == 0)
                return this;

            // Standard deviation (sigma) is often chosen based on the radius.
            // A common heuristic is sigma = radius / 3.0 or sigma = radius / 2.0.
            // We will use sigma = radius / 3.0 for a more compact, less boxy blur.
            double sigma = radius / 3.0;

            // 1. Generate the 1D Gaussian Kernel
            float[] kernel = CreateGaussianKernel((int)MathF.Ceiling(radius), sigma);

            int height = _height;
            int width = _width;

            return this.WithHorizontalBlur(kernel).WithVerticalBlur(kernel);
        }

        public Heightmap WithHorizontalBlur(float[] kernel)
        {
            var kernelSize = kernel.Length;
            var radius = (kernelSize - 1) / 2;

            var output = new Heightmap(_width, _height);

            // Horizontal Pass: iterate over rows (y)
            for (int y = 0; y < _height; y++)
            {
                // Iterate over columns (x)
                for (int x = 0; x < _width; x++)
                {
                    float blurredValue = 0;

                    // Apply the 1D kernel
                    for (int i = 0; i < kernelSize; i++)
                    {
                        // Calculate the index of the source pixel
                        int srcX = x + i - radius;

                        // Handle boundary conditions (e.g., use reflection or clamping)
                        // This uses a simple **clamping** (replicate edge pixels)
                        int clampedX = Math.Min(Math.Max(srcX, 0), _width - 1);

                        // Accumulate the weighted sum
                        blurredValue += this[clampedX, y] * kernel[i];
                    }
                    output[x, y] = blurredValue;
                }
            }
            return output;
        }

        public Heightmap WithVerticalBlur(float[] kernel)
        {
            var kernelSize = kernel.Length;
            var radius = (kernelSize - 1) / 2;

            var output = new Heightmap(_width, _height);

            // Vertical Pass: iterate over columns (x)
            for (int x = 0; x < _width; x++)
            {
                // Iterate over rows (y)
                for (int y = 0; y < _height; y++)
                {
                    float blurredValue = 0;

                    // Apply the 1D kernel
                    for (int i = 0; i < kernelSize; i++)
                    {
                        // Calculate the index of the source pixel
                        int srcY = y + i - radius;

                        // Handle boundary conditions using **clamping**
                        int clampedY = Math.Min(Math.Max(srcY, 0), _height - 1);

                        // Accumulate the weighted sum
                        blurredValue += this[x, clampedY] * kernel[i];
                    }
                    output[x, y] = blurredValue;
                }
            }
            return output;
        }

        /// <summary>
        /// Generates a 1D Gaussian kernel (e.g., for a radius of 3, the kernel size is 7).
        /// </summary>
        private static float[] CreateGaussianKernel(int radius, double sigma)
        {
            int size = 2 * radius + 1;
            float[] kernel = new float[size];
            double twoSigmaSq = 2.0 * sigma * sigma;
            double sqrtTwoPiSigmaSq = Math.Sqrt(Math.PI * twoSigmaSq);
            float sum = 0;

            for (int i = 0; i < size; i++)
            {
                // Center the index
                int x = i - radius;

                // Gaussian function: G(x) = (1 / sqrt(2*pi*sigma^2)) * exp(-x^2 / (2*sigma^2))
                // The normalization term is often ignored initially, and the kernel is normalized later.
                double value = Math.Exp(-(x * x) / twoSigmaSq);
                kernel[i] = (float)value;
                sum += kernel[i];
            }

            // Normalize the kernel so the sum of all elements is 1
            for (int i = 0; i < size; i++)
            {
                kernel[i] /= sum;
            }

            return kernel;
        }

        internal Heightmap WithHighPass(float highPassRadius)
        {
            var blur = this.WithBlur(highPassRadius);
            var res = new Heightmap(_width, _height);
            // iterate over rows (y)
            for (int y = 0; y < _height; y++)
            {
                // Iterate over columns (x)
                for (int x = 0; x < _width; x++)
                {
                    res[x,y] = this[x,y] - blur[x,y];
                }
            }
            return res;
        }

        internal Heightmap WithScale(float thickness, float padding)
        {
            var max = float.MinValue;
            var min = float.MaxValue;
            foreach (var v in _data)
            {
                max = MathF.Max(max, v);
                min = MathF.Min(min, v);
            }

            var d = Math.Max(1e-6f, max - min);
            var scale = thickness / d;
            var res = new Heightmap(_width, _height);
            // iterate over rows (y)
            for (int y = 0; y < _height; y++)
            {
                // Iterate over columns (x)
                for (int x = 0; x < _width; x++)
                {
                    res[x, y] = (this[x, y]-min)*scale + padding;
                }
            }
            return res;
        }
    }
}