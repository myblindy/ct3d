using ct3d.RenderPrimitives;
using ct3d.Support;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
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
        readonly TerrainData[] heightMap;
        readonly GameState gameState;

        public int Width { get; }
        public int Height { get; }

        public bool Dirty { get; set; } = true;

        /// <summary>
        /// Returns the selected cell, if any, including the decimal portion. If no cell is selected, returns <see cref="NoSelectedCell"/>.
        /// </summary>
        public Vector2 SelectedCell { get; private set; }
        public static readonly Vector2 NoSelectedCell = new(-1, -1);

        readonly int chunkSize;

        readonly Texture roadsTexture = new("roads.png");

        readonly ShaderProgram terrainShader = new("terrain.main");
        readonly ShaderProgram gridShader = new("terrain.grid");
        readonly PickingBuffer pickingBuffer;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vertex
        {
            public Vector3 Position, Normal;
            public Color4 Color;
            public Vector2 UV;
            public int RoadsIndex;
        }
        VertexIndexBuffer<Vertex, ushort> vertexIndexBuffer;

        bool disposedValue;

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
                        ref var terrainX0Y0 = ref this[x, y];
                        var zXY = transformHeight(terrainX0Y0.Height);
                        ref var terrainX1Y1 = ref this[x + 1, y + 1];
                        var zX1Y1 = transformHeight(terrainX1Y1.Height);

                        // the 2 triangles that compose each quad of ground
                        var vx0y0 = new Vector3(x0, y0, zXY);
                        ref var terrainX1Y0 = ref this[x + 1, y];
                        var vx1y0 = new Vector3(x0 + 1, y0, transformHeight(terrainX1Y0.Height));
                        var vx1y1 = new Vector3(x0 + 1, y0 + 1, zX1Y1);
                        ref var terrainX0Y1 = ref this[x, y + 1];
                        var vx0y1 = new Vector3(x0, y0 + 1, transformHeight(terrainX0Y1.Height));

                        // transpose the seam if needed
                        if (vx0y0.Z == vx1y0.Z && vx0y0.Z == vx0y1.Z && vx0y0.Z != vx1y1.Z ||
                            vx1y1.Z == vx1y0.Z && vx1y1.Z == vx0y1.Z && vx0y0.Z != vx1y1.Z)
                        {
                            var normal = Vector3.Normalize(Vector3.Cross(vx0y0 - vx1y0, vx0y0 - vx0y1));
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(0, 0) };
                            *vertex++ = new Vertex { Position = vx1y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(1, 0) };
                            *vertex++ = new Vertex { Position = vx0y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(0, 1) };

                            Vector3.Cross(vx1y0 - vx0y1, vx1y0 - vx1y1, out normal);
                            normal = -Vector3.Normalize(normal);
                            *vertex++ = new Vertex { Position = vx1y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(1, 0) };
                            *vertex++ = new Vertex { Position = vx0y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(0, 1) };
                            *vertex++ = new Vertex { Position = vx1y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(1, 1) };

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
                            var normal = Vector3.Normalize(Vector3.Cross(vx1y0 - vx0y0, vx1y1 - vx0y0));
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(0, 0) };
                            *vertex++ = new Vertex { Position = vx1y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(1, 0) };
                            *vertex++ = new Vertex { Position = vx1y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(1, 1) };

                            Vector3.Cross(vx1y1 - vx0y0, vx0y1 - vx0y0, out normal);
                            normal = Vector3.Normalize(normal);
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(0, 0) };
                            *vertex++ = new Vertex { Position = vx1y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(1, 1) };
                            *vertex++ = new Vertex { Position = vx0y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(0, 1) };

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

        public Terrain(int w, int h, int chunkSize, GameState gameState)
        {
            (Width, Height, heightMap, this.gameState, this.chunkSize) = (w, h, new TerrainData[w * h], gameState, chunkSize);
            pickingBuffer = new PickingBuffer("terrain.pick", gameState.WindowSize.X, gameState.WindowSize.Y);

            terrainShader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
            gridShader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
            pickingBuffer.Shader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
        }

        public void SetHeight(int x, int y, byte height) { heightMap[x * Width + y].Height = height; Dirty = true; }
        public void SetRoad(int x, int y, TerrainRoadData roadData) { Dirty |= this[x, y].RoadData != roadData; heightMap[x * Width + y].RoadData = roadData; }
        public void AddRoad(int x, int y, TerrainRoadData roadData) => SetRoad(x, y, this[x, y].RoadData | roadData);
        public void RemoveRoad(int x, int y, TerrainRoadData roadData) => SetRoad(x, y, this[x, y].RoadData & ~roadData);
        public ref TerrainData this[int x, int y] => ref heightMap[x * Width + y];

        public static TerrainRoadData TerrainRoadDataFromUV(Vector2 uv)
        {
            uv -= new Vector2(.5f, .5f);
            var absUv = new Vector2(Math.Abs(uv.X), Math.Abs(uv.Y));

            // dead zone
            if (absUv.X < .2f && absUv.Y < .2f) return TerrainRoadData.None;

            return uv.X <= 0
                ? uv.Y.Between(-absUv.X, absUv.X) ? TerrainRoadData.Left : uv.Y < absUv.X ? TerrainRoadData.Down : TerrainRoadData.Up
                : uv.Y.Between(-absUv.X, absUv.X) ? TerrainRoadData.Right : uv.Y < absUv.X ? TerrainRoadData.Down : TerrainRoadData.Up;
        }

        public void Render()
        {
            if (Dirty)
            {
                var (vertices, indices) = BuildTerrainVertices(0..chunkSize, 0..chunkSize);
                if (vertexIndexBuffer is null)
                    vertexIndexBuffer = new(vertices, indices);
                else
                    vertexIndexBuffer.Update(vertices, indices);

                Dirty = false;
            }

            vertexIndexBuffer.Bind();

            // pass 1, render to a picking texture
            pickingBuffer.TestPosition = new((int)gameState.MousePosition.X, gameState.WindowSize.Y - (int)gameState.MousePosition.Y);
            pickingBuffer.Bind();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexIndexBuffer.VertexCount);
            pickingBuffer.UnbindAndProcess();

            // pass 2, render the terrain 
            terrainShader.ProgramUniform("selectedPrimitiveID", pickingBuffer.SelectedPrimitiveID);
            terrainShader.Use();
            roadsTexture.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexIndexBuffer.VertexCount);

            // pass 3, render the grid
            gridShader.Use();
            GL.DrawElements(BeginMode.Lines, vertexIndexBuffer.IndexCount, DrawElementsType.UnsignedShort, 0);

            // return the picked cell information
            SelectedCell = pickingBuffer.SelectedPrimitiveID == 0 ? NoSelectedCell
                : new Vector2((pickingBuffer.SelectedPrimitiveID - 1) / 2 / (chunkSize - 1), (pickingBuffer.SelectedPrimitiveID - 1) / 2 % (chunkSize - 1)) + pickingBuffer.SelectedPrimitiveUV;
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
                roadsTexture.Dispose();

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

    [Flags]
    enum TerrainRoadData { None = 0, Up = 1 << 0, Right = 1 << 1, Down = 1 << 2, Left = 1 << 3 }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TerrainData
    {
        public byte Height;
        public TerrainRoadData RoadData { get => (TerrainRoadData)(info & 0xF); set => info = (byte)(info & 0xF0 | (byte)value); }

        byte info;
    }

}
