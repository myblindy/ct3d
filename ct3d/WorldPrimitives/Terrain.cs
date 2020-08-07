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

        public bool Dirty { get; set; } = true;

        private ShaderProgram TerrainShader { get; } = new ShaderProgram("terrain");
        private ShaderProgram GridShader { get; } = new ShaderProgram("terrain.grid");

        public void SetWorldMatrix(ref Matrix4 camera)
        {
            TerrainShader.ProgramUniform("world", ref camera);
            GridShader.ProgramUniform("world", ref camera);
        }

        public void SetProjectionMatrix(ref Matrix4 projection)
        {
            TerrainShader.ProgramUniform("projection", ref projection);
            GridShader.ProgramUniform("projection", ref projection);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vertex
        {
            public Vector3 Position, Normal;
            public Color4 Color;
        }
        VertexIndexBuffer<Vertex, ushort> vertexIndexBuffer;

        private bool disposedValue;

        unsafe (Vertex[], ushort[]) BuildTerrainVertices(Range xRange, Range yRange)
        {
            var vertices = new Vertex[(xRange.End.Value - xRange.Start.Value) * (yRange.End.Value - yRange.Start.Value) * 6];
            var indices = new ushort[
                2 * (xRange.End.Value - xRange.Start.Value) * (yRange.End.Value - yRange.Start.Value - 1) +
                2 * (xRange.End.Value - xRange.Start.Value - 1) * (yRange.End.Value - yRange.Start.Value)];

            fixed (Vertex* firstVertex = vertices)
            fixed (ushort* firstIndex = indices)
            {
                var vertex = firstVertex;
                var index = firstIndex;

                for (int x = xRange.Start.Value; x < xRange.End.Value - 1; ++x)
                    for (int y = yRange.Start.Value; y < yRange.End.Value - 1; ++y)
                    {
                        var x0 = x - xRange.Start.Value;
                        var y0 = y - yRange.Start.Value;
                        var color = Color4.Green;

                        static float transformHeight(byte heightValue) => heightValue / 4f;
                        float zXY = transformHeight(this[x, y]);
                        float zX1Y1 = transformHeight(this[x + 1, y + 1]);

                        // the 2 triangles that compose each quad of ground
                        *vertex++ = new Vertex { Position = new Vector3(x0, y0, zXY), Color = color };
                        *vertex++ = new Vertex { Position = new Vector3(x0 + 1, y0, transformHeight(this[x + 1, y])), Color = color };
                        *vertex++ = new Vertex { Position = new Vector3(x0 + 1, y0 + 1, zX1Y1), Color = color };

                        *vertex++ = new Vertex { Position = new Vector3(x0, y0, zXY), Color = color };
                        *vertex++ = new Vertex { Position = new Vector3(x0 + 1, y0 + 1, zX1Y1), Color = color };
                        *vertex++ = new Vertex { Position = new Vector3(x0, y0 + 1, transformHeight(this[x, y + 1])), Color = color };

                        // use the index buffer to draw the grid lines with the vertices above
                        if (y0 > 0)
                        {
                            *index++ = (ushort)(vertex - firstVertex - 6);
                            *index++ = (ushort)(vertex - firstVertex - 5);
                        }

                        if (x0 > 0)
                        {
                            *index++ = (ushort)(vertex - firstVertex - 6);
                            *index++ = (ushort)(vertex - firstVertex - 1);
                        }
                    }
            }

            return (vertices, indices);
        }

        public Terrain(int w, int h)
        {
            (Width, Height, heightMap) = (w, h, new byte[w * h]);
        }

        public byte this[int x, int y]
        {
            get => heightMap[x * Width + y];
            set { heightMap[x * Width + y] = value; Dirty = true; }
        }

        public void Render()
        {
            if (Dirty)
            {
                var (vertices, indices) = BuildTerrainVertices(0..5, 0..5);
                if (vertexIndexBuffer is null)
                    vertexIndexBuffer = new VertexIndexBuffer<Vertex, ushort>(vertices, indices);
                else
                    vertexIndexBuffer.Update(vertices, indices);

                Dirty = false;
            }

            TerrainShader.Use();
            vertexIndexBuffer.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexIndexBuffer.VertexCount);

            GridShader.Use();
            GL.DrawElements(BeginMode.Lines, vertexIndexBuffer.IndexCount, DrawElementsType.UnsignedShort, 0);
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
                TerrainShader.Dispose();
                GridShader.Dispose();
                vertexIndexBuffer?.Dispose();

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
