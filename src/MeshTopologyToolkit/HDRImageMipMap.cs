using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class HDRImageMipMap: IImageMipMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }
        public bool IsHDR => true;
        
        private IReadOnlyList<Vector4> _pixels;

        public HDRImageMipMap(int width, int height, int depth, IReadOnlyList<Vector4> rgbaPixels)
        {
            if (rgbaPixels.Count != width * height * depth)
                throw new ArgumentException("Pixel count does not match the dimensions of the image.");

            Width = width;
            Height = height;
            Depth = depth;
            _pixels = rgbaPixels;
        }

        public IReadOnlyList<Vector4> GetHdrPixels() { return _pixels; }

        public IReadOnlyList<Color32> GetPixels() { return new LDRAdapter(_pixels); }

        private class LDRAdapter : IReadOnlyList<Color32>
        {
            private readonly IReadOnlyList<Vector4> _pixels;

            public LDRAdapter(IReadOnlyList<Vector4> pixels)
            {
                this._pixels = pixels;
            }

            public Color32 this[int index] => _pixels[index];

            public int Count => _pixels.Count;

            public IEnumerator<Color32> GetEnumerator()
            {
                foreach (var p in _pixels)
                    yield return p;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
