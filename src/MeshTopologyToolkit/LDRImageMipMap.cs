using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class LDRImageMipMap : IImageMipMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Depth { get; set; }

        public bool IsHDR => false;

        private IReadOnlyList<Color32> _pixels;

        public LDRImageMipMap(int width, int height, int depth, IReadOnlyList<Color32> rgbaPixels)
        {
            if (rgbaPixels.Count != width * height * depth)
                throw new ArgumentException("Pixel count does not match the dimensions of the image.");
            Width = width;
            Height = height;
            Depth = depth;
            _pixels = rgbaPixels;
        }

        public IReadOnlyList<Vector4> GetHdrPixels() => new HDRAdapter(_pixels);

        public IReadOnlyList<Color32> GetPixels() => _pixels;

        private class HDRAdapter: IReadOnlyList<Vector4>
        {
            private readonly IReadOnlyList<Color32> _pixels;

            public HDRAdapter(IReadOnlyList<Color32> pixels)
            {
                this._pixels = pixels;
            }

            public Vector4 this[int index] => _pixels[index];

            public int Count => _pixels.Count;

            public IEnumerator<Vector4> GetEnumerator()
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
