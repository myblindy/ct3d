﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

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

        static readonly Dictionary<Type, VertexTypeCacheType> vertexTypeCache = new();
        static readonly Dictionary<Type, VertexAttribType> vertexAttribTypesCache = new()
        {
            [typeof(float)] = VertexAttribType.Float,
            [typeof(Vector2)] = VertexAttribType.Float,
            [typeof(Vector3)] = VertexAttribType.Float,
            [typeof(Vector4)] = VertexAttribType.Float,
            [typeof(int)] = VertexAttribType.Int,
            [typeof(uint)] = VertexAttribType.UnsignedInt,
        };
        static readonly Dictionary<Type, int> componentCountCache = new()
        {
            [typeof(float)] = 1,
            [typeof(Vector2)] = 2,
            [typeof(Vector3)] = 3,
            [typeof(Vector4)] = 4,
            [typeof(int)] = 1,
            [typeof(uint)] = 1,
        };

        static VertexTypeCacheType VertexTypeData
        {
            get
            {
                Type vertexType = typeof(TVertex);
                if (vertexTypeCache.TryGetValue(vertexType, out var data))
                    return data;

                // decode the type
                data.Size = Marshal.SizeOf<TVertex>();
                data.PerFieldData = vertexType.GetFields()
                    .Select(fi => (vertexAttribTypesCache[fi.FieldType], (int)Marshal.OffsetOf<TVertex>(fi.Name), componentCountCache[fi.FieldType]))
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
                GL.VertexArrayAttribBinding(vertexArrayObject, idx, 0);
            }
        }

        public unsafe void Update(TVertex[] vertices, TIndex[] indices)
        {
            if (vertices.Length != VertexCount || indices.Length != IndexCount) throw new InvalidOperationException();

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
