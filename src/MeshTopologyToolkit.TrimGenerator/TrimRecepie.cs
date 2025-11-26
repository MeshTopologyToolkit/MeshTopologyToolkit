using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    /// <summary>
    /// Describes parameters required to generate a single trim element (texture region + geometry size).
    /// Instances of this class are used by the trim generator commands to specify where a trim resides in
    /// a texture atlas and how large the produced geometry should be in world or UI units.
    /// </summary>
    public class TrimRecepie
    {
        /// <summary>
        /// Height of the trim region in pixels.
        /// </summary>
        public int HeightInPixels { get; set; }

        /// <summary>
        /// The corner texture coordinate in the atlas where this trim's image begins.
        /// </summary>
        public Vector2 TexCoord { get; set; }

        /// <summary>
        /// The size of the trim region in texture coordinate space (UV units).
        /// </summary>
        public Vector2 TexCoordSize { get; set; }

        /// <summary>
        /// Physical size of the generated trim geometry in world units.
        /// </summary>
        public Vector2 SizeInUnits { get; set; }
    }
}