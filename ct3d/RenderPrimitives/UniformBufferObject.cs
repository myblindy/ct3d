using OpenToolkit.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.RenderPrimitives
{
    class UniformBufferObject<TValue> : IDisposable where TValue : unmanaged
    {
        TValue value;
        private bool disposedValue;
        readonly int valueSize;
        readonly uint uniformBufferObject;

        public UniformBufferObject()
        {
            GL.CreateBuffers(1, out uniformBufferObject);
            GL.NamedBufferData(uniformBufferObject, valueSize = Marshal.SizeOf<TValue>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }

        public ref TValue Value => ref value;

        public void Upload() =>
            GL.NamedBufferSubData(uniformBufferObject, IntPtr.Zero, valueSize, ref value);

        public void Bind(int bindingIndex) =>
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingIndex, (int)uniformBufferObject);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed state
                }

                // unmanaged state
                GL.DeleteBuffer(uniformBufferObject);

                disposedValue = true;
            }
        }

        ~UniformBufferObject()
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
