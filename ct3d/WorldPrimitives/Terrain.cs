using ct3d.RenderPrimitives;
using ct3d.Support;
using MoreLinq;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.WorldPrimitives
{
    sealed class Terrain : IDisposable
    {
        readonly TerrainData[] heightMap;
        readonly GameState gameState;
        readonly TerrainGraph terrainGraph;

        public int Width { get; }
        public int Height { get; }

        public bool HeightMapDirty { get; set; } = true;

        /// <summary>
        /// Returns the selected cell, if any, including the decimal portion. If no cell is selected, returns <see cref="NoSelectedCell"/>.
        /// </summary>
        public Vector2 SelectedCell { get; private set; }
        public static readonly Vector2 NoSelectedCell = new(-1, -1);

        uint selectedPrimitiveId;

        readonly int chunkSize;

        readonly Texture roadsTexture = new("roads.png");

        readonly ShaderProgram terrainShader = new("terrain.main");
        readonly ShaderProgram gridShader = new("terrain.grid");
        //readonly PickingBuffer pickingBuffer;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vertex
        {
            public Vector3 Position, Normal;
            public Vector4 Color;
            public Vector2 UV;
            public int RoadsIndex;
        }

        struct Triangle
        {
            public Vector3 A, B, C;

            public Triangle(Vector3 a, Vector3 b, Vector3 c) => (A, B, C) = (a, b, c);
        }

        struct TerrainChunk
        {
            public VertexIndexBuffer<Vertex, ushort> VertexIndexBuffer;
            public Triangle[] Triangles;
            public Vector2 Offset;
            public Vector2 Size;
        }

        readonly TerrainChunk[] terrainChunks = new TerrainChunk[1];

        bool disposedValue;

        static readonly Vector4 grassColor = new(0, 1, 0, 0);

        unsafe (Vertex[], ushort[]) BuildTerrainVertices(Range xRange, Range yRange)
        {
            var vertices = new Vertex[(xRange.End.Value - xRange.Start.Value - 1) * (yRange.End.Value - yRange.Start.Value - 1) * 6];
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

                        const float uvMin = 0.05f, uvMax = 0.95f;

                        // transpose the seam if needed
                        if (vx0y0.Z == vx1y0.Z && vx0y0.Z == vx0y1.Z && vx0y0.Z != vx1y1.Z ||
                            vx1y1.Z == vx1y0.Z && vx1y1.Z == vx0y1.Z && vx0y0.Z != vx1y1.Z)
                        {
                            var normal = Vector3.Normalize(Vector3.Cross(vx0y0 - vx1y0, vx0y0 - vx0y1));
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMin, uvMin) };
                            *vertex++ = new Vertex { Position = vx1y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMax, uvMin) };
                            *vertex++ = new Vertex { Position = vx0y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMin, uvMax) };

                            normal = -Vector3.Normalize(Vector3.Cross(vx1y0 - vx0y1, vx1y0 - vx1y1));
                            *vertex++ = new Vertex { Position = vx1y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMax, uvMin) };
                            *vertex++ = new Vertex { Position = vx0y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMin, uvMax) };
                            *vertex++ = new Vertex { Position = vx1y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMax, uvMax) };

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
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMin, uvMin) };
                            *vertex++ = new Vertex { Position = vx1y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMax, uvMin) };
                            *vertex++ = new Vertex { Position = vx1y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMax, uvMax) };

                            normal = Vector3.Normalize(Vector3.Cross(vx1y1 - vx0y0, vx0y1 - vx0y0));
                            *vertex++ = new Vertex { Position = vx0y0, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMin, uvMin) };
                            *vertex++ = new Vertex { Position = vx1y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMax, uvMax) };
                            *vertex++ = new Vertex { Position = vx0y1, Color = grassColor, Normal = normal, RoadsIndex = (int)terrainX0Y0.RoadData, UV = new(uvMin, uvMax) };

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
            (Width, Height, heightMap, this.gameState, this.chunkSize, terrainGraph) = (w, h, new TerrainData[w * h], gameState, chunkSize, new(this));

            terrainShader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
            gridShader.BindUniformBlock("ViewMatrices", 0, gameState.ProjectionWorldUniformBufferObject);
        }

        public void SetHeight(int x, int y, byte height) { heightMap[x * Width + y].Height = height; HeightMapDirty = true; }
        public void SetRoad(int x, int y, TerrainRoadData roadData) { HeightMapDirty |= this[x, y].RoadData != roadData; terrainGraph.UpdateGraph(x, y, roadData); heightMap[x * Width + y].RoadData = roadData; }
        public void AddRoad(int x, int y, TerrainRoadData roadData) => SetRoad(x, y, this[x, y].RoadData | roadData);
        public void RemoveRoad(int x, int y, TerrainRoadData roadData) => SetRoad(x, y, this[x, y].RoadData & ~roadData);
        public ref TerrainData this[int x, int y] { get { if (x < 0 || y < 0 || x >= Width || y >= Height) return ref TerrainData.OutOfBounds; return ref heightMap[x * Width + y]; } }

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

        Vector2 LastMousePosition = new(-1, -1);

        static Vector4 Extend(Vector2 v, float z, float w) => new(v.X, v.Y, z, w);
        static Vector3 Extend(Vector2 v, float z) => new(v.X, v.Y, z);

        public void Update()
        {
            for (int chunkIdx = 0; chunkIdx < terrainChunks.Length; ++chunkIdx)
                if (HeightMapDirty)
                {
                    ref var chunk = ref terrainChunks[chunkIdx];
                    var (vertices, indices) = BuildTerrainVertices(0..chunkSize, 0..chunkSize);
                    if (chunk.VertexIndexBuffer is null)
                        chunk.VertexIndexBuffer = new(vertices, indices);
                    else
                        chunk.VertexIndexBuffer.Update(vertices, indices);

                    if (chunk.Triangles?.Length != vertices.Length / 3)
                        chunk.Triangles = new Triangle[vertices.Length / 3];
                    for (int tri = 0; tri < chunk.Triangles.Length; ++tri)
                        chunk.Triangles[tri] = new(vertices[tri * 3].Position, vertices[tri * 3 + 1].Position, vertices[tri * 3 + 2].Position);

                    chunk.Offset = new(0, 0);
                    chunk.Size = new(chunkSize, chunkSize);

                    HeightMapDirty = false;
                }

            // terrain cell pick
            if (LastMousePosition != gameState.MousePosition)
            {
                LastMousePosition = gameState.MousePosition;

                var found = false;

                var cameraOrigin = gameState.CameraOrigin;

                var normalizedScreenMousePosition = new Vector4(gameState.MousePosition.X / gameState.WindowSize.X * 2 - 1, -(gameState.MousePosition.Y / gameState.WindowSize.Y * 2 - 1), 1f, 1f);
                if (!Matrix4x4.Invert(gameState.ProjectionWorldUniformBufferObject.Value.View * gameState.ProjectionWorldUniformBufferObject.Value.Projection, out var invertedProjectionViewTransform))
                    throw new InvalidOperationException();
                var screenMousePosition = Vector4.Transform(normalizedScreenMousePosition, Matrix4x4.Transpose(invertedProjectionViewTransform));
                var cameraDirection = Vector3.Normalize(new Vector3(screenMousePosition.X, screenMousePosition.Y, screenMousePosition.Z));
                var cameraTarget = cameraOrigin + cameraDirection;

                for (int chunkIdx = 0; chunkIdx < terrainChunks.Length; ++chunkIdx)
                {
                    ref var chunk = ref terrainChunks[chunkIdx];
                    for (uint triIdx = 0; triIdx < chunk.Triangles.Length; ++triIdx)
                    {
                        ref var tri = ref chunk.Triangles[triIdx];

                        var edge1 = tri.B - tri.A;
                        var edge2 = tri.C - tri.A;

                        var pVec = Vector3.Cross(cameraDirection, edge2);
                        var det = Vector3.Dot(edge1, pVec);
                        if (Math.Abs(det) < 1e-8) throw new InvalidOperationException();        // degenerate triangle?
                        var invDet = 1f / det;

                        var tVec = cameraOrigin - tri.A;
                        var u = Vector3.Dot(tVec, pVec) * invDet;
                        if (u < 0 || u > 1) continue;           // outside

                        var qVec = Vector3.Cross(tVec, edge1);
                        var v = Vector3.Dot(cameraDirection, qVec) * invDet;
                        if (v < 0 || u + v > 1) continue;       // outside

                        // inside at position u,v
                        (SelectedCell, found, selectedPrimitiveId) = (new(tri.A.X + u, tri.A.X + v), true, triIdx & ~1U);
                        break;
                    }
                }

                if (!found)
                    SelectedCell = NoSelectedCell;
            }
        }

        public void Render()
        {
            ref var chunk = ref terrainChunks[0];

            if (chunk.VertexIndexBuffer is null) return;

            chunk.VertexIndexBuffer.Bind();

            // pass 1, render the terrain 
            terrainShader.ProgramUniform("selectedPrimitiveID", selectedPrimitiveId);
            terrainShader.Use();
            roadsTexture.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, chunk.VertexIndexBuffer.VertexCount);

            // pass 3, render the grid
            gridShader.Use();
            GL.DrawElements(BeginMode.Lines, chunk.VertexIndexBuffer.IndexCount, DrawElementsType.UnsignedShort, 0);

            // return the picked cell information
            //SelectedCell = pickingBuffer.SelectedPrimitiveID == 0 ? NoSelectedCell
            //    : new Vector2((pickingBuffer.SelectedPrimitiveID - 1) / 2 / (chunkSize - 1), (pickingBuffer.SelectedPrimitiveID - 1) / 2 % (chunkSize - 1)) + pickingBuffer.SelectedPrimitiveUV;
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
                for (int idx = 0; idx < terrainChunks.Length; ++idx)
                    terrainChunks[idx].VertexIndexBuffer.Dispose();
                //pickingBuffer.Dispose();
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
        public static TerrainData OutOfBounds = new();

        public byte Height;
        public TerrainRoadData RoadData { get => (TerrainRoadData)(info & 0xF); set => info = (byte)(info & 0xF0 | (byte)value); }

        byte info;
    }

}
