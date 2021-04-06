using ct3d.WorldPrimitives;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
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
                Profile = ContextProfile.Core,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 6),
                StartFocused = true,
                StartVisible = true,
                Size = new(800, 600),
                Title = "Shitty TTD Clone",
                Flags = ContextFlags.ForwardCompatible,
            })
        {
        }

        GameState gameState;
        Terrain terrain;

        protected unsafe override void OnLoad()
        {
            base.OnLoad();

            gameState = new GameState { WindowSize = new(Size.X, Size.Y) };

            VSync = VSyncMode.Off;

            // enable debug messages
#if DEBUG
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GL.DebugMessageCallback((src, type, id, severity, len, msg, usr) =>
            {
                if (severity > DebugSeverity.DebugSeverityNotification)
                    Console.WriteLine($"GL ERROR {Encoding.ASCII.GetString((byte*)msg, len)}, type: {type}, severity: {severity}, source: {src}");
            }, IntPtr.Zero);
#endif

            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color4.Aqua);

            var rng = new Random();
            terrain = new Terrain(600, 600, 10, gameState);
            terrain.SetHeight(1, 1, 1);
            terrain.SetHeight(1, 2, 1);
            terrain.SetHeight(2, 1, 1);
            terrain.SetHeight(2, 2, 1);

            //terrain.SetRoad(1, 1, TerrainRoadData.Down | TerrainRoadData.Right);
            //terrain.SetRoad(1, 0, TerrainRoadData.Up | TerrainRoadData.Down);
            //terrain.SetRoad(2, 1, TerrainRoadData.Left | TerrainRoadData.Right);
            //terrain.SetRoad(3, 1, TerrainRoadData.Left | TerrainRoadData.Up | TerrainRoadData.Down);
            //terrain.SetRoad(3, 2, TerrainRoadData.Down);
            //terrain.SetRoad(3, 0, TerrainRoadData.Up | TerrainRoadData.Right);
            //terrain.SetRoad(4, 0, TerrainRoadData.Left);
            terrain.AddRoad(1, 1, TerrainRoadData.Right);
            terrain.AddRoad(3, 1, TerrainRoadData.Left);
            terrain.AddRoad(2, 1, TerrainRoadData.Right);
            terrain.AddRoad(2, 1, TerrainRoadData.Left);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            gameState.WindowSize = new(e.Size.X, e.Size.Y);

            GL.Viewport(0, 0, e.Width, e.Height);

            // set up the projection matrix
            gameState.CameraFrustrumSize = new(10, 10f * Size.Y / Size.X);
            gameState.ProjectionWorldUniformBufferObject.Value.Projection = Matrix4x4.CreateOrthographic(gameState.CameraFrustrumSize.X, gameState.CameraFrustrumSize.Y, 0.1f, 20f);

            gameState.ProjectionWorldUniformBufferObject.Upload();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            gameState.MousePosition = new(e.Position.X, e.Position.Y);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            const float cameraStep = .1f;
            if (KeyboardState[Keys.A])
                gameState.CameraPosition.X -= cameraStep;
            if (KeyboardState[Keys.D])
                gameState.CameraPosition.X += cameraStep;
            if (KeyboardState[Keys.S])
                gameState.CameraPosition.Y -= cameraStep;
            if (KeyboardState[Keys.W])
                gameState.CameraPosition.Y += cameraStep;

            gameState.ProjectionWorldUniformBufferObject.Value.View =
                //worldTransformFix *
                gameState.GetCameraTransform();

            gameState.ProjectionWorldUniformBufferObject.Upload();

            terrain.Update();

            if (IsMouseButtonDown(MouseButton.Button1) && terrain.SelectedCell != Terrain.NoSelectedCell)
                terrain.AddRoad((int)terrain.SelectedCell.X, (int)terrain.SelectedCell.Y,
                    Terrain.TerrainRoadDataFromUV(new(terrain.SelectedCell.X % 1, terrain.SelectedCell.Y % 1)));
        }

        readonly Stopwatch stopwatch = Stopwatch.StartNew();
        readonly List<double> frameTimesMs = new();

        static readonly Matrix4 worldTransformFix = Matrix4.CreateRotationZ(MathF.PI / 4);

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

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
