using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit
{
    /// <summary>
    /// Describes how a mesh is bound to a hierarchy of nodes (joints).
    /// Joint indices stored in the mesh <see cref="MeshAttributeKey.Joints"/> attribute are indices into <see cref="Joints"/>.
    /// </summary>
    public class Skin
    {
        public Skin(string? name = null)
        {
            Name = name;
        }

        public string? Name { get; set; }

        /// <summary>
        /// Joint nodes. Vertex joint indices select from this list.
        /// </summary>
        public IList<Node> Joints { get; } = new List<Node>();

        /// <summary>
        /// One matrix per joint that transforms from model space to the joint space at the bind pose
        /// (System.Numerics row-vector convention, same as SharpGLTF returns).
        /// </summary>
        public IList<Matrix4x4> InverseBindMatrices { get; } = new List<Matrix4x4>();

        /// <summary>
        /// Optional root node of the skeleton.
        /// </summary>
        public Node? Skeleton { get; set; }

        public override string ToString() => Name != null ? $"Skin \"{Name}\" ({Joints.Count} joints)" : $"Skin ({Joints.Count} joints)";
    }
}
