using System.Numerics;

namespace MeshTopologyToolkit.Viewer;

public class DrawableMesh
{
    public Matrix4x4 ModelMatrix { get; set; } = Matrix4x4.Identity;
    public Mesh Mesh { get; set; }
}
