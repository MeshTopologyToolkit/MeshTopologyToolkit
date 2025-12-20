
namespace MeshTopologyToolkit.Urho3D
{
    struct VertexElement
    {
        public static VertexElement[] LegacyVertexElements = new[]{
            new VertexElement(VertexElementType.TYPE_VECTOR3, VertexElementSemantic.SEM_POSITION, 0, false),     // Position
            new VertexElement(VertexElementType.TYPE_VECTOR3, VertexElementSemantic.SEM_NORMAL, 0, false),       // Normal
            new VertexElement(VertexElementType.TYPE_UBYTE4_NORM, VertexElementSemantic.SEM_COLOR, 0, false),    // Color
            new VertexElement(VertexElementType.TYPE_VECTOR2, VertexElementSemantic.SEM_TEXCOORD, 0, false),     // Texcoord1
            new VertexElement(VertexElementType.TYPE_VECTOR2, VertexElementSemantic.SEM_TEXCOORD, 1, false),     // Texcoord2
            new VertexElement(VertexElementType.TYPE_VECTOR3, VertexElementSemantic.SEM_TEXCOORD, 0, false),     // Cubetexcoord1
            new VertexElement(VertexElementType.TYPE_VECTOR3, VertexElementSemantic.SEM_TEXCOORD, 1, false),     // Cubetexcoord2
            new VertexElement(VertexElementType.TYPE_VECTOR4, VertexElementSemantic.SEM_TANGENT, 0, false),      // Tangent
            new VertexElement(VertexElementType.TYPE_VECTOR4, VertexElementSemantic.SEM_BLENDWEIGHTS, 0, false), // Blendweights
            new VertexElement(VertexElementType.TYPE_UBYTE4, VertexElementSemantic.SEM_BLENDINDICES, 0, false),  // Blendindices
            new VertexElement(VertexElementType.TYPE_VECTOR4, VertexElementSemantic.SEM_TEXCOORD, 4, true),      // Instancematrix1
            new VertexElement(VertexElementType.TYPE_VECTOR4, VertexElementSemantic.SEM_TEXCOORD, 5, true),      // Instancematrix2
            new VertexElement(VertexElementType.TYPE_VECTOR4, VertexElementSemantic.SEM_TEXCOORD, 6, true),      // Instancematrix3
            new VertexElement(VertexElementType.TYPE_INT, VertexElementSemantic.SEM_OBJECTINDEX, 0, false)       // Objectindex
        };

        public VertexElement(VertexElementType type, VertexElementSemantic sem, byte index, bool _)
        {
            Type = type;
            Semantic = sem;
            Index = index;
            Offset = 0;
        }

        public VertexElement(VertexElementType type, VertexElementSemantic sem, byte index, int offset)
        {
            Type = type;
            Semantic = sem;
            Index = index;
            Offset = offset;
        }

        public VertexElementType Type;
        public VertexElementSemantic Semantic;
        public ushort Index;
        public int Offset;

        internal ushort GetSize()
        {
            switch (Type)
            {
                case VertexElementType.TYPE_INT:
                    return 4;
                case VertexElementType.TYPE_FLOAT:
                    return 4;
                case VertexElementType.TYPE_VECTOR2:
                    return 8;
                case VertexElementType.TYPE_VECTOR3:
                    return 12;
                case VertexElementType.TYPE_VECTOR4:
                    return 16;
                case VertexElementType.TYPE_UBYTE4:
                    return 4;
                case VertexElementType.TYPE_UBYTE4_NORM:
                    return 4;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
