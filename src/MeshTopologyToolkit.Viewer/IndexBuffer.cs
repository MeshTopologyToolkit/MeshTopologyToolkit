using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace MeshTopologyToolkit.Viewer;

public class IndexBuffer : IDisposable
{
    private readonly GlInterface _gl;
    public int Id { get; }
    public int Count { get; }

    public IndexBuffer(GlInterface gl, ReadOnlySpan<int> indices)
    {
        _gl = gl;
        Id = _gl.GenBuffer();
        _gl.CheckError();
        Count = indices.Length;
        _gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, Id);
        _gl.CheckError();
        unsafe
        {
            fixed (int* p = indices)
            {
                _gl.BufferData(GL_ELEMENT_ARRAY_BUFFER, new IntPtr(indices.Length * sizeof(int)), new IntPtr(p), GL_STATIC_DRAW);
                _gl.CheckError();
            }
        }
    }

    public void Bind()
    {
        _gl.BindBuffer(GL_ELEMENT_ARRAY_BUFFER, Id);
        _gl.CheckError();
    }
    public void Dispose() => _gl.DeleteBuffer(Id);
}
