using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;

namespace ct3d.RenderPrimitives
{
    sealed class VertexIndexBuffer<TVertex, TIndex> : IDisposable
        where TVertex : unmanaged
        where TIndex : unmanaged
    {
        struct VertexTypeCacheType
        {
            public (VertexAttribType vertexAttribType, int offset, int size)[] PerFieldData;
            public int Size;
        }

        static readonly Dictionary<Type, VertexTypeCacheType> vertexTypeCache = new Dictionary<Type, VertexTypeCacheType>();

        static VertexTypeCacheType VertexTypeData
        {
            get
            {
                Type vertexType = typeof(TVertex);
                if (vertexTypeCache.TryGetValue(vertexType, out var data))
                    return data;

                // decode the type
                data.Size = Marshal.SizeOf<TVertex>();
                data.PerFieldData = vertexType.GetFields().Select(fi =>
                     (fi.FieldType == typeof(float) || fi.FieldType == typeof(Vector2) || fi.FieldType == typeof(Vector3) || fi.FieldType == typeof(Vector4) || fi.FieldType == typeof(Color4)
                         ? VertexAttribType.Float
                         : throw new NotImplementedException(), (int)Marshal.OffsetOf<TVertex>(fi.Name),
                      fi.FieldType == typeof(float) ? 1
                        : fi.FieldType == typeof(Vector2) ? 2
                        : fi.FieldType == typeof(Vector3) ? 3
                        : fi.FieldType == typeof(Vector4) || fi.FieldType == typeof(Color4) ? 4
                        : throw new NotImplementedException()))
                    .ToArray();

                return data;
            }
        }

        VertexTypeCacheType vertexTypeData;

        readonly int[] bufferObjects = new int[2];
        readonly int vertexArrayObject;
        private bool disposedValue;

        public int VertexCount { get; }
        public int IndexCount { get; }

        public unsafe VertexIndexBuffer(TVertex[] vertices, TIndex[] indices)
        {
            vertexTypeData = VertexTypeData;
            GL.CreateBuffers(2, bufferObjects);
            GL.NamedBufferStorage(bufferObjects[0], vertexTypeData.Size * (VertexCount = vertices.Length), vertices, BufferStorageFlags.DynamicStorageBit);
            GL.NamedBufferStorage(bufferObjects[1], sizeof(TIndex) * (IndexCount = indices.Length), indices, BufferStorageFlags.DynamicStorageBit);

            GL.CreateVertexArrays(1, out vertexArrayObject);
            GL.VertexArrayVertexBuffer(vertexArrayObject, 0, bufferObjects[0], IntPtr.Zero, vertexTypeData.Size);
            GL.VertexArrayElementBuffer(vertexArrayObject, bufferObjects[1]);

            for (int idx = 0; idx < vertexTypeData.PerFieldData.Length; ++idx)
            {
                GL.EnableVertexArrayAttrib(vertexArrayObject, idx);
                GL.VertexArrayAttribFormat(vertexArrayObject, idx, vertexTypeData.PerFieldData[idx].size, VertexAttribType.Float, false, vertexTypeData.PerFieldData[idx].offset);
                GL.VertexArrayAttribBinding(vertexArrayObject, 2, 0);
            }
        }

        public unsafe void Update(TVertex[] vertices, TIndex[] indices)
        {
            GL.NamedBufferSubData(bufferObjects[0], IntPtr.Zero, vertexTypeData.Size * vertices.Length, vertices);
            GL.NamedBufferSubData(bufferObjects[1], IntPtr.Zero, sizeof(TIndex) * indices.Length, indices);
        }

        public void Bind() => GL.BindVertexArray(vertexArrayObject);

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed
                }

                // unmanaged
                GL.DeleteVertexArray(vertexArrayObject);
                GL.DeleteBuffers(2, bufferObjects);

                disposedValue = true;
            }
        }

        ~VertexIndexBuffer()
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
