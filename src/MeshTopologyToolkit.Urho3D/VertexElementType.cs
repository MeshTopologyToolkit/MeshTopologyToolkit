
namespace MeshTopologyToolkit.Urho3D
{
    /// Arbitrary vertex declaration element datatypes.
    enum VertexElementType : byte
    {
        TYPE_INT = 0,
        TYPE_FLOAT,
        TYPE_VECTOR2,
        TYPE_VECTOR3,
        TYPE_VECTOR4,
        TYPE_UBYTE4,
        TYPE_UBYTE4_NORM,
        MAX_VERTEX_ELEMENT_TYPES
    };
    enum PrimitiveType
    {
        TRIANGLE_LIST = 0,
        LINE_LIST,
        POINT_LIST,
        TRIANGLE_STRIP,
        LINE_STRIP,
        TRIANGLE_FAN
    };

}
