using ct3d.RenderPrimitives;
using OpenToolkit.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ct3d
{
    sealed class GameState : IDisposable
    {
        public struct ProjectionWorldUniformBufferObjectValueType
        {
            public Matrix4 Projection;
            public Matrix4 World;
        }

        public Vector2 MousePosition, CameraPosition;
        public Vector2i WindowSize;
        public UniformBufferObject<ProjectionWorldUniformBufferObjectValueType> ProjectionWorldUniformBufferObject =
            new UniformBufferObject<ProjectionWorldUniformBufferObjectValueType>();
        private bool disposedValue;

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // managed state
                }

                // unmanaged state
                ProjectionWorldUniformBufferObject.Dispose();

                disposedValue = true;
            }
        }

        ~GameState()
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
