using ct3d.RenderPrimitives;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.WorldPrimitives
{
    sealed class Terrain : IDisposable
    {
        readonly byte[] heightMap;

        public int Width { get; }
        public int Height { get; }

        readonly ShaderProgram shader = new ShaderProgram("terrain");

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vertex
        {
            public Vector3 Position, Normal;
            public Color4 Color;
        }
        readonly VertexIndexBuffer<Vertex, ushort> vertexIndexBuffer;

        private bool disposedValue;

        public Terrain(int w, int h)
        {
            (Width, Height, heightMap) = (w, h, new byte[w * h]);

            vertexIndexBuffer = new VertexIndexBuffer<Vertex, ushort>(new[]
            {
                new Vertex { Position = new Vector3(0, 1, -2), Normal = new Vector3(0, 0, -1), Color = Color4.Red },
                new Vertex { Position = new Vector3(-1, 0, -2), Normal = new Vector3(0, 0, -1), Color = Color4.Green },
                new Vertex { Position = new Vector3(1, 0, -2), Normal = new Vector3(0, 0, -1), Color = Color4.Blue },
            }, new ushort[] { 0, 1, 2 });
        }

        public byte this[int x, int y]
        {
            get => heightMap[x * Width + y];
            set => heightMap[x * Width + y] = value;
        }

        public void Render()
        {
            shader.Use();
            vertexIndexBuffer.DrawArrays(PrimitiveType.Triangles, 0, 3);

        }

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed objects
                }

                // unmanaged objects
                shader.Dispose();
                vertexIndexBuffer.Dispose();

                disposedValue = true;
            }
        }

        ~Terrain()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
