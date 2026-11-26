using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    public class Material
    {
        private Dictionary<string, float> _scalarParams = new Dictionary<string, float>();
        private Dictionary<string, Vector4> _vector4Params = new Dictionary<string, Vector4>();
        private Dictionary<string, Texture> _textureParams = new Dictionary<string, Texture>();
        private Dictionary<string, int> _texCoordParams = new Dictionary<string, int>();

        public Material()
        {
        }

        public Material(string name)
        {
            Name = name;
        }

        public Material(string name, Vector4 baseColor)
        {
            Name = name;
            SetVector4(MaterialParam.BaseColor, baseColor);
        }

        public Material(Vector4 baseColor)
        {
            SetVector4(MaterialParam.BaseColor, baseColor);
        }

        public string? Name { get; set; }

        public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;

        /// <summary>Alpha threshold used when <see cref="AlphaMode"/> is <see cref="AlphaMode.Mask"/>.</summary>
        public float AlphaCutoff { get; set; } = 0.5f;

        public bool DoubleSided { get; set; }

        public void SetScalar(string name, float scalar)
        {
            _scalarParams[name] = scalar;
        }

        public void SetVector4(string name, Vector4 value)
        {
            _vector4Params[name] = value;
        }

        public void SetTexture(string name, Texture? value)
        {
            if (value == null)
                _textureParams.Remove(name);
            else
                _textureParams[name] = value;
        }

        /// <summary>Selects the texture coordinate set (TEXCOORD channel) a texture slot reads; a slot without an entry reads set 0.</summary>
        public void SetTexCoord(string name, int texCoord)
        {
            if (texCoord == 0)
                _texCoordParams.Remove(name);
            else
                _texCoordParams[name] = texCoord;
        }

        public IReadOnlyDictionary<string, int> TexCoordParams => _texCoordParams;

        public IReadOnlyDictionary<string, float> ScalarParams => _scalarParams;

        public IReadOnlyDictionary<string, Vector4> Vector4Params => _vector4Params;

        public IReadOnlyDictionary<string, Texture> TextureParams => _textureParams;
    }
}
