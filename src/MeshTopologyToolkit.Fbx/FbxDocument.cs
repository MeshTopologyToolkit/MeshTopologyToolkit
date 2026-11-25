using System.Collections.Generic;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Top level of a parsed FBX file.
    /// </summary>
    public sealed class FbxDocument
    {
        /// <summary>File version, for example 7400 for FBX 7.4 and 6100 for FBX 6.1.</summary>
        public int Version { get; set; } = 7400;

        /// <summary>
        /// The version stored in the <c>FBXVersion</c> node when there is one, otherwise <see cref="Version"/>.
        /// Binary files of FBX 5.x have a different version in the file header.
        /// </summary>
        public int EffectiveVersion
        {
            get
            {
                var node = Child("FBXHeaderExtension")?.Child("FBXVersion") ?? Child("FBXVersion");
                var v = node?.GetInt(0) ?? 0;
                return v > 0 ? v : Version;
            }
        }

        public bool IsBinary { get; set; }

        public List<FbxNode> Roots { get; } = new List<FbxNode>();

        public FbxNode? Child(string name)
        {
            foreach (var n in Roots)
                if (n.Name == name)
                    return n;
            return null;
        }

        public IEnumerable<FbxNode> ChildrenNamed(string name)
        {
            foreach (var n in Roots)
                if (n.Name == name)
                    yield return n;
        }
    }
}
