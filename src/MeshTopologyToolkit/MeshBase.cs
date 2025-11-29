using System.Collections.Generic;

namespace MeshTopologyToolkit
{
    public class MeshBase
    {
        IList<MeshDrawCall> _drawCalls = new List<MeshDrawCall>();

        IList<MeshLod> _meshLods = new List<MeshLod>();

        public MeshBase(string? name = null)
        {
            Name = name;
            _meshLods.Add(new MeshLod());
        }

        public IList<MeshDrawCall> DrawCalls => _drawCalls;

        /// <summary>
        /// The list of Level Of Detail definitions.
        /// </summary>
        public IList<MeshLod> Lods => _meshLods;

        public string? Name { get; set; }
    }
}
