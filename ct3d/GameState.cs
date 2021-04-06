using ct3d.RenderPrimitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ct3d
{
    sealed class GameState : IDisposable
    {
        public struct ProjectionWorldUniformBufferObjectValueType
        {
            public Matrix4x4 Projection;
            public Matrix4x4 View;
        }

        public Vector2 MousePosition, CameraPosition;
        public Vector2 WindowSize;
        public UniformBufferObject<ProjectionWorldUniformBufferObjectValueType> ProjectionWorldUniformBufferObject = new();

        public Vector3 CameraOrigin => new(CameraPosition.X + 5, CameraPosition.Y, 8);
        public Vector3 CameraTarget => new(CameraPosition.X + 5, CameraPosition.Y + 5, 0);
        public Vector2 CameraFrustrumSize { get; set; }

        public static readonly Vector3 CameraUp = new(0, 0, 1);

        public Matrix4x4 GetCameraTransform() => Matrix4x4.CreateLookAt(CameraOrigin, CameraTarget, CameraUp);

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
