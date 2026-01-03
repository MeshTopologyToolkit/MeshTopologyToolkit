using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using MeshTopologyToolkit.Operators;
using System.Numerics;
using static Avalonia.OpenGL.GlConsts;

namespace MeshTopologyToolkit.Viewer;

public class SceneViewerControl : OpenGlControlBase
{
    private MeshShader _myShader;
    private FileContainer _content;
    private List<DrawableMesh> _drawables;

    // Camera settings
    private float _moveSpeed = 0.5f;
    private float _lookSensitivity = 0.2f;

    // State tracking
    private Point _lastMousePosition;
    private bool _isRightMouseDown;
    private readonly HashSet<Key> _pressedKeys = new();

    // Euler angles for rotation
    private float _yaw = 0f;
    private float _pitch = 0f;
    private float _roll = 0f;
    private Vector3 _cameraTarget = Vector3.Zero;
    private float _cameraDistance = 5.0f;
    const float DegToRad = MathF.PI / 180f;

    public SceneViewerControl(FileContainer content)
    {
        _content = content;
        _content = new EnsureNormalsOperator().Transform(_content);

        // Timer for smooth movement (approx 60fps)
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (s, e) => UpdateCamera();
        timer.Start();
    }

    private void UpdateCamera()
    {
        if (_pressedKeys.Count == 0) return;

        var rotation = Quaternion.CreateFromYawPitchRoll(_yaw * DegToRad, _pitch * DegToRad, _roll * DegToRad);
        var forward = Vector3.Transform(-Vector3.UnitZ, rotation);

        var right = Vector3.Cross(forward, new Vector3(0, 1, 0));

        var movement = Vector3.Zero;

        if (_pressedKeys.Contains(Key.W)) movement += forward;
        if (_pressedKeys.Contains(Key.S)) movement -= forward;
        if (_pressedKeys.Contains(Key.A)) movement -= right;
        if (_pressedKeys.Contains(Key.D)) movement += right;

        if (movement != Vector3.Zero)
        {
            _cameraTarget += movement* _moveSpeed;

            RequestNextFrameRendering();
        }

    }

    public void HandlePointerPressed(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            _isRightMouseDown = true;
            _lastMousePosition = e.GetPosition(this);
            Cursor = new Cursor(StandardCursorType.SizeAll);
        }
        base.OnPointerPressed(e);
    }

    public void HandlePointerReleased(PointerReleasedEventArgs e)
    {
        _isRightMouseDown = false;
        Cursor = Cursor.Default;
        base.OnPointerReleased(e);
    }

    public void HandlePointerMoved(PointerEventArgs e)
    {
        if (_isRightMouseDown)
        {
            var currentPos = e.GetPosition(this);
            var deltaX = (float)(currentPos.X - _lastMousePosition.X);
            var deltaY = (float)(currentPos.Y - _lastMousePosition.Y);

            if (deltaX != 0 || deltaY != 0)
            {
                _yaw -= deltaX * _lookSensitivity;
                _pitch = Math.Clamp(_pitch - (deltaY * _lookSensitivity), -89, 89);
                RequestNextFrameRendering();
            }

            _lastMousePosition = currentPos;
        }
    }

    public void HandleKeyDown(KeyEventArgs e)
    {
        _pressedKeys.Add(e.Key);
        base.OnKeyDown(e);
    }

    public void HandleKeyUp(KeyEventArgs e)
    {
        _pressedKeys.Remove(e.Key);
        base.OnKeyUp(e);
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        string? version = gl.GetString(GL_VERSION);
        Console.WriteLine($"GL Version: {version}");

        _drawables = new List<DrawableMesh>();
        var meshes = new Dictionary<IMesh, Mesh>();
        var bboxes = new Dictionary<IMesh, BoundingBox3>();
        var overallBbox = BoundingBox3.Empty;
        foreach (var node in _content.Scenes.First().VisitAllChildren())
        {
            var mesh = node.Mesh?.Mesh;
            if (mesh != null)
            {
                var nodeMatrix = node.GetWorldSpaceTransform().ToMatrix();

                if (!bboxes.TryGetValue(mesh, out var bbox))
                {
                    var positions = mesh.GetAttribute<Vector3>(MeshAttributeKey.Position);
                    bbox = new BoundingBox3(positions);
                    bboxes[mesh] = bbox;
                }

                overallBbox = overallBbox.Merge(bbox.Transform(nodeMatrix));

                if (!meshes.TryGetValue(mesh, out var existingMesh))
                {
                    existingMesh = BuildMesh(gl, mesh);
                    meshes[mesh] = existingMesh;
                }

                _drawables.Add(new DrawableMesh { Mesh = existingMesh , ModelMatrix = nodeMatrix });
            }

            if (overallBbox.IsEmpty)
                overallBbox = new BoundingBox3(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

            _cameraTarget = overallBbox.Center();
            _cameraDistance = overallBbox.Size().Length() * 2.0f;
        }

        _myShader = new MeshShader(gl);

        gl.CheckError();
    }

    private Mesh BuildMesh(GlInterface gl, IMesh mesh)
    {
        var unifiedMesh = mesh.AsUnified();

        var hasPositions = unifiedMesh.TryGetAttribute<Vector3>(MeshAttributeKey.Position, out var positions);
        var hasNormals = unifiedMesh.TryGetAttribute<Vector3>(MeshAttributeKey.Normal, out var normals);
        var hasColors = unifiedMesh.TryGetAttribute<Vector4>(MeshAttributeKey.Color, out var colors);

        const int componentsPerVertex = 10;
        var dataList = new float[positions.Count*componentsPerVertex];
        for (int i=0; i<positions.Count; i++)
        {
            dataList[i * componentsPerVertex + 0] = positions[i].X;
            dataList[i * componentsPerVertex + 1] = positions[i].Y;
            dataList[i * componentsPerVertex + 2] = positions[i].Z;
            dataList[i * componentsPerVertex + 3] = normals[i].X;
            dataList[i * componentsPerVertex + 4] = normals[i].Y;
            dataList[i * componentsPerVertex + 5] = normals[i].Z;
            if (hasColors)
            {
                dataList[i * componentsPerVertex + 6] = colors[i].X;
                dataList[i * componentsPerVertex + 7] = colors[i].Y;
                dataList[i * componentsPerVertex + 8] = colors[i].Z;
                dataList[i * componentsPerVertex + 9] = colors[i].W;
            }
            else
            {
                dataList[i * componentsPerVertex + 6] = 1.0f;
                dataList[i * componentsPerVertex + 7] = 1.0f;
                dataList[i * componentsPerVertex + 8] = 1.0f;
                dataList[i * componentsPerVertex + 9] = 1.0f;
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

        var rotation = Quaternion.CreateFromYawPitchRoll(_yaw * DegToRad, _pitch * DegToRad, _roll * DegToRad);
        var forward = Vector3.Transform(-Vector3.UnitZ, rotation);

        var cameraPosition = _cameraTarget - forward * _cameraDistance;

        var camera = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(cameraPosition);
        Matrix4x4.Invert(camera, out var view);

        //var view = Matrix4x4.CreateLookAt(
        //    new Vector3(MathF.Cos(_yaw), 1, MathF.Sin(_yaw)) * 5.0f,
        //    Vector3.Zero,         // Looking at the center
        //    Vector3.UnitY);       // Y is up

        foreach (var drawable in _drawables)
        {
            drawable.Mesh.Render(_myShader, drawable.ModelMatrix, view, projection);
        }

        gl.CheckError();
    }
}
