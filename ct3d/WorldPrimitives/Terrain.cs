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
        readonly GameState gameState;

        public int Width { get; }
        public int Height { get; }

        public bool Dirty { get; set; } = true;

        private readonly ShaderProgram terrainShader = new ShaderProgram("terrain");
        private readonly ShaderProgram gridShader = new ShaderProgram("terrain.grid");
        private readonly PickingBuffer pickingBuffer;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vertex
        {
            public Vector3 Position, Normal;
            public Color4 Color;
        }
        VertexIndexBuffer<Vertex, ushort> vertexIndexBuffer;

        private bool disposedValue;

        static readonly Color4 grassColor = Color4.Lime;

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

                        static float transformHeight(byte heightValue) => heightValue / 3f;
                        float zXY = transformHeight(this[x, y]);
                        float zX1Y1 = transformHeight(this[x + 1, y + 1]);

                        // the 2 triangles that compose each quad of ground
                        Vector3 vx0y0 = new Vector3(x0, y0, zXY);
                        Vector3 vx01y0 = new Vector3(x0 + 1, y0, transformHeight(this[x + 1, y]));
                        Vector3 vx01y01 = new Vector3(x0 + 1, y0 + 1, zX1Y1);
                        Vector3 vx0y01 = new Vector3(x0, y0 + 1, transformHeight(this[x, y + 1]));

                        // transpose the seam if needed
                        if (vx0y0.Z == vx01y0.Z && vx0y0.Z == vx0y01.Z && vx0y0.Z != vx01y01.Z ||
                            vx01y01.Z == vx01y0.Z && vx01y01.Z == vx0y01.Z && vx0y0.Z != vx01y01.Z)
                        {
                            var normal = Vector3.Normalize(Vector3.Cross(vx0y0 - vx01y0, vx0y0 - vx0y01));
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx01y0, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx0y01, Color = grassColor, Normal = normal };

                            Vector3.Cross(vx01y0 - vx0y01, vx01y0 - vx01y01, out normal);
                            normal = -Vector3.Normalize(normal);
                            *vertex++ = new Vertex { Position = vx01y0, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx0y01, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx01y01, Color = grassColor, Normal = normal };

                            // use the index buffer to draw the grid lines with the vertices above
                            if (y0 > 0)
                            {
                                *index++ = (ushort)(vertex - firstVertex - 6);
                                *index++ = (ushort)(vertex - firstVertex - 4);
                            }

                            if (x0 > 0)
                            {
                                *index++ = (ushort)(vertex - firstVertex - 6);
                                *index++ = (ushort)(vertex - firstVertex - 5);
                            }
                        }
                        else
                        {
                            var normal = Vector3.Normalize(Vector3.Cross(vx01y0 - vx0y0, vx01y01 - vx0y0));
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx01y0, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx01y01, Color = grassColor, Normal = normal };

                            Vector3.Cross(vx01y01 - vx0y0, vx0y01 - vx0y0, out normal);
                            normal = Vector3.Normalize(normal);
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx01y01, Color = grassColor, Normal = normal };
                            *vertex++ = new Vertex { Position = vx0y01, Color = grassColor, Normal = normal };

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
            }

            return (vertices, indices);
        }

        public Terrain(int w, int h, GameState gameState)
        {
            (Width, Height, heightMap, this.gameState) = (w, h, new byte[w * h], gameState);
            pickingBuffer = new PickingBuffer("terrain.pick", gameState.WindowSize.X, gameState.WindowSize.Y, buffersCount: 2);

            terrainShader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
            gridShader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
            pickingBuffer.Shader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
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
                var (vertices, indices) = BuildTerrainVertices(0..4, 0..4);
                if (vertexIndexBuffer is null)
                    vertexIndexBuffer = new VertexIndexBuffer<Vertex, ushort>(vertices, indices);
                else
                    vertexIndexBuffer.Update(vertices, indices);

                Dirty = false;
            }

            vertexIndexBuffer.Bind();

            // pass 1, render to a picking texture
            pickingBuffer.TestPosition = new Vector2i((int)gameState.MousePosition.X, gameState.WindowSize.Y - (int)gameState.MousePosition.Y);
            pickingBuffer.Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexIndexBuffer.VertexCount);
            pickingBuffer.UnbindAndProcess();

            // pass 2, render the terrain 
            terrainShader.ProgramUniform("selectedPrimitiveID", pickingBuffer.SelectedPrimitiveID);
            terrainShader.Use();
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexIndexBuffer.VertexCount);

            // pass 3, render the grid
            gridShader.Use();
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
                terrainShader.Dispose();
                gridShader.Dispose();
                vertexIndexBuffer?.Dispose();
                pickingBuffer.Dispose();

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
