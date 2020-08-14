using ct3d.WorldPrimitives;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;
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
            terrain = new Terrain(600, 600, 10, gameState);
            terrain.SetHeight(1, 1, 1);
            terrain.SetHeight(1, 2, 1);
            terrain.SetHeight(2, 1, 1);
            terrain.SetHeight(2, 2, 1);

            terrain.SetRoad(1, 1, TerrainRoadData.Down | TerrainRoadData.Right);
            terrain.SetRoad(1, 0, TerrainRoadData.Up | TerrainRoadData.Down);
            terrain.SetRoad(2, 1, TerrainRoadData.Left | TerrainRoadData.Right);
            terrain.SetRoad(3, 1, TerrainRoadData.Left | TerrainRoadData.Up | TerrainRoadData.Down);
            terrain.SetRoad(3, 2, TerrainRoadData.Down);
            terrain.SetRoad(3, 0, TerrainRoadData.Up | TerrainRoadData.Right);
            terrain.SetRoad(4, 0, TerrainRoadData.Left);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            gameState.WindowSize = e.Size;

            GL.Viewport(0, 0, e.Width, e.Height);

            // set up the projection matrix
            gameState.ProjectionWorldUniformBufferObject.Value.Projection = Matrix4.CreateOrthographic(10, 10f * Size.Y / Size.X, 0.1f, 20f);
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

            const float cameraStep = .1f;
            if (KeyboardState[Key.A])
                gameState.CameraPosition.X -= cameraStep;
            if (KeyboardState[Key.D])
                gameState.CameraPosition.X += cameraStep;
            if (KeyboardState[Key.S])
                gameState.CameraPosition.Y -= cameraStep;
            if (KeyboardState[Key.W])
                gameState.CameraPosition.Y += cameraStep;

            gameState.ProjectionWorldUniformBufferObject.Value.World =
                Matrix4.CreateRotationZ(MathF.PI / 4) *
                Matrix4.LookAt(
                    gameState.CameraPosition.X + 5, gameState.CameraPosition.Y, 8,
                    gameState.CameraPosition.X + 5, gameState.CameraPosition.Y + 5, 0,
                    0, 0, 1);

            gameState.ProjectionWorldUniformBufferObject.Upload();

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
