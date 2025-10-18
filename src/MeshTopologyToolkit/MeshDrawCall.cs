
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
            switch (Type)
            {
                case MeshTopology.TriangleList:
                    {   
                        for (int i=StartIndex; i<StartIndex+NumIndices-2; i+=3)
                        {
                            yield return new Face(indices[i], indices[i+1], indices[i+2]);
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
