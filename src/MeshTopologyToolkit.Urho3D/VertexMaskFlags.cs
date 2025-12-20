
namespace MeshTopologyToolkit.Urho3D
{
    // Legacy vertex element bitmasks.
    [Flags]
    public enum VertexMaskFlags : uint
    {
        MASK_NONE = 0x0,
        MASK_POSITION = 0x1,
        MASK_NORMAL = 0x2,
        MASK_COLOR = 0x4,
        MASK_TEXCOORD1 = 0x8,
        MASK_TEXCOORD2 = 0x10,
        MASK_CUBETEXCOORD1 = 0x20,
        MASK_CUBETEXCOORD2 = 0x40,
        MASK_TANGENT = 0x80,
        MASK_BLENDWEIGHTS = 0x100,
        MASK_BLENDINDICES = 0x200,
        MASK_INSTANCEMATRIX1 = 0x400,
        MASK_INSTANCEMATRIX2 = 0x800,
        MASK_INSTANCEMATRIX3 = 0x1000,
        MASK_OBJECTINDEX = 0x2000,
    };
}
