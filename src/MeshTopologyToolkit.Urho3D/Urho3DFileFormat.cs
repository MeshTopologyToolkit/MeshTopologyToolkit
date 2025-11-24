
using System.Numerics;

namespace MeshTopologyToolkit.Urho3D
{
    public class Urho3DFileFormat : IFileFormat
    {
        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Urho3D model", ".mdl"),
        };

        const int LegacyVersion = 1;
        const int MorphWeightVersion = 2;

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public bool TryRead(IFileSystemEntry entry, out FileContainer content)
        {
            content = new FileContainer();
            if (!entry.Exists)
                return false;

            using (var stream = entry.OpenRead())
            {
                if (stream == null)
                    return false;

                using (var binaryReader = new BinaryReader(stream))
                {
                    var magic = binaryReader.ReadBytes(4);
                    if (magic[0] != 'U' || magic[1] != 'M' || magic[2] != 'D')
                        return false;

                    int version = LegacyVersion;
                    bool hasVertexDeclarations = true;
                    if (magic[3] == 'L')
                    {
                        version = LegacyVersion;
                        hasVertexDeclarations = false;
                    }
                    else if (magic[3] == '2')
                    {
                        version = LegacyVersion;
                        hasVertexDeclarations = true;
                    }
                    else if (magic[3] == '3')
                    {
                        version = binaryReader.ReadInt32();
                        hasVertexDeclarations = true;
                    }
                    else
                    {
                        return false;
                    }

                    // Read vertex buffers.
                    var vertexBuffers = new List<UnifiedIndexedMesh>();
                    uint numVertexBuffers = binaryReader.ReadUInt32();
                    for (uint vertexBufferIndex = 0; vertexBufferIndex < numVertexBuffers; ++vertexBufferIndex)
                    {
                        var vertexCount = binaryReader.ReadInt32();
                        var elements = new List<VertexElement>();
                        if (!hasVertexDeclarations)
                        {
                            var elementMask = binaryReader.ReadUInt32();
                            ushort offset = 0;
                            for (int elementIndex = 0; elementIndex < VertexElement.LegacyVertexElements.Length; ++elementIndex)
                            {
                                if (0 != (elementMask & (1u << elementIndex)))
                                {
                                    var element = VertexElement.LegacyVertexElements[elementIndex];
                                    element.Offset = offset;
                                    elements.Add(element);
                                    offset += element.GetSize();
                                }
                            }
                        }
                        else
                        {
                            uint numElements = binaryReader.ReadUInt32();
                            ushort offset = 0;
                            for (uint elementIndex = 0; elementIndex < numElements; ++elementIndex)
                            {
                                var elementType = (VertexElementType)binaryReader.ReadByte();
                                var elementSemantic = (VertexElementSemantic)binaryReader.ReadByte();
                                var elementIndexInSemantic = binaryReader.ReadUInt16();
                                var element = new VertexElement { Type = elementType, Semantic = elementSemantic, Index = elementIndexInSemantic, Offset = offset };
                                elements.Add(element);
                                offset += element.GetSize();
                            }
                        }

                        var morphRangeStarts = binaryReader.ReadUInt32();
                        var morphRangeCounts = binaryReader.ReadUInt32();

                        var vertexSize = elements.Max(e => e.Offset + e.GetSize());

                        var buffer = binaryReader.ReadBytes(vertexSize * vertexCount);

                        var mesh = new UnifiedIndexedMesh($"VertexBuffer{vertexBufferIndex}");
                        foreach (var element in elements)
                        {
                            MeshAttributeKey meshAttributeKey = GetMeshAttrKey(element);
                            mesh.AddAttribute(meshAttributeKey, GetMeshAttr(element, buffer, vertexCount, vertexSize));
                        }
                        vertexBuffers.Add(mesh);
                    }

                    var finalMesh = UnifiedIndexedMesh.Merge(vertexBuffers);
                    var vertexBufferOffsets = new List<int>();
                    {
                        int offset = 0;
                        foreach (var vb in vertexBuffers)
                        {
                            vertexBufferOffsets.Add(offset);
                            offset += vb.GetNumVertices();
                        }
                    }
                    // Read vertex buffers.
                    var indexBuffers = new List<List<int>>();
                    uint numIndexBuffers = binaryReader.ReadUInt32();
                    for (uint indexBufferIndex = 0; indexBufferIndex < numIndexBuffers; ++indexBufferIndex)
                    {
                        uint indexCount = binaryReader.ReadUInt32();
                        uint indexSize = binaryReader.ReadUInt32();
                        switch (indexSize)
                        {
                            case 2:
                                {
                                    var indices = new List<int>();
                                    for (uint i = 0; i < indexCount; ++i)
                                    {
                                        var index = binaryReader.ReadUInt16();
                                        indices.Add(index);
                                    }
                                    indexBuffers.Add(indices);
                                    break;

                                }
                            case 4:
                                {
                                    var indices = new List<int>();
                                    for (uint i = 0; i < indexCount; ++i)
                                    {
                                        var index = binaryReader.ReadUInt32();
                                        indices.Add((int)index);
                                    }
                                    indexBuffers.Add(indices);
                                    break;
                                }
                            default:
                                throw new NotSupportedException($"Index size {indexSize} is not supported.");
                        }
                    }
                    // Read geometries.
                    uint numGeometries = binaryReader.ReadUInt32();
                    for (uint geometryIndex = 0; geometryIndex < numGeometries; ++geometryIndex)
                    {
                        var boneMapping = new int[binaryReader.ReadInt32()];
                        for (uint j = 0; j < boneMapping.Length; ++j)
                            boneMapping[j] = (int)binaryReader.ReadUInt32();

                        uint numLodLevels = binaryReader.ReadUInt32();
                        for (uint lodLevelIndex = 0; lodLevelIndex < numLodLevels; ++lodLevelIndex)
                        {
                            var distance = binaryReader.ReadSingle();
                            var primitiveType = (PrimitiveType)binaryReader.ReadUInt32();
                            var vbIndex = binaryReader.ReadUInt32();
                            var ibIndex = binaryReader.ReadUInt32();
                            var startIndex = binaryReader.ReadUInt32();
                            var indexCount = binaryReader.ReadUInt32();

                            if (indexCount > 0)
                            {
                                var drawCallStartIndex = finalMesh.Indices.Count;
                                for (var i = startIndex; i < startIndex + indexCount; ++i)
                                {
                                    var index = indexBuffers[(int)ibIndex][(int)i] + vertexBufferOffsets[(int)vbIndex];
                                    finalMesh.Indices.Add(index);
                                }

                                finalMesh.DrawCalls.Add(new MeshDrawCall((int)lodLevelIndex, (int)geometryIndex, GetPrimType(primitiveType), drawCallStartIndex, finalMesh.Indices.Count-drawCallStartIndex ));
                            }
                        }
                    }

                    uint numMorphs = binaryReader.ReadUInt32();
                    for (uint morphIndex = 0; morphIndex < numMorphs; ++morphIndex)
                    {
                        var name = binaryReader.ReadStringZ();
                        if (version >= MorphWeightVersion)
                        {
                            var weight = binaryReader.ReadSingle();
                        }
                        uint numBuffers = binaryReader.ReadUInt32();
                        for (uint bufferIndex = 0; bufferIndex < numBuffers; ++bufferIndex)
                        {
                            uint vbIndex = binaryReader.ReadUInt32();
                            var elementMask = (VertexMaskFlags)binaryReader.ReadUInt32();
                            uint vertexCount = binaryReader.ReadUInt32();
                            var vertexSize = 4;
                            if (0 != (elementMask & VertexMaskFlags.MASK_POSITION))
                            {
                                vertexSize += 4 * 3;
                            }
                            if (0 != (elementMask & VertexMaskFlags.MASK_NORMAL))
                            {
                                vertexSize += 4 * 3;
                            }
                            if (0 != (elementMask & VertexMaskFlags.MASK_TANGENT))
                            {
                                vertexSize += 4 * 3;
                            }
                            binaryReader.ReadBytes((int)(vertexSize * vertexCount));
                        }
                    }

                    var skeleton = new Skeleton();
                    skeleton.Read(binaryReader);

                    binaryReader.ReadBoundingBox3();

                    content.Meshes.Add(finalMesh);
                    content.AddSingleMeshScene(finalMesh);
                }
                return true;
            }
        }

        private MeshTopology GetPrimType(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.TRIANGLE_LIST:
                    return MeshTopology.TriangleList;
                case PrimitiveType.TRIANGLE_STRIP:
                    return MeshTopology.TriangleStrip;
                case PrimitiveType.TRIANGLE_FAN:
                    return MeshTopology.TriangleFan;
                case PrimitiveType.LINE_LIST:
                    return MeshTopology.LineList;
                case PrimitiveType.POINT_LIST:
                    return MeshTopology.Points;
                case PrimitiveType.LINE_STRIP:
                    return MeshTopology.LineStrip;
            }
            throw new NotSupportedException($"PrimitiveType {primitiveType} is not supported.");
        }

        private IMeshVertexAttribute GetMeshAttr(VertexElement element, byte[] buffer, int vertexCount, int vertexSize)
        {
            var ms = new MemoryStream(buffer);
            var reader = new BinaryReader(ms);

            IMeshVertexAttribute<T> ReadSparseList<T>(Func<T> read) where T:notnull
            {
                var attr = new ListMeshVertexAttribute<T>();
                for (int i=0; i<vertexCount; ++i)
                {
                    var pos = vertexSize*i + element.Offset;
                    ms.Seek(pos, SeekOrigin.Begin);
                    attr.Add(read());
                }
                return attr;
            };

            switch (element.Type)
            {
                case VertexElementType.TYPE_VECTOR2:
                    return ReadSparseList<Vector2>(() => {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        return new Vector2(x, y);
                    });
                case VertexElementType.TYPE_VECTOR3:
                    return ReadSparseList<Vector3>(() => {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        var z = reader.ReadSingle();
                        return new Vector3(x, y, z);
                    });
                case VertexElementType.TYPE_VECTOR4:
                    return ReadSparseList<Vector4>(() => {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        var z = reader.ReadSingle();
                        var w = reader.ReadSingle();
                        return new Vector4(x, y, z, w);
                    });
                case VertexElementType.TYPE_UBYTE4:
                    return ReadSparseList<Vector4>(() => {
                        var x = reader.ReadByte();
                        var y = reader.ReadByte();
                        var z = reader.ReadByte();
                        var w = reader.ReadByte();
                        return new Vector4(x, y, z, w);
                    });
                default:
                    throw new NotSupportedException($"VertexElementType {element.Type} is not supported.");
            }
        }

        private MeshAttributeKey GetMeshAttrKey(VertexElement element)
        {
            switch (element.Semantic)
            {
                case VertexElementSemantic.SEM_POSITION:
                    return new MeshAttributeKey(MeshAttributeNames.Position, element.Index);
                case VertexElementSemantic.SEM_NORMAL:
                    return new MeshAttributeKey(MeshAttributeNames.Normal, element.Index);
                case VertexElementSemantic.SEM_TANGENT:
                    return new MeshAttributeKey(MeshAttributeNames.Tangent, element.Index);
                case VertexElementSemantic.SEM_COLOR:
                    return new MeshAttributeKey(MeshAttributeNames.Color, element.Index);
                case VertexElementSemantic.SEM_TEXCOORD:
                    return new MeshAttributeKey(MeshAttributeNames.TexCoord, element.Index);
                case VertexElementSemantic.SEM_BLENDINDICES:
                    return new MeshAttributeKey(MeshAttributeNames.Joints, element.Index);
                case VertexElementSemantic.SEM_BLENDWEIGHTS:
                    return new MeshAttributeKey(MeshAttributeNames.Weights, element.Index);
                default:
                    throw new NotSupportedException($"VertexElementSemantic {element.Semantic} is not supported.");
            }
        }

        public bool TryWrite(IFileSystemEntry entry, FileContainer content)
        {
            return false;
        }
    }

}
