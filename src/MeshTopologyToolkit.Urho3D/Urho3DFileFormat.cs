
using MeshTopologyToolkit.Operators;
using System.Numerics;

namespace MeshTopologyToolkit.Urho3D
{
    public class Urho3DFileFormat : IFileFormat
    {
        static readonly SupportedExtension[] _extensions = new[] {
            new SupportedExtension("Urho3D model", ".mdl"),
        };
        private Urho3DModelVersion _preferableVersion;

        public IReadOnlyList<SupportedExtension> SupportedExtensions => _extensions;

        public Urho3DFileFormat(Urho3DModelVersion preferableVersion = Urho3DModelVersion.Rbfx)
        {
            _preferableVersion = preferableVersion;
        }

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

                    var flags = new Urho3DModelVersionFlags(Urho3DModelVersion.Original);

                    if (magic[3] == 'L')
                    {
                        flags = new Urho3DModelVersionFlags(Urho3DModelVersion.Original);
                    }
                    else if (magic[3] == '2')
                    {
                        flags = new Urho3DModelVersionFlags(Urho3DModelVersion.VertexDeclarations);
                    }
                    else if (magic[3] == '3')
                    {
                        var version = binaryReader.ReadInt32();
                        flags = new Urho3DModelVersionFlags(Urho3DModelVersion.Rbfx + (version-1));
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
                        if (!flags.HasVertexDeclarations)
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
                            while (finalMesh.Lods.Count <= lodLevelIndex)
                                finalMesh.Lods.Add(new MeshLod { Distance = distance });
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

                                finalMesh.DrawCalls.Add(new MeshDrawCall((int)lodLevelIndex, (int)geometryIndex, GetPrimType(primitiveType), drawCallStartIndex, finalMesh.Indices.Count - drawCallStartIndex));
                            }
                        }
                    }

                    uint numMorphs = binaryReader.ReadUInt32();
                    for (uint morphIndex = 0; morphIndex < numMorphs; ++morphIndex)
                    {
                        var name = binaryReader.ReadStringZ();
                        if (flags.HasMorphWeights)
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

                    content.Add(finalMesh);
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
        private PrimitiveType GetPrimType(MeshTopology primitiveType)
        {
            switch (primitiveType)
            {
                case MeshTopology.TriangleList:
                    return PrimitiveType.TRIANGLE_LIST;
                case MeshTopology.TriangleStrip:
                    return PrimitiveType.TRIANGLE_STRIP;
                case MeshTopology.TriangleFan:
                    return PrimitiveType.TRIANGLE_FAN;
                case MeshTopology.LineList:
                    return PrimitiveType.LINE_LIST;
                case MeshTopology.Points:
                    return PrimitiveType.POINT_LIST;
                case MeshTopology.LineStrip:
                    return PrimitiveType.LINE_STRIP;
            }
            throw new NotSupportedException($"PrimitiveType {primitiveType} is not supported.");
        }
        private IMeshVertexAttribute GetMeshAttr(VertexElement element, byte[] buffer, int vertexCount, int vertexSize)
        {
            var ms = new MemoryStream(buffer);
            var reader = new BinaryReader(ms);

            IMeshVertexAttribute<T> ReadSparseList<T>(Func<T> read) where T : notnull
            {
                var attr = new ListMeshVertexAttribute<T>();
                for (int i = 0; i < vertexCount; ++i)
                {
                    var pos = vertexSize * i + element.Offset;
                    ms.Seek(pos, SeekOrigin.Begin);
                    attr.Add(read());
                }
                return attr;
            }
            ;

            switch (element.Type)
            {
                case VertexElementType.TYPE_VECTOR2:
                    return ReadSparseList<Vector2>(() =>
                    {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        return new Vector2(x, y);
                    });
                case VertexElementType.TYPE_VECTOR3:
                    return ReadSparseList<Vector3>(() =>
                    {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        var z = reader.ReadSingle();
                        return new Vector3(x, y, z);
                    });
                case VertexElementType.TYPE_VECTOR4:
                    return ReadSparseList<Vector4>(() =>
                    {
                        var x = reader.ReadSingle();
                        var y = reader.ReadSingle();
                        var z = reader.ReadSingle();
                        var w = reader.ReadSingle();
                        return new Vector4(x, y, z, w);
                    });
                case VertexElementType.TYPE_UBYTE4:
                    return ReadSparseList<Vector4>(() =>
                    {
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
            return this.TryWrite(entry, _preferableVersion, content);
        }

        public bool TryWrite(IFileSystemEntry entry, Urho3DModelVersion version, FileContainer content)
        {

            var singleMesh = content;
            if (content.Meshes.Count > 1)
                singleMesh = new MergeOperator().Transform(content);
            if (content.Meshes.Count == 0)
                return false;

            var flags = new Urho3DModelVersionFlags(version);
            using var stream = entry.OpenWrite();
            using var writer = new BinaryWriter(stream);
            writer.Write((byte)flags.Magic[0]);
            writer.Write((byte)flags.Magic[1]);
            writer.Write((byte)flags.Magic[2]);
            writer.Write((byte)flags.Magic[3]);
            if (flags.HasVersion)
                writer.Write((uint)flags.Version);

            var mesh = content.Meshes[0].AsUnified();
            if (!mesh.HasAttribute(MeshAttributeKey.Normal))
                mesh = new EnsureNormalsOperator().Transform(mesh).AsUnified();
            if (!mesh.HasAttribute(MeshAttributeKey.Tangent))
                mesh = new EnsureTangentsOperator().Transform(mesh).AsUnified();

            // uint       Number of vertex buffers
            writer.Write((uint)1);

            var hasPositions = mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions);
            var hasTexCoords = mesh.TryGetAttribute<Vector2>(MeshAttributeKey.TexCoord, out var texCoords);
            var hasNormals = mesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var normals);
            var hasTangents = mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Tangent, out var tangents);
            var hasColors = mesh.TryGetAttribute<Vector4>(MeshAttributeKey.Color, out var colors);

            // uint Vertex count
            writer.Write((uint)positions.Count);

            if (flags.HasVertexDeclarations)
            {
                var elements = new List<VertexElement>();
                if (hasPositions)
                    elements.Add(VertexElement.LegacyVertexElements[0]);
                if (hasNormals)
                    elements.Add(VertexElement.LegacyVertexElements[1]);
                if (hasColors)
                    elements.Add(VertexElement.LegacyVertexElements[2]);
                if (hasTexCoords)
                    elements.Add(VertexElement.LegacyVertexElements[3]);
                if (hasTangents)
                    elements.Add(VertexElement.LegacyVertexElements[7]);

                writer.Write((uint)elements.Count);
                foreach (var el in elements)
                {
                    writer.Write((byte)el.Type);
                    writer.Write((byte)el.Semantic);
                    writer.Write((ushort)el.Index);
                }
            }
            else
            {
                // uint Legacy vertex element mask(determines vertex size)
                uint elementMask = 0;
                if (hasPositions)
                    elementMask |= (uint)VertexMaskFlags.MASK_POSITION;
                if (hasNormals)
                    elementMask |= (uint)VertexMaskFlags.MASK_NORMAL;
                if (hasColors)
                    elementMask |= (uint)VertexMaskFlags.MASK_COLOR;
                if (hasTexCoords)
                    elementMask |= (uint)VertexMaskFlags.MASK_TEXCOORD1;
                if (hasTangents)
                    elementMask |= (uint)VertexMaskFlags.MASK_TANGENT;
                writer.Write((uint)elementMask);
            }
            // uint Morphable vertex range start index
            writer.Write((uint)0);
            // uint Morphable vertex count
            writer.Write((uint)0);
            // byte[] Vertex data(vertex count * vertex size)
            for (int vertexIndex=0; vertexIndex<positions.Count; ++vertexIndex)
            {
                if (hasPositions)
                    writer.Write(positions[vertexIndex]);
                if (hasNormals)
                    writer.Write(normals[vertexIndex]);
                if (hasColors)
                {
                    var c = Vector4.Clamp(colors[vertexIndex], Vector4.Zero, Vector4.One)*255.0f;
                    writer.Write((byte)colors[vertexIndex].X);
                    writer.Write((byte)colors[vertexIndex].Y);
                    writer.Write((byte)colors[vertexIndex].Z);
                    writer.Write((byte)colors[vertexIndex].W);
                }
                if (hasTexCoords)
                    writer.Write(texCoords[vertexIndex]);
                if (hasTangents)
                    writer.Write(tangents[vertexIndex]);
            }

            // uint Number of index buffers
            writer.Write((uint)1);

            //uint Index count
            writer.Write((uint)mesh.Indices.Count);
            if (mesh.Indices.Count < ushort.MaxValue)
            {
                //uint Index size(2 for 16 - bit indices, 4 for 32 - bit indices)
                writer.Write((uint)2);
                //byte[] Index data(index count * index size)
                foreach (var i in mesh.Indices)
                {
                    writer.Write((ushort)i);
                }
            }
            else
            {
                //uint Index size(2 for 16 - bit indices, 4 for 32 - bit indices)
                writer.Write((uint)4);
                //byte[] Index data(index count * index size)
                foreach (var i in mesh.Indices)
                {
                    writer.Write((uint)i);
                }
            }

            //uint    Number of geometries
            var geometries = mesh.DrawCalls.ToLookup(_ => _.MaterialIndex).OrderBy(_ => _.Key).ToList();
            var lodIndices = mesh.DrawCalls.Select(_=>_.LodLevel).Distinct().OrderBy(_=>_).ToList();
            writer.Write((uint)geometries.Count());
            var perGeometryBBox = new List<BoundingBox3>();
            foreach (var geometry in geometries)
            {
                //uint Number of bone mapping entries
                writer.Write((uint)0);

                BoundingBox3 geomBbox = BoundingBox3.Empty;

                //uint       Number of LOD levels
                writer.Write((uint)lodIndices.Count);
                foreach (var lodIndex in lodIndices)
                {
                    var drawCall = geometry.Where(_ => _.LodLevel == lodIndex).FirstOrDefault();
                    //float      LOD distance
                    if (lodIndex >= mesh.Lods.Count)
                        throw new FormatException($"Unknown LOD level {lodIndex}");
                    writer.Write(mesh.Lods[lodIndex].Distance ?? 0.0f);
                    //uint       Primitive type
                    writer.Write((uint)GetPrimType(drawCall.Type));
                    //uint       Vertex buffer index
                    writer.Write((uint)0);
                    //uint       Index buffer index
                    writer.Write((uint)0);
                    //uint       Start index
                    writer.Write((uint)drawCall.StartIndex);
                    //uint       Index count
                    writer.Write((uint)drawCall.NumIndices);

                    for (int i=drawCall.StartIndex; i<drawCall.NumIndices; ++i)
                    {
                        geomBbox = geomBbox.Merge(positions[mesh.Indices[i]]);
                    }
                }

                perGeometryBBox.Add(geomBbox);
            }

            //uint Number of vertex morphs (may be 0)
            writer.Write((uint)0);

            //uint       Number of bones (may be 0)
            writer.Write((uint)0);

            var bbox = new BoundingBox3(positions);
            writer.Write(bbox.Min);
            writer.Write(bbox.Max);

            foreach (var perGeom in perGeometryBBox)
            {
                writer.Write((perGeom.Min + perGeom.Max) * 0.5f);
            }

            return true;
        }
    }

}
