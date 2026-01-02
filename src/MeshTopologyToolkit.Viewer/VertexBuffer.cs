using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace MeshTopologyToolkit.Viewer;

public class VertexBuffer : IDisposable
{
    private readonly GlInterface _gl;
    public int Id { get; }

    public VertexBuffer(GlInterface gl, ReadOnlySpan<float> data)
    {
        _gl = gl;
        Id = _gl.GenBuffer();
        _gl.CheckError();

        _gl.BindBuffer(GL_ARRAY_BUFFER, Id);
        _gl.CheckError();

        unsafe
        {
            fixed (float* p = data)
            {
                _gl.BufferData(GL_ARRAY_BUFFER, new IntPtr(data.Length * sizeof(float)), new IntPtr(p), GL_STATIC_DRAW);
                _gl.CheckError();
            }
        }
    }

    public void Bind()
    {
        _gl.BindBuffer(GL_ARRAY_BUFFER, Id);
        _gl.CheckError();
    }
    public void Dispose() => _gl.DeleteBuffer(Id);
}
