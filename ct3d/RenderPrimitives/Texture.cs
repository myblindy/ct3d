using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct3d.RenderPrimitives
{
    sealed class Texture : IDisposable
    {
        readonly uint textureObject;
        private bool disposedValue;

        public Texture(string filename, TextureMinFilter minFilter = TextureMinFilter.Linear, TextureMagFilter magFilter = TextureMagFilter.Linear)
        {
            using var image = Image.Load<Rgba32>(Path.Combine("Media", filename));
            var bytes = image.GetPixelRowSpan(0);

            GL.CreateTextures(TextureTarget.Texture2D, 1, out textureObject);
            GL.TextureStorage2D(textureObject, 1, SizedInternalFormat.Rgba8, image.Width, image.Height);
            GL.TextureSubImage2D(textureObject, 0, 0, 0, image.Width, image.Height, PixelFormat.Rgba, PixelType.UnsignedByte, ref bytes[0]);

            int val = (int)minFilter;
            GL.TextureParameterI(textureObject, TextureParameterName.TextureMinFilter, ref val);
            val = (int)magFilter;
            GL.TextureParameterI(textureObject, TextureParameterName.TextureMagFilter, ref val);

            GL.GenerateTextureMipmap(textureObject);
        }

        public void Bind(int activeTextureIndex = 0)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + activeTextureIndex);
            GL.BindTexture(TextureTarget.Texture2D, textureObject);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed state
                }

                // unmanaged state
                GL.DeleteTexture(textureObject);

                disposedValue = true;
            }
        }

        ~Texture()
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
