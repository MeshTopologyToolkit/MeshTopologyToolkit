using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Operators
{
    public class AlignOperator: ContentOperatorBase
    {
        Dictionary<IMesh, Vector3> _offsets = new Dictionary<IMesh, Vector3>();

        public AlignOperator(Vector3 alignment)
        {
            Alignment = alignment;
        }

        public Vector3 Alignment { get; }

        public override Node Transform(Node node)
        {
            var result = base.Transform(node);
            if (node.Mesh != null && node.Mesh.Mesh != null)
            {
                var mesh = node.Mesh.Mesh;
                if (_offsets.TryGetValue(mesh, out var offset))
                {
                    if (result.Transform is TRSTransform tRSTransform)
                    {
                        result.Transform = new TRSTransform(
                            tRSTransform.Translation + offset,
                            tRSTransform.Rotation,
                            tRSTransform.Scale);
                    }
                    else
                    {
                        var matrix = result.Transform.ToMatrix();
                        matrix.Translation += offset;
                        result.Transform = new MatrixTransform(matrix);
                    }
                }
                return node;
            }
            return node;
        }

        public override IMesh Transform(IMesh mesh)
        {
            var result = mesh.DeepCopy();

            if (result.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions))
            {
                var bbox = new BoundingBox3(positions);
                var center = bbox.Min + bbox.Size() * Alignment;

                var newPositions = new ListMeshVertexAttribute<Vector3>(positions.Count);
                foreach (var pos in positions)
                {
                    newPositions.Add(pos - center);
                }

                result.ReplaceAttribute(MeshAttributeKey.Position, newPositions);

                _offsets[mesh] = center;
            }

            return result;
        }
    }
}
