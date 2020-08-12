using ct3d.WorldPrimitives;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ct3d
{
    class Program : GameWindow
    {
        public Program() : base(
            new GameWindowSettings
            {
                RenderFrequency = 60,
                UpdateFrequency = 60,
                IsMultiThreaded = false,
            },
            new NativeWindowSettings
            {
                Profile = ContextProfile.Any,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 6),
                StartFocused = true,
                StartVisible = true,
                Size = new Vector2i(800, 600),
                Title = "Shitty TTD Clone",
                Flags = ContextFlags.ForwardCompatible,
            })
        {
        }

        GameState gameState;
        Terrain terrain;

        protected unsafe override void OnLoad()
        {
            MakeCurrent();

            gameState = new GameState { WindowSize = Size };

            VSync = VSyncMode.Off;

            // enable debug messages
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GL.DebugMessageCallback((src, type, id, severity, len, msg, usr) =>
            {
                if (severity > DebugSeverity.DebugSeverityNotification)
                    Console.WriteLine($"GL ERROR {Encoding.ASCII.GetString((byte*)msg, len)}, type: {type}, severity: {severity}, source: {src}");
            }, IntPtr.Zero);

            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color4.Aqua);

            var rng = new Random();
            terrain = new Terrain(200, 200, gameState);
            terrain[1, 1] = terrain[1, 2] = terrain[2, 1] = terrain[2, 2] = 1;

            gameState.ProjectionWorldUniformBufferObject.Value.World = Matrix4.LookAt(2, -2, 5, 2, 5, 0, 0, 0, 1);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            gameState.WindowSize = e.Size;

            GL.Viewport(0, 0, e.Width, e.Height);

            // set up the projection matrix
            gameState.ProjectionWorldUniformBufferObject.Value.Projection = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 2, (float)Size.Y / Size.X, 0.1f, 20f);
            gameState.ProjectionWorldUniformBufferObject.Upload();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            gameState.MousePosition = e.Position;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
        }

        readonly Stopwatch stopwatch = Stopwatch.StartNew();
        readonly List<double> frameTimesMs = new List<double>();

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var start = stopwatch.Elapsed;
            terrain.Render();

            SwapBuffers();

            frameTimesMs.Add((stopwatch.Elapsed - start).TotalMilliseconds);
            if (frameTimesMs.Count > 150)
            {
                Console.WriteLine($"Average frame times: {frameTimesMs.Average()}ms, max frame time: {frameTimesMs.Max()}ms");
                frameTimesMs.Clear();
            }
        }

        protected override void OnUnload()
        {
            terrain.Dispose();
            gameState.Dispose();
            base.OnUnload();
        }

        static void Main()
        {
            using var program = new Program();
            program.Run();
        }
    }
}
