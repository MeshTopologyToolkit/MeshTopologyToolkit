
using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public struct MeshDrawCall
    {
        public MeshDrawCall(MeshTopology type, int startIndex, int numIndices)
        {
            Type = type;
            StartIndex = startIndex;
            NumIndices = numIndices;
        }

        public MeshTopology Type { get; }
        public int StartIndex { get; }
        public int NumIndices { get; }


        public IEnumerable<Face> GetFaces(IReadOnlyList<int> indices)
        {
            foreach (var face in GetFaces())
            {
                yield return new Face(indices[face.A], indices[face.B], indices[face.C]);
            }
        }

        public IEnumerable<Face> GetFaces()
        {
            switch (Type)
            {
                case MeshTopology.TriangleList:
                    {
                        for (int i = StartIndex; i < StartIndex + NumIndices - 2; i += 3)
                        {
                            yield return new Face(i, i + 1, i + 2);
                        }
                    }
                    break;
                case MeshTopology.TriangleStrip:
                    {
                        bool swapDirection = false;
                        for (int i = StartIndex; i < StartIndex + NumIndices - 2; ++i)
                        {
                            if (swapDirection)
                            {
                                yield return new Face(i + 1, i, i + 2);
                            }
                            else
                            {
                                yield return new Face(i, i + 1, i + 2);
                            }
                        }
                    }
                    break;
                case MeshTopology.TriangleFan:
                    {
                        for (int i = StartIndex + 1; i < StartIndex + NumIndices - 1; ++i)
                        {
                            yield return new Face(StartIndex, i, i + 1);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public MeshDrawCall Clone()
        {
            return new MeshDrawCall(Type, StartIndex, NumIndices);
        }

        public override string ToString()
        {
            return $"{Type} ({StartIndex} ... {StartIndex+NumIndices-1})";
        }

        public struct Face
        {
            public int A;
            public int B;
            public int C;

            public Face(int v1, int v2, int v3) : this()
            {
                A = v1;
                B = v2;
                C = v3;
            }
        }
    }
}
