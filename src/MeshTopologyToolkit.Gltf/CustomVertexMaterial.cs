using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Memory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    internal class CustomVertexMaterial : IVertexMaterial
    {
        private UnifiedIndexedMesh _sourceMesh;
        private int _index;
        private Dictionary<int, IMeshVertexAttribute<Vector4>> _colors = new Dictionary<int, IMeshVertexAttribute<Vector4>>();
        private Dictionary<int, IMeshVertexAttribute<Vector2>> _texCoords = new Dictionary<int, IMeshVertexAttribute<Vector2>>();


        public CustomVertexMaterial(UnifiedIndexedMesh sourceMesh, int index)
        {
            this._sourceMesh = sourceMesh;
            this._index = index;

            foreach (var key in sourceMesh.GetAttributeKeys())
            {
                if (key.Name == MeshAttributeNames.Color)
                {
                    _colors[key.Channel] = sourceMesh.GetAttribute<Vector4>(key);
                    MaxColors = Math.Max(MaxColors, key.Channel+1);
                }
                if (key.Name == MeshAttributeNames.TexCoord)
                {
                    _texCoords[key.Channel] = sourceMesh.GetAttribute<Vector2>(key);
                    MaxTextCoords = Math.Max(MaxTextCoords, key.Channel + 1);
                }
            }
        }

        /// <summary>
        /// Gets the number of color attributes available in this vertex
        /// </summary>
        public int MaxColors { get; set; }

        public int MaxTextCoords { get; set; }

        public void Add(in VertexMaterialDelta delta)
        {
            throw new NotImplementedException();
        }

        public Vector4 GetColor(int index)
        {
            if (_colors.TryGetValue(index, out var attr))
                return attr[index];
            return Vector4.One;
        }

        public IEnumerable<KeyValuePair<string, AttributeFormat>> GetEncodingAttributes()
        {
            foreach (var color in _colors)
                yield return new KeyValuePair<string, AttributeFormat>(MeshAttributeNames.Color + "_" + color.Key,
                    AttributeFormat.Float4);

            foreach (var texCoor in _texCoords)
                yield return new KeyValuePair<string, AttributeFormat>(MeshAttributeNames.Color + "_" + texCoor.Key,
                    AttributeFormat.Float2);
        }

        public Vector2 GetTexCoord(int index)
        {
            if (_texCoords.TryGetValue(index, out var attr))
                return attr[index];
            return Vector2.Zero;
        }

        public void SetColor(int setIndex, Vector4 color)
        {
            throw new NotImplementedException();
        }

        public void SetTexCoord(int setIndex, Vector2 coord)
        {
            throw new NotImplementedException();
        }

        public VertexMaterialDelta Subtract(IVertexMaterial baseValue)
        {
            throw new NotImplementedException();
        }
    }
}