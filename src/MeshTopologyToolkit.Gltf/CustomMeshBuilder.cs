using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace MeshTopologyToolkit.Gltf
{
    internal class CustomMeshBuilder : IMeshBuilder<MaterialBuilder>
    {
        private List<IPrimitiveReader<MaterialBuilder>> _primitives = new List<IPrimitiveReader<MaterialBuilder>>();

        public CustomMeshBuilder(string name, UnifiedIndexedMesh sourceMesh, IReadOnlyList<MaterialBuilder> materials)
        {
            Name = name;
            Materials = materials;

            for (int i = 0; i < sourceMesh.DrawCalls.Count; i++)
            {
                MeshDrawCall drawCall = sourceMesh.DrawCalls[i];
                _primitives.Add(new CustomPrimitiveReader(sourceMesh, drawCall, materials[i]));
            }
        }

        public string Name { get; set; }

        public JsonNode Extras { get; set; } = new JsonObject();

        public bool IsEmpty => _primitives.Count == 0;

        public IEnumerable<MaterialBuilder> Materials { get; set; }

        public IReadOnlyCollection<IPrimitiveReader<MaterialBuilder>> Primitives => _primitives;

        public IMeshBuilder<MaterialBuilder> Clone(Func<MaterialBuilder, MaterialBuilder>? materialCloneCallback = null)
        {
            throw new NotImplementedException();
        }

        public IMorphTargetBuilder UseMorphTarget(int index)
        {
            throw new NotImplementedException();
        }

        public IPrimitiveBuilder UsePrimitive(MaterialBuilder material, int primitiveVertexCount = 3)
        {
            throw new NotImplementedException();
        }

        public void Validate()
        {
        }
    }
}