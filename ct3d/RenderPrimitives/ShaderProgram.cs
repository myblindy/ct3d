using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenToolkit.Graphics.OpenGL4;

namespace ct3d.RenderPrimitives
{
    sealed class ShaderProgram : IDisposable
    {
        const string PathPrefix = "Shaders";
        readonly int programObject;
        private bool disposedValue;

        public ShaderProgram(string commonShaderPath) : this(commonShaderPath + ".vert", commonShaderPath + ".frag") { }

        public ShaderProgram(string vertexShaderPath, string fragmentShaderPath)
        {
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, File.ReadAllText(Path.Combine(PathPrefix, vertexShaderPath)));
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int compileStatus);
            if (compileStatus == 0)
                throw new InvalidOperationException($"Vertex shader {vertexShaderPath} could not be compiled:\n\n{GL.GetShaderInfoLog(vertexShader)}");

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, File.ReadAllText(Path.Combine(PathPrefix, fragmentShaderPath)));
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out compileStatus);
            if (compileStatus == 0)
                throw new InvalidOperationException($"Fragment shader {fragmentShaderPath} could not be compiled:\n\n{GL.GetShaderInfoLog(fragmentShader)}");

            programObject = GL.CreateProgram();
            GL.AttachShader(programObject, vertexShader);
            GL.AttachShader(programObject, fragmentShader);
            GL.LinkProgram(programObject);
            GL.GetProgram(programObject, GetProgramParameterName.LinkStatus, out var linkStatus);
            if (linkStatus == 0)
                throw new InvalidOperationException($"Program could not be linked with vertex shader {vertexShaderPath} and fragment shader {fragmentShaderPath}:\n\n{GL.GetProgramInfoLog(programObject)}");

            GL.DetachShader(programObject, vertexShader);
            GL.DetachShader(programObject, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        public void Use() => GL.UseProgram(programObject);

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed state
                }

                // unmanaged state
                GL.DeleteProgram(programObject);

                disposedValue = true;
            }
        }

        ~ShaderProgram()
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
