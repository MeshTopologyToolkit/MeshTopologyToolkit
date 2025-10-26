namespace MeshTopologyToolkit
{
    /// <summary>
    /// Mesh attribute names matching glTF specification.
    /// https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html
    /// </summary>
    public struct MeshAttributeNames
    {
        public readonly static string Position = "POSITION";
        public readonly static string Normal = "NORMAL";
        public readonly static string Tangent = "TANGENT";
        public readonly static string Color = "COLOR";
        public readonly static string TexCoord = "TEXCOORD";
        public readonly static string Joints = "JOINTS";
        public readonly static string Weights = "WEIGHTS";
    }
}
