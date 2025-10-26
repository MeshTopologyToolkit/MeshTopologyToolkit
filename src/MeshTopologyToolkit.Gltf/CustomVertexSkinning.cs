using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Memory;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    internal class CustomVertexSkinning : IVertexSkinning
    {
        private UnifiedIndexedMesh sourceMesh;
        private int _index;
        private IMeshVertexAttribute<Vector4>? _jointsAttrLow;
        private IMeshVertexAttribute<Vector4>? _weightsAttrLow;

        public CustomVertexSkinning(UnifiedIndexedMesh sourceMesh, int index)
        {
            this.sourceMesh = sourceMesh;
            this._index = index;
            foreach (var key in sourceMesh.GetAttributeKeys())
            {
                if (key.Equals(MeshAttributeKey.Joints))
                {
                    _jointsAttrLow = sourceMesh.GetAttribute<Vector4>(key);
                }
                else if (key.Equals(MeshAttributeKey.Weights))
                {
                    _weightsAttrLow = sourceMesh.GetAttribute<Vector4>(key);
                }
            }

            if (_jointsAttrLow != null && _weightsAttrLow != null)
            {
                MaxBindings = 4;
            }
            else
            {
                MaxBindings = 0;
            }
        }

        public int MaxBindings { get; set; } = 0;

        public Vector4 JointsLow => (_jointsAttrLow != null)?_jointsAttrLow[_index]:Vector4.Zero;

        public Vector4 JointsHigh => Vector4.Zero;

        public Vector4 WeightsLow => (_weightsAttrLow != null) ? _weightsAttrLow[_index] : Vector4.Zero; 

        public Vector4 WeightsHigh => Vector4.Zero;

        public (int Index, float Weight) GetBinding(int index)
        {
            throw new NotImplementedException();
        }

        public SparseWeight8 GetBindings()
        {
            return SparseWeight8.Create(JointsLow, WeightsLow, JointsHigh, WeightsHigh);
        }

        public IEnumerable<KeyValuePair<string, AttributeFormat>> GetEncodingAttributes()
        {
            if (_jointsAttrLow != null)
                yield return new KeyValuePair<string, AttributeFormat>(MeshAttributeNames.Joints + "_0", AttributeFormat.Float4);
            if (_weightsAttrLow != null)
                yield return new KeyValuePair<string, AttributeFormat>(MeshAttributeNames.Weights + "_0", AttributeFormat.Float4);
        }

        public void SetBindings(in SparseWeight8 bindings)
        {
            throw new NotImplementedException();
        }

        public void SetBindings(params (int Index, float Weight)[] bindings)
        {
            throw new NotImplementedException();
        }
    }
}