using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class Node
    {
        public Node(string? name = null)
        {
            Name = name;
        }
        public string? Name { get; set; }

        private List<Node> _children = new List<Node>();

        public Node? _parent;

        public ITransform Transform { get; set; } = TRSTransform.Identity;

        public MeshReference? Mesh { get; set; }

        public IReadOnlyList<Node> Children => _children;

        public Node? Parent => _parent;

        public void AddChild(Node child)
        {
            child.Detach();
            _children.Add(child);
            child._parent = this;
        }

        public void Detach()
        {
            if (_parent != null)
            {
                _parent._children.Remove(this);
                _parent = null;
            }
        }

        public ITransform GetWorldSpaceTransform()
        {
            if (_parent == null)
                return Transform;

            return _parent.Transform.Combine(Transform);
        }
    }
}
