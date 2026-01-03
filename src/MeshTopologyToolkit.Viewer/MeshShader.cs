using Avalonia.OpenGL;
using System.Runtime.InteropServices;
using static Avalonia.OpenGL.GlConsts;

namespace MeshTopologyToolkit.Viewer;

public class MeshShader : IDisposable
{
    private readonly GlInterface _gl;
    public int Program { get; private set; }

    // Uniform Locations
    public int ModelLoc { get; }
    public int ViewLoc { get; }
    public int ProjectionLoc { get; }

    public MeshShader(GlInterface gl)
    {
        _gl = gl;

        string vsSource = @"
            attribute vec3 aPos;
            attribute vec3 aNormal;
            attribute vec4 aColor;

            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;

            varying vec4 ourColor;

            void main() {
                gl_Position = projection * view * model * vec4(aPos, 1.0);
                vec3 n = (model * vec4(aNormal, 0.0)).xyz;
                vec3 eye = vec3(view[0][2], view[1][2], view[2][2]);
                float scale = dot(normalize(n), eye) * 0.5 + 0.5;
                ourColor = vec4(aColor.xyz * scale, aColor.w);
            }";

        string fsSource = @"
            precision mediump float;
            varying vec4 ourColor;
            //DECLAREGLFRAG

            void main() {
                gl_FragColor = ourColor;
            }";

        Program = CompileProgram(gl, vsSource, fsSource);
        gl.CheckError();
        Use();

        ModelLoc = gl.GetUniformLocationString(Program, "model");
        gl.CheckError();
        ViewLoc = gl.GetUniformLocationString(Program, "view");
        gl.CheckError();
        ProjectionLoc = gl.GetUniformLocationString(Program, "projection");
        gl.CheckError();
    }

    private string GetShader(GlInterface gl, bool fragment, string shader)
    {
        var GlVersion = gl.ContextInfo.Version;

        var version = (GlVersion.Type == GlProfileType.OpenGL
            ? RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 150 : 120
            : 100);
        var data = "#version " + version + "\n";
        if (GlVersion.Type == GlProfileType.OpenGLES)
            data += "precision mediump float;\n";
        if (version >= 150)
        {
            shader = shader.Replace("attribute", "in");
            if (fragment)
                shader = shader
                    .Replace("varying", "in")
                    .Replace("//DECLAREGLFRAG", "out vec4 outFragColor;")
                    .Replace("gl_FragColor", "outFragColor");
            else
                shader = shader.Replace("varying", "out");
        }

        data += shader;

        return data;
    }

    private int CompileProgram(GlInterface gl, string vs, string fs)
    {
        int vShader = gl.CreateShader(GL_VERTEX_SHADER);
        gl.CheckError();
        CompileShader(gl, vShader, GetShader(gl, false, vs));

        int fShader = gl.CreateShader(GL_FRAGMENT_SHADER);
        gl.CheckError();
        CompileShader(gl, fShader, GetShader(gl, true, fs));

        int prog = gl.CreateProgram();
        gl.CheckError();
        gl.AttachShader(prog, vShader);
        gl.CheckError();
        gl.AttachShader(prog, fShader);
        gl.CheckError();

        gl.BindAttribLocationString(prog, 0, "aPos");
        gl.CheckError();
        gl.BindAttribLocationString(prog, 1, "aNormal");
        gl.CheckError();
        gl.BindAttribLocationString(prog, 2, "aColor");
        gl.CheckError();

        var error = gl.LinkProgramAndGetError(prog);

        if (!string.IsNullOrEmpty(error))
            throw new Exception($"Shader Link Error: {error}");

        return prog;
    }

    private void CompileShader(GlInterface gl, int vShader, string vs)
    {
        var error = gl.CompileShaderAndGetError(vShader, vs);
        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine(error);
            throw new Exception(error);
        }
    }

    public void Use()
    {
        _gl.UseProgram(Program);
        _gl.CheckError();
    }
    public void Dispose()
    {
        _gl.DeleteProgram(Program);
        Program = -1;
    }
}
