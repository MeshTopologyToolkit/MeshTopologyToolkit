using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

class Program
{
    static void Main()
    {
        // Create window and graphics device
        var windowCI = new Veldrid.StartupUtilities.WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 800,
            WindowHeight = 600,
            WindowTitle = "MeshTopologyToolkit Viewer",
            WindowInitialState = WindowState.Maximized
        };

        var window = Veldrid.StartupUtilities.VeldridStartup.CreateWindow(ref windowCI);
        GraphicsDevice gd = Veldrid.StartupUtilities.VeldridStartup.CreateGraphicsDevice(window, GraphicsBackend.OpenGL);

        // Vertex data for a triangle
        var vertices = new[]
        {
            new VertexPositionColor(new Vector2(0f, 0.5f), RgbaFloat.Red),
            new VertexPositionColor(new Vector2(0.5f, -0.5f), RgbaFloat.Green),
            new VertexPositionColor(new Vector2(-0.5f, -0.5f), RgbaFloat.Blue)
        };

        // Create vertex buffer
        DeviceBuffer vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)(vertices.Length * VertexPositionColor.SizeInBytes),
            BufferUsage.VertexBuffer));
        gd.UpdateBuffer(vertexBuffer, 0, vertices);

        Shader[] shaders = gd.ResourceFactory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(VertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragmentCode), "main"));

        // Define vertex layout
        VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4)
        );

        // Create pipeline
        GraphicsPipelineDescription pipelineDesc = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleOverrideBlend,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = Array.Empty<ResourceLayout>(),
            ShaderSet = new ShaderSetDescription(new[] { vertexLayout }, shaders),
            Outputs = gd.SwapchainFramebuffer.OutputDescription
        };
        Pipeline pipeline = gd.ResourceFactory.CreateGraphicsPipeline(pipelineDesc);

        // Main render loop
        while (window.Exists)
        {
            window.PumpEvents();

            CommandList cl = gd.ResourceFactory.CreateCommandList();
            cl.Begin();
            cl.SetFramebuffer(gd.SwapchainFramebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Black);
            cl.SetPipeline(pipeline);
            cl.SetVertexBuffer(0, vertexBuffer);
            cl.Draw(3);
            cl.End();
            gd.SubmitCommands(cl);
            if (window.Exists)
                gd.SwapBuffers();
            cl.Dispose();
        }

        // Cleanup
        foreach (var shader in shaders)
            shader.Dispose();
        pipeline.Dispose();
        vertexBuffer.Dispose();
        gd.Dispose();
    }

    struct VertexPositionColor
    {
        public Vector2 Position;
        public RgbaFloat Color;
        public const uint SizeInBytes = 2 * 4 + 4 * 4; // Vector2 + RgbaFloat

        public VertexPositionColor(Vector2 pos, RgbaFloat color)
        {
            Position = pos;
            Color = color;
        }
    }

    private static string VertexCode = @"
#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

    private static string FragmentCode = @"
#version 450
layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;
void main()
{
    fsout_Color = fsin_Color;
}";
}
