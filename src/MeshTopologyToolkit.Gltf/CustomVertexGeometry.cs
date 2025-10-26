using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Gltf
{
    internal class CustomVertexGeometry : IVertexGeometry
    {
        private UnifiedIndexedMesh _sourceMesh;
        private int _index;
        private IMeshVertexAttribute<Vector3>? _positions;
        private IMeshVertexAttribute<Vector3>? _normals;
        private IMeshVertexAttribute<Vector4>? _tangents;

        public CustomVertexGeometry(UnifiedIndexedMesh sourceMesh, int index)
        {
            this._sourceMesh = sourceMesh;
            this._index = index;

            foreach (var key in sourceMesh.GetAttributeKeys())
            {
                if (key.Equals(MeshAttributeKey.Position))
                {
                    _positions = sourceMesh.GetAttribute<Vector3>(key);
                }
                else if (key.Equals(MeshAttributeKey.Normal))
                {
                    _normals = sourceMesh.GetAttribute<Vector3>(key);
                }
                else if (key.Equals(MeshAttributeKey.Tangent))
                {
                    _tangents = sourceMesh.GetAttribute<Vector4>(key);
                }
            }
        }

        public void Add(in VertexGeometryDelta delta)
        {
            throw new NotImplementedException();
        }

        public void ApplyTransform(in Matrix4x4 xform)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<KeyValuePair<string, AttributeFormat>> GetEncodingAttributes()
        {
            if (_positions != null)
            {
                yield return new KeyValuePair<string, AttributeFormat>("POSITION", AttributeFormat.Float3);
            }
            if (_normals != null)
            {
                yield return new KeyValuePair<string, AttributeFormat>("NORMAL", AttributeFormat.Float3);
            }
            if (_tangents != null)
            {
                yield return new KeyValuePair<string, AttributeFormat>("TANGENT", AttributeFormat.Float3);
            }
        }

        public Vector3 GetPosition()
        {
            return (_positions != null)? _positions[_index] : Vector3.Zero;
        }

        public void SetNormal(in Vector3 normal)
        {
            throw new NotImplementedException();
        }

        public void SetPosition(in Vector3 position)
        {
            throw new NotImplementedException();
        }

        public void SetTangent(in Vector4 tangent)
        {
            throw new NotImplementedException();
        }

        public VertexGeometryDelta Subtract(IVertexGeometry baseValue)
        {
            throw new NotImplementedException();
        }

        public bool TryGetNormal(out Vector3 normal)
        {
            if (_normals != null)
            {
                normal = _normals[_index];
                return true;
            }
            normal = Vector3.UnitZ;
            return false;
        }

        public bool TryGetTangent(out Vector4 tangent)
        {
            if (_tangents != null)
            {
                tangent = _tangents[_index];
                return true;
            }
            tangent = Vector4.Zero;
            return false;
        }
    }
}