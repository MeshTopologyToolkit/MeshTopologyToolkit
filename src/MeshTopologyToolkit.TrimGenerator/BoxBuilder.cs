using MeshTopologyToolkit.Operators;
using System.Numerics;

namespace MeshTopologyToolkit.TrimGenerator
{
    public class BoxBuilder
    {
        private float _maxDeviation;
        private bool _snapToRight;
        private bool _snapToBottom;
        private UnifiedIndexedMesh _mesh;

        public BoxBuilder(float maxDeviation, bool snapToRight = false, bool snapToBottom = false)
        {
            _maxDeviation = maxDeviation;
            _snapToRight = snapToRight;
            _snapToBottom = snapToBottom;
            _mesh = new UnifiedIndexedMesh();
            Positions = new ListMeshVertexAttribute<Vector3>();
            Normals = new ListMeshVertexAttribute<Vector3>();
            TexCoords = new ListMeshVertexAttribute<Vector2>();

            _mesh.AddAttribute(MeshAttributeKey.Position, Positions);
            _mesh.AddAttribute(MeshAttributeKey.Normal, Normals);
            _mesh.AddAttribute(MeshAttributeKey.TexCoord, TexCoords);
        }

        public ListMeshVertexAttribute<Vector3> Positions { get; internal set; }
        public ListMeshVertexAttribute<Vector3> Normals { get; internal set; }
        public ListMeshVertexAttribute<Vector2> TexCoords { get; internal set; }
        public IList<int> Indices => _mesh.Indices;

        public UnifiedIndexedMesh Build()
        {
            _mesh.DrawCalls.Add(new MeshDrawCall(0, 0, MeshTopology.TriangleList, 0, _mesh.Indices.Count));
            _mesh = (UnifiedIndexedMesh)new EnsureTangentsOperator().Transform(_mesh);
            return _mesh;
        }

        public bool AddQuadToBox(BoundingBox3 pos, BoundingBox2 uv, Matrix4x4 tranform)
        {
            if (MathF.Abs(pos.Max.X - pos.Min.X) < 1e-6f)
                return false;
            if (MathF.Abs(pos.Max.Y - pos.Min.Y) < 1e-6f)
                return false;

            var positions = Positions;
            var normals = Normals;
            var texCoords = TexCoords;

            var startIndex = positions.Count;

            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(0, 0, 0)), tranform));
            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(1, 0, 0)), tranform));
            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(1, 1, 0)), tranform));
            positions.Add(Vector3.Transform(pos.Lerp(new Vector3(0, 1, 0)), tranform));

            var n = Vector3.TransformNormal(new Vector3(0, 0, 1), tranform);
            normals.Add(n);
            normals.Add(n);
            normals.Add(n);
            normals.Add(n);

            texCoords.Add(uv.Lerp(new Vector2(0, 0)));
            texCoords.Add(uv.Lerp(new Vector2(1, 0)));
            texCoords.Add(uv.Lerp(new Vector2(1, 1)));
            texCoords.Add(uv.Lerp(new Vector2(0, 1)));

            Indices.Add(startIndex);
            Indices.Add(startIndex + 1);
            Indices.Add(startIndex + 2);
            Indices.Add(startIndex);
            Indices.Add(startIndex + 2);
            Indices.Add(startIndex + 3);
            return true;
        }

        public void AddSideToBox(TrimGenerationArguments args, Vector2 size, Matrix4x4 tranform)
        {
            float width = size.X;
            float height = size.Y;

            if (height > width)
            {
                (height, width) = (width, height);
                tranform = Matrix4x4.CreateRotationZ(MathF.PI * 0.5f) * tranform;
            }

            var recepie = args.FindMatchingRecepie(height);

            var halfSize = new Vector3(width, height, 0) * 0.5f;
            var requestedSizeInUV = new Vector2(width, height) * args.UnitsToUV;

            // If requested normal map slice is bigger than the recepie slice we have to scale the requested size down.
            if (requestedSizeInUV.X > recepie.TexCoordSize.X || requestedSizeInUV.Y > recepie.TexCoordSize.Y)
            {
                var scaleFactor = MathF.Min(recepie.TexCoordSize.X / requestedSizeInUV.X, recepie.TexCoordSize.Y / requestedSizeInUV.Y);
                requestedSizeInUV *= scaleFactor;
            }

            (var leftFactor, var rightFactor) = GetSliceFactors(requestedSizeInUV.X, recepie.TexCoordSize.X, recepie.BevelSize.X);
            if (_snapToRight)
            {
                (leftFactor, rightFactor) = (rightFactor, leftFactor);
            }
            (var bottomFactor, var topFactor) = GetSliceFactors(requestedSizeInUV.Y, recepie.TexCoordSize.Y, recepie.BevelSize.Y);
            if (_snapToBottom)
            {
                (bottomFactor, topFactor) = (topFactor, bottomFactor);
            }
            if (rightFactor == 0 || leftFactor == 0)
                requestedSizeInUV.X = recepie.TexCoordSize.X;
            if (bottomFactor == 0 || topFactor == 0)
                requestedSizeInUV.Y = recepie.TexCoordSize.Y;

            var wholeBox = new BoundingBox3(halfSize * new Vector3(-1, -1, 1), halfSize * new Vector3(1, 1, 1));
            var wholeUvBox = FlipY(new BoundingBox2(recepie.TexCoord, recepie.TexCoord + recepie.TexCoordSize));
            var requestedUvBox = FlipY(new BoundingBox2(recepie.TexCoord, recepie.TexCoord + requestedSizeInUV));
            var breakPoint = wholeBox.Lerp(new Vector3(leftFactor, topFactor, 0.0f));
            var breakUvPoint = requestedUvBox.Lerp(new Vector2(leftFactor, topFactor));

            AddQuadToBox(
                new BoundingBox3(wholeBox.Min, breakPoint),
                new BoundingBox2(requestedUvBox.Min, breakUvPoint).AlignWithin(wholeUvBox, new Vector2(0, 0)),
                tranform);
            AddQuadToBox(
                new BoundingBox3(breakPoint, wholeBox.Max),
                new BoundingBox2(breakUvPoint, requestedUvBox.Max).AlignWithin(wholeUvBox, new Vector2(1, 1)),
                tranform);
            AddQuadToBox(
                new BoundingBox3(
                    wholeBox.Min * new Vector3(1, 0, 1) + breakPoint * new Vector3(0, 1, 0),
                    breakPoint * new Vector3(1, 0, 1) + wholeBox.Max * new Vector3(0, 1, 0)
                    ),
                new BoundingBox2(
                    requestedUvBox.Min * new Vector2(1, 0) + breakUvPoint * new Vector2(0, 1),
                    breakUvPoint * new Vector2(1, 0) + requestedUvBox.Max * new Vector2(0, 1)
                    ).AlignWithin(wholeUvBox, new Vector2(0, 1)),
                tranform);
            AddQuadToBox(
                new BoundingBox3(
                    wholeBox.Min * new Vector3(0, 1, 1) + breakPoint * new Vector3(1, 0, 0),
                    breakPoint * new Vector3(0, 1, 1) + wholeBox.Max * new Vector3(1, 0, 0)
                    ),
                new BoundingBox2(
                    requestedUvBox.Min * new Vector2(0, 1) + breakUvPoint * new Vector2(1, 0),
                    breakUvPoint * new Vector2(0, 1) + requestedUvBox.Max * new Vector2(1, 0)
                    ).AlignWithin(wholeUvBox, new Vector2(1, 0)),
                tranform);
        }

        public BoundingBox2 FlipY(BoundingBox2 boundingBox2)
        {
            return new BoundingBox2(new Vector2(boundingBox2.Min.X, boundingBox2.Max.Y), new Vector2(boundingBox2.Max.X, boundingBox2.Min.Y));
        }

        public (float, float) GetSliceFactors(float requestedSize, float actualSize, float bevelSize)
        {
            var scale = requestedSize / actualSize;
            if (scale < 0)
                scale = 1.0f / scale;

            var deviation = MathF.Abs(1.0f - scale) * 100.0f;
            if (deviation < _maxDeviation)
                return (1.0f, 0.0f);

            if (requestedSize < actualSize)
            {
                var bevelFactor = MathF.Min(2.0f * bevelSize / requestedSize, 0.5f);
                return (1.0f - bevelFactor, bevelFactor);
            }
            else
            {
                var fullWidthA = Math.Max(actualSize - 2.0f * bevelSize, bevelSize);
                var fullWidthB = requestedSize - fullWidthA;
                return (fullWidthA / requestedSize, fullWidthB / requestedSize);
            }
        }
    }

}