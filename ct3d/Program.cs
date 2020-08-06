using ct3d.WorldPrimitives;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;
using System;

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
                Flags = ContextFlags.Debug | ContextFlags.ForwardCompatible
            })
        {
        }

        Terrain terrain;

        protected override void OnLoad()
        {
            MakeCurrent();

            terrain = new Terrain(5, 5);

            base.OnLoad();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            terrain.Render();

            base.OnRenderFrame(args);
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
