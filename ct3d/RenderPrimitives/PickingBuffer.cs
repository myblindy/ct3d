using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.RenderPrimitives
{
    sealed class PickingBuffer : IDisposable
    {
        readonly int frameBufferObject, pickingTextureObject, depthRenderBufferObject;
        readonly int[] pixelBufferObjects;
        int currentPixelBufferObject;
        bool disposedValue;

        public ShaderProgram Shader { get; }

        /// <summary>
        /// The position to test for the primitive ID. The result is stored in <see cref="SelectedPrimitiveID"/>.
        /// </summary>
        public Vector2i TestPosition { get; set; }

        /// <summary>
        /// The result of the test at <see cref="TestPosition"/>.
        /// </summary>
        public uint SelectedPrimitiveID { get; private set; }

        public PickingBuffer(string shaderName, int w, int h, int buffersCount = 2)
        {
            GL.CreateFramebuffers(1, out frameBufferObject);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out pickingTextureObject);
            GL.TextureStorage2D(pickingTextureObject, 1, SizedInternalFormat.R8ui, w, h);
            GL.NamedFramebufferTexture(frameBufferObject, FramebufferAttachment.ColorAttachment0, pickingTextureObject, 0);

            GL.CreateBuffers(buffersCount, pixelBufferObjects = new int[buffersCount]);
            for (int idx = 0; idx < buffersCount; ++idx)
                GL.NamedBufferData(pixelBufferObjects[idx], sizeof(uint), IntPtr.Zero, BufferUsageHint.DynamicRead);

            GL.CreateRenderbuffers(1, out depthRenderBufferObject);
            GL.NamedRenderbufferStorage(depthRenderBufferObject, RenderbufferStorage.DepthComponent, w, h);
            GL.NamedFramebufferRenderbuffer(depthRenderBufferObject, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRenderBufferObject);

            GL.ReadBuffer(ReadBufferMode.None);
            GL.NamedFramebufferDrawBuffer(frameBufferObject, DrawBufferMode.ColorAttachment0);

            var status = GL.CheckNamedFramebufferStatus(frameBufferObject, FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
                throw new InvalidOperationException($"Invalid framebuffer for {shaderName}: {status}");

            Shader = new ShaderProgram(shaderName);
        }

        public void Bind()
        {
            Shader.Use();
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, frameBufferObject);
        }

        public void UnbindAndProcess()
        {
            // done drawing, unmap the draw frame buffer
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            // map the frame buffer in read mode, and the current pixel buffer object in pack mode
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, frameBufferObject);
            GL.BindBuffer(BufferTarget.PixelPackBuffer, pixelBufferObjects[currentPixelBufferObject]);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

            // start the read operation for 1 "pixel" (ie 1 int)
            GL.ReadPixels(TestPosition.X, TestPosition.Y, 1, 1, PixelFormat.RedInteger, PixelType.Int, IntPtr.Zero);

            // unmap everything pixel packing and framebuffer related
            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
            GL.ReadBuffer(ReadBufferMode.None);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

            // increment the current pixel buffer and map it to system memory to read the selected ID
            currentPixelBufferObject = (currentPixelBufferObject + 1) % pixelBufferObjects.Length;
            SelectedPrimitiveID = (uint)Marshal.ReadInt32(GL.MapNamedBuffer(pixelBufferObjects[currentPixelBufferObject], BufferAccess.ReadOnly));
            GL.UnmapNamedBuffer(pixelBufferObjects[currentPixelBufferObject]);
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
                GL.DeleteTextures(2, new[] { pickingTextureObject, depthRenderBufferObject });
                GL.DeleteFramebuffer(frameBufferObject);
                GL.DeleteBuffers(pixelBufferObjects.Length, pixelBufferObjects);
                Shader.Dispose();

                disposedValue = true;
            }
        }

        ~PickingBuffer()
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
