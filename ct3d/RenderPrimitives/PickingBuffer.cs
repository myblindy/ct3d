using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.RenderPrimitives
{
    sealed class PickingBuffer : IDisposable
    {
        readonly int frameBufferObject, pickingTextureObject, depthRenderBufferObject;
        public ShaderProgram Shader { get; }
        private bool disposedValue;

        public Vector2i TestPosition { get; set; }
        public uint SelectedPrimitiveID;

        public PickingBuffer(string shaderName, int w, int h)
        {
            GL.CreateFramebuffers(1, out frameBufferObject);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out pickingTextureObject);
            GL.TextureStorage2D(pickingTextureObject, 1, SizedInternalFormat.R8ui, w, h);
            GL.NamedFramebufferTexture(frameBufferObject, FramebufferAttachment.ColorAttachment0, pickingTextureObject, 0);

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
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, frameBufferObject);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);

            GL.ReadPixels(TestPosition.X, TestPosition.Y, 1, 1, PixelFormat.RedInteger, PixelType.Int, ref SelectedPrimitiveID);

            GL.ReadBuffer(ReadBufferMode.None);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
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
