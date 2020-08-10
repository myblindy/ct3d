using ct3d.WorldPrimitives;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;
using System;
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
                Flags = ContextFlags.ForwardCompatible
            })
        {
        }

        readonly GameState gameState = new GameState();
        Terrain terrain;

        protected unsafe override void OnLoad()
        {
            gameState.WindowSize = Size;

            MakeCurrent();

            // enable debug messages
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GL.DebugMessageCallback((src, type, id, severity, len, msg, usr) =>
                Console.WriteLine($"GL ERROR {Encoding.ASCII.GetString((byte*)msg, len)}, type: {type}, severity: {severity}, source: {src}"), IntPtr.Zero);

            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(Color4.Aqua);

            var rng = new Random();
            terrain = new Terrain(10, 20, gameState);
            //for (int y = 0; y < terrain.Height; ++y)
            //    for (int x = 0; x < terrain.Width; ++x)
            //        terrain[x, y] = (byte)rng.Next(4);
            terrain[2, 2] = 1;

            var camera = Matrix4.LookAt(5, -2, 5, 5, 5, 0, 0, 0, 1);
            terrain.SetWorldMatrix(ref camera);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            gameState.WindowSize = e.Size;

            GL.Viewport(0, 0, e.Width, e.Height);

            // set up the projection matrix
            var projection = Matrix4.CreatePerspectiveFieldOfView(MathF.PI / 2, (float)Size.Y / Size.X, 0.1f, 20f);
            terrain.SetProjectionMatrix(ref projection);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            gameState.MousePosition = e.Position;
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            terrain.Render();

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            terrain.Dispose();
            base.OnUnload();
        }

        static void Main()
        {
            using var program = new Program();
            program.Run();
        }
    }
}
