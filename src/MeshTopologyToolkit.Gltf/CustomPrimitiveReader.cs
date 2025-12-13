using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MeshTopologyToolkit.Gltf
{

    internal class CustomPrimitiveReader : IPrimitiveReader<MaterialBuilder>
    {
        private UnifiedIndexedMesh _sourceMesh;
        private MeshDrawCall _drawCall;
        private List<int> _indices = new List<int>();

        public CustomPrimitiveReader(UnifiedIndexedMesh sourceMesh, MeshDrawCall drawCall, MaterialBuilder materialBuilder)
        {
            this._sourceMesh = sourceMesh;
            this._drawCall = drawCall;
            this.Material = materialBuilder;

            var remappedIndices = new DictionaryMeshVertexAttribute<int>();
            for (int i = drawCall.StartIndex; i < drawCall.StartIndex + drawCall.NumIndices; i++)
            {
                _indices.Add(remappedIndices.Add(sourceMesh.Indices[i]));
            }

            Vertices = remappedIndices.Select(_ => new CustomVertexBuilder(sourceMesh, _)).ToList();

            switch (drawCall.Type)
            {
                case MeshTopology.TriangleList:
                    {
                        VerticesPerPrimitive = 3;
                        Triangles = VisitTrianlgeList();
                        break;
                    }
                case MeshTopology.LineList:
                    {
                        VerticesPerPrimitive = 2;
                        Lines = VisitLineList();
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private IReadOnlyList<(int A, int B)> VisitLineList()
        {
            var result = new List<(int A, int B)>();
            for (int i = 0; i < _indices.Count - 1; i += 2)
            {
                result.Add((_indices[i], _indices[i + 1]));
            }
            return result;
        }

        private IReadOnlyList<(int A, int B, int C)> VisitTrianlgeList()
        {
            var result = new List<(int A, int B, int C)>();
            for (int i = 0; i < _indices.Count - 2; i += 3)
            {
                result.Add((_indices[i], _indices[i + 1], _indices[i + 2]));
            }
            return result;
        }

        public Type VertexType => typeof(CutomVertexType);

        public MaterialBuilder Material { get; set; }

        public int VerticesPerPrimitive { get; set; } = 3;

        public IReadOnlyList<IVertexBuilder> Vertices { get; set; } = Array.Empty<IVertexBuilder>();

        public IReadOnlyList<IPrimitiveMorphTargetReader> MorphTargets { get; set; } = Array.Empty<IPrimitiveMorphTargetReader>();

        public IReadOnlyList<int> Points { get; set; } = Array.Empty<int>();

        public IReadOnlyList<(int A, int B)> Lines { get; set; } = Array.Empty<(int A, int B)>();

        public IReadOnlyList<(int A, int B, int C)> Triangles { get; set; } = Array.Empty<(int A, int B, int C)>();

        public IReadOnlyList<(int A, int B, int C, int? D)> Surfaces { get; set; } = Array.Empty<(int A, int B, int C, int? D)>();

        public IReadOnlyList<int> GetIndices()
        {
            return _indices;
        }
    }
}