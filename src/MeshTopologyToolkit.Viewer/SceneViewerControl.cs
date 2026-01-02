using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using System.Numerics;
using static Avalonia.OpenGL.GlConsts;

namespace MeshTopologyToolkit.Viewer;

public class SceneViewerControl : OpenGlControlBase
{
    private MeshShader _myShader;
    private FileContainer _content;
    private List<DrawableMesh> _drawables;
    private float yaw = 0f;

    public SceneViewerControl(FileContainer content)
    {
        _content = content;
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        string? version = gl.GetString(GL_VERSION);
        Console.WriteLine($"GL Version: {version}");

        _drawables = new List<DrawableMesh>();
        var meshes = new Dictionary<IMesh, Mesh>();
        foreach (var node in _content.Scenes.First().VisitAllChildren())
        {
            var mesh = node.Mesh?.Mesh;
            if (mesh != null)
            {
                if (!meshes.TryGetValue(mesh, out var existingMesh))
                {
                    existingMesh = BuildMesh(gl, mesh);
                    meshes[mesh] = existingMesh;
                }
                _drawables.Add(new DrawableMesh { Mesh = existingMesh , ModelMatrix = node.GetWorldSpaceTransform().ToMatrix() });
            }
        }

        _myShader = new MeshShader(gl);

        gl.CheckError();
    }

    private Mesh BuildMesh(GlInterface gl, IMesh mesh)
    {
        var unifiedMesh = mesh.AsUnified();

        var hasPositions = unifiedMesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions);
        var hasColors = unifiedMesh.TryGetAttribute<Vector3>(MeshAttributeKey.Color, out var colors);

        var dataList = new float[positions.Count*6];
        for (int i=0; i<positions.Count; i++)
        {
            dataList[i * 6 + 0] = positions[i].X;
            dataList[i * 6 + 1] = positions[i].Y;
            dataList[i * 6 + 2] = positions[i].Z;
            if (hasColors)
            {
                dataList[i * 6 + 3] = colors[i].X;
                dataList[i * 6 + 4] = colors[i].Y;
                dataList[i * 6 + 5] = colors[i].Z;
            }
            else
            {
                dataList[i * 6 + 3] = 1.0f;
                dataList[i * 6 + 4] = 1.0f;
                dataList[i * 6 + 5] = 1.0f;
            }
        }
 
        var drawableMesh = new Mesh(gl, dataList, unifiedMesh.Indices.ToArray());
        foreach (var drawCall in unifiedMesh.DrawCalls)
        {
            drawableMesh.DrawCalls.Add(new DrawCall
            {
                Type = GetType(drawCall.Type),
                IndexOffset = drawCall.StartIndex,
                IndexCount = drawCall.NumIndices
            });
        }
        return drawableMesh;
    }

    private int GetType(MeshTopology type)
    {
        switch (type)
        {
            case MeshTopology.Points:
                return DrawCall.GL_POINTS;
            case MeshTopology.LineList:
                return DrawCall.GL_LINES;
            case MeshTopology.LineStrip:
                return DrawCall.GL_LINE_STRIP;
            case MeshTopology.TriangleList:
                return DrawCall.GL_TRIANGLES;
            case MeshTopology.TriangleStrip:
                return DrawCall.GL_TRIANGLE_STRIP;
            default:
                throw new NotSupportedException($"Unsupported topology type: {type}");
        }
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        gl.Viewport(0, 0, (int)Bounds.Width, (int)Bounds.Height);
        //gl.Enable(GL_CULL_FACE);
        gl.Disable(GL_CULL_FACE);
        //gl.Disable(GL_SCISSOR_TEST);
        //gl.DepthFunc(GL_LESS);
        gl.CheckError();
        gl.ClearColor(0.1f, 0.2f, 0.6f, 1f);
        gl.CheckError();
        gl.Clear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        gl.CheckError();
        gl.Enable(GL_DEPTH_TEST);
        gl.CheckError();

        float aspect = (float)(Bounds.Width / Bounds.Height);

        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            (float)Math.PI / 4, aspect, 0.1f, 1000f);

        yaw += 0.01f;

        var view = Matrix4x4.CreateLookAt(
            new Vector3(MathF.Cos(yaw), 1, MathF.Sin(yaw)) * 5.0f,
            Vector3.Zero,         // Looking at the center
            Vector3.UnitY);       // Y is up

        foreach (var drawable in _drawables)
        {
            drawable.Mesh.Render(_myShader, drawable.ModelMatrix, view, projection);
        }

        gl.CheckError();

        // Request next frame for animation if needed
        RequestNextFrameRendering();
    }
}
