using Avalonia.OpenGL;

namespace MeshTopologyToolkit.Viewer;

public class DrawCall
{
    public const int GL_POINTS = 0x0000;
    public const int GL_LINES = 0x0001;
    public const int GL_LINE_LOOP = 0x0002;
    public const int GL_LINE_STRIP = 0x0003;
    public const int GL_TRIANGLES = 0x0004;
    public const int GL_TRIANGLE_STRIP = 0x0005;
    public const int GL_TRIANGLE_FAN = 0x0006;

    public const int GL_BYTE = 0x1400;
    public const int GL_UNSIGNED_BYTE = 0x1401;
    public const int GL_SHORT = 0x1402;
    public const int GL_UNSIGNED_SHORT = 0x1403;
    public const int GL_INT = 0x1404;
    public const int GL_UNSIGNED_INT = 0x1405;
    public const int GL_FLOAT = 0x1406;
    public const int GL_FIXED = 0x140C;

    public int Type { get; set; } = GL_TRIANGLES;
    public int IndexOffset { get; set; } // Where in the index buffer to start
    public int IndexCount { get; set; }  // How many indices to draw

    public void Execute(GlInterface gl)
    {
        unsafe
        {
            gl.DrawElements(Type, IndexCount, GL_UNSIGNED_INT, new IntPtr(IndexOffset * sizeof(ushort)));
            gl.CheckError();
        }
    }
}
