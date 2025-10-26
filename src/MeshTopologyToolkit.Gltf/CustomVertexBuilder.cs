using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;
using System;

namespace MeshTopologyToolkit.Gltf
{

    internal class CustomVertexBuilder : IVertexBuilder
    {
        private UnifiedIndexedMesh _sourceMesh;
        private int _index;

        public CustomVertexBuilder(UnifiedIndexedMesh sourceMesh, int index)
        {
            this._sourceMesh = sourceMesh;
            this._index = index;
        }

        public IMeshBuilder<TMaterial> CreateCompatibleMesh<TMaterial>(string name = null)
        {
            throw new NotImplementedException();
        }

        public IVertexGeometry GetGeometry()
        {
            return new CustomVertexGeometry(_sourceMesh, _index);
        }

        public IVertexMaterial GetMaterial()
        {
            return new CustomVertexMaterial(_sourceMesh, _index);
        }

        public IVertexSkinning GetSkinning()
        {
            return new CustomVertexSkinning(_sourceMesh, _index);
        }

        public void SetGeometry(IVertexGeometry geometry)
        {
            throw new NotImplementedException();
        }

        public void SetMaterial(IVertexMaterial material)
        {
            throw new NotImplementedException();
        }

        public void SetSkinning(IVertexSkinning skinning)
        {
            throw new NotImplementedException();
        }
    }
}