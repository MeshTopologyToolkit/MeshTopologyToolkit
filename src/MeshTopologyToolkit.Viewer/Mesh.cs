using Avalonia.OpenGL;
using System.Numerics;
using static Avalonia.OpenGL.GlConsts;

namespace MeshTopologyToolkit.Viewer;

public class Mesh : IDisposable
{
    private GlInterface _gl;

    public int Vao { get; }
    public VertexBuffer VB { get; }
    public IndexBuffer IB { get; }
    public List<DrawCall> DrawCalls { get; } = new();

    public Mesh(GlInterface gl, float[] vertices, int[] indices)
    {
        _gl = gl;
        Vao = _gl.GenVertexArray();
        _gl.CheckError();
        _gl.BindVertexArray(Vao);
        _gl.CheckError();

        VB = new VertexBuffer(_gl, vertices);
        IB = new IndexBuffer(_gl, indices);

        // Setup Layout (Location 0 = Pos, Location 1 = Color)
        unsafe
        {
            _gl.BindVertexArray(Vao);
            _gl.CheckError();
            int stride = 6 * sizeof(float);
            _gl.VertexAttribPointer(0, 3, GL_FLOAT, 0, stride, IntPtr.Zero);
            _gl.CheckError();
            _gl.EnableVertexAttribArray(0);
            _gl.CheckError();
            _gl.VertexAttribPointer(1, 3, GL_FLOAT, 0, stride, new IntPtr(3 * sizeof(float)));
            _gl.CheckError();
            _gl.EnableVertexAttribArray(1);
            _gl.CheckError();
        }
        _gl.BindVertexArray(0);
    }

    public void Render(MeshShader shader, Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
    {
        _gl.CheckError();
        VB.Bind();
        IB.Bind();
        _gl.BindVertexArray(Vao);
        _gl.CheckError();
        shader.Use();

        // Upload Matrices
        unsafe
        {
            _gl.UniformMatrix4fv(shader.ModelLoc, 1, false, &model);
            _gl.CheckError();
            _gl.UniformMatrix4fv(shader.ViewLoc, 1, false, &view);
            _gl.CheckError();
            _gl.UniformMatrix4fv(shader.ProjectionLoc, 1, false, &projection);
            _gl.CheckError();
        }

        foreach (var call in DrawCalls)
        {
            call.Execute(_gl);
        }

        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(Vao);
        VB.Dispose();
        IB.Dispose();
    }
}
