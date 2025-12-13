using System;
using System.Collections.Generic;
using System.Numerics;

namespace MeshTopologyToolkit.Operators
{
    public class EliminateTVerticesOperator : ContentOperatorBase
    {
        float _threshold = 1e-6f;

        public EliminateTVerticesOperator()
        {
        }

        public override IMesh Transform(IMesh mesh)
        {
            // Optimize mesh by welding nearby vertices.
            var sourceMesh = new SeparatedIndexedMesh(mesh, _threshold);

            var stridedIndices = new StridedIndexContainer(sourceMesh);
            int positionAttrIndex = -1;
            for (int i = 0; i < stridedIndices.Keys.Count; i++)
            {
                if (stridedIndices.Keys[i] == MeshAttributeKey.Position)
                {
                    positionAttrIndex = i;
                    break;
                }
            }

            var resultMesh = new SeparatedIndexedMesh();
            var attributeValues = new List<IMeshVertexAttribute>();
            var attributeIndices = new List<List<int>>();
            foreach (var attrKey in stridedIndices.Keys)
            {
                var attrIndices = new List<int>();
                var attrValues = sourceMesh.GetAttribute(attrKey);
                attributeIndices.Add(attrIndices);
                attributeValues.Add(attrValues);
                resultMesh.AddAttribute(attrKey, attrValues, attrIndices);
            }

            var positionTree = new RTree3();
            {
                var positions = sourceMesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
                for (var posIndex = 0; posIndex < positions.Count; ++posIndex)
                {
                    var index = positionTree.Insert(new BoundingBox3(positions[posIndex], _threshold));
                    if (index != posIndex)
                        throw new Exception("There is bug in RTree");
                }
            }

            void addRange(StridedIndexRange range)
            {
                for (int i = 0; i < attributeIndices.Count; i++)
                {
                    attributeIndices[i].Add(range[i]);
                }
            }

            // Iterate through all faces, slice and remap when required.
            foreach (var drawCall in sourceMesh.DrawCalls)
            {
                int startIndex = attributeIndices[0].Count;
                switch (drawCall.Type)
                {
                    case MeshTopology.TriangleList:
                    case MeshTopology.TriangleStrip:
                    case MeshTopology.TriangleFan:
                        {
                            attributeValues[positionAttrIndex].TryCast<Vector3>(MeshVertexAttributeConverterProvider.Default, out var resPositions);
                            var processor = new FaceProcessor(resPositions, positionTree, stridedIndices, positionAttrIndex, attributeValues);
                            foreach (var face in drawCall.GetFaces())
                            {
                                foreach (var splitFace in processor.Process(face))
                                {
                                    addRange(stridedIndices[splitFace.A]);
                                    addRange(stridedIndices[splitFace.B]);
                                    addRange(stridedIndices[splitFace.C]);
                                }
                            }
                            var numIndices = attributeIndices[0].Count - startIndex;
                            if (numIndices % 3 != 0)
                                throw new Exception("There is bug in face splitting");
                            resultMesh.DrawCalls.Add(new MeshDrawCall(drawCall.LodLevel, drawCall.MaterialIndex, MeshTopology.TriangleList, startIndex, numIndices));
                            break;
                        }
                    // Make a copy of indices by default.
                    default:
                        {
                            for (int i=drawCall.StartIndex; i<drawCall.StartIndex+ drawCall.NumIndices; ++i)
                            {
                                addRange(stridedIndices[i]);
                            }
                            resultMesh.DrawCalls.Add(new MeshDrawCall(drawCall.LodLevel, drawCall.MaterialIndex, drawCall.Type, startIndex, attributeIndices[0].Count - startIndex));
                            break;
                        }
                }
            }

            return resultMesh;
        }

        class FaceProcessor
        {
            private IMeshVertexAttribute<Vector3> _positions;
            private RTree3 _positionTree;
            private StridedIndexContainer _stridedIndices;
            private int _positionKeyIndex;
            private List<IMeshVertexAttribute> _attributeValues;
            private List<int> _tempIndices;

            public FaceProcessor(IMeshVertexAttribute<Vector3> positions, RTree3 positionTree, StridedIndexContainer stridedIndices, int positionKeyIndex, List<IMeshVertexAttribute> attributeValues)
            {
                _positions = positions;
                _positionTree = positionTree;
                _stridedIndices = stridedIndices;
                _positionKeyIndex = positionKeyIndex;
                _attributeValues = attributeValues;
                _tempIndices = new List<int>(_attributeValues.Count);
            }

            internal IEnumerable<MeshDrawCall.Face> Process(MeshDrawCall.Face face)
            {
                return Process(face, 2);
            }
            internal IEnumerable<MeshDrawCall.Face> Process(MeshDrawCall.Face face, int depth)
            {
                if (depth < 0)
                {
                    // Restore face to original order and return.
                    yield return face.Rotate();
                    yield break;
                }

                var vertexAIndices = _stridedIndices[face.A];
                var vertexBIndices = _stridedIndices[face.B];
                var positionA = _positions[vertexAIndices[_positionKeyIndex]];
                var positionB = _positions[vertexBIndices[_positionKeyIndex]];
                var direction = positionB - positionA;
                var distanceSquared = direction.LengthSquared();
                if (distanceSquared >= 1e-6f)
                {
                    //direction = direction * (1.0f / distance);
                    var bbox = BoundingBox3.Empty.Merge(positionA).Merge(positionB);
                    foreach (var splitVertexIndex in _positionTree.Query(bbox))
                    {
                        var splitVertex = _positions[splitVertexIndex];
                        if (splitVertexIndex == vertexAIndices[_positionKeyIndex] || splitVertexIndex == vertexBIndices[_positionKeyIndex])
                            continue;

                        // Project point onto the (infinite) line
                        var factor = Vector3.Dot(splitVertex - positionA, direction) / distanceSquared;

                        if (factor <= 1e-6f || factor >= 1.0f - 1e-6f)
                            continue;

                        // Find closest point on segment
                        Vector3 closestPoint = positionA + factor * direction;

                        // Check distance from point to segment
                        float sqrDist = (splitVertex - closestPoint).LengthSquared();
                        if (sqrDist <= 1e-6f)
                        {
                            var newVertexIndex = _stridedIndices.Count;
                            for (int i=0; i< _attributeValues.Count; ++i)
                            {
                                var index = _attributeValues[i].Lerp(vertexAIndices[i], vertexBIndices[i], factor);
                                _tempIndices.Add(index);
                                //_attributeIndices[i].Add(index);
                            }
                            _stridedIndices.Add(new StridedIndexRange(_tempIndices, 0, _tempIndices.Count));
                            _tempIndices.Clear();

                            foreach (var subFace in Process(new MeshDrawCall.Face(face.A, newVertexIndex, face.C), 2))
                                yield return subFace;
                            foreach (var subFace in Process(new MeshDrawCall.Face(newVertexIndex, face.B, face.C), 2))
                                yield return subFace;
                            yield break;
                        }
                    }
                }

                foreach (var subFace in Process(face.Rotate(), depth - 1))
                    yield return subFace;
            }
        }
    }
}
