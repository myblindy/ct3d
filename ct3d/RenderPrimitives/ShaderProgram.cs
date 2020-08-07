using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Mathematics;

namespace ct3d.RenderPrimitives
{
    sealed class ShaderProgram : IDisposable
    {
        const string PathPrefix = "Shaders";
        readonly int programObject;
        readonly Dictionary<string, int> uniforms = new Dictionary<string, int>();
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

            // get uniform names
            GL.GetProgram(programObject, GetProgramParameterName.ActiveUniforms, out int uniformCount);
            for (int idx = 0; idx < uniformCount; ++idx)
            {
                GL.GetActiveUniformName(programObject, idx, 60, out _, out var name);
                uniforms[name] = GL.GetUniformLocation(programObject, name);
            }
        }

        internal void ProgramUniform(string name, ref Matrix4 mat) =>
            GL.ProgramUniformMatrix4(programObject, uniforms[name], false, ref mat);

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
