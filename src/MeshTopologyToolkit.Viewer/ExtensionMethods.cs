using Avalonia.OpenGL;
using static Avalonia.OpenGL.GlConsts;

namespace MeshTopologyToolkit.Viewer;

public static class ExtensionMethods
{
    public static void CheckError(this GlInterface gl)
    {
        int err;
        for (; ;)
        {
            err = gl.GetError();
            if (err == GL_NO_ERROR)
                return;
            Console.WriteLine($"OpenGL Error {err}: {GetGLErrorString(err)}");
        }
    }

    private static string GetGLErrorString(int errorCode)
    {
        if (errorCode == 0) return "GL_NO_ERROR";

        return errorCode switch
        {
            0x0500 => "GL_INVALID_ENUM",
            0x0501 => "GL_INVALID_VALUE",
            0x0502 => "GL_INVALID_OPERATION",
            0x0503 => "GL_STACK_OVERFLOW",
            0x0504 => "GL_STACK_UNDERFLOW",
            0x0505 => "GL_OUT_OF_MEMORY",
            0x0506 => "GL_INVALID_FRAMEBUFFER_OPERATION",
            _ => $"Unknown Error: 0x{errorCode:X}"
        };
    }
}
