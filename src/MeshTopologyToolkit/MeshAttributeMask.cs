using System;

namespace MeshTopologyToolkit
{
    [Flags]
    public enum MeshAttributeMask
    {
        /// <summary>
        /// No extra attributes besides position.
        /// </summary>
        None = 0,

        /// <summary>
        /// Normal attribute.
        /// </summary>
        Normal = 1 << 0,

        /// <summary>
        /// Tangent attribute.
        /// </summary>
        Tangent = 1 << 1,

        /// <summary>
        /// TexCoord attribute.
        /// </summary>
        TexCoord = 1 << 2,

        /// <summary>
        /// Color attribute.
        /// </summary>
        Color = 1 << 3,

        /// <summary>
        /// All attributes.
        /// </summary>
        All = Normal | Tangent | TexCoord | Color,
    }
}
