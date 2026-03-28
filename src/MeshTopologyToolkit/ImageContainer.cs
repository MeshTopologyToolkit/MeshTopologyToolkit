using System.Collections;
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class ImageContainer: IReadOnlyList<IImageMipMap>
    {
        List<IImageMipMap> _mipmaps;

        public ImageContainer(params IImageMipMap[] mipmaps)
        {
            _mipmaps = new List<IImageMipMap>(mipmaps);
        }

        public IImageMipMap this[int index] => _mipmaps[index];

        public int Count => _mipmaps.Count;

        public IEnumerator<IImageMipMap> GetEnumerator()
        {
            return ((IEnumerable<IImageMipMap>)_mipmaps).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_mipmaps).GetEnumerator();
        }
    }
}
