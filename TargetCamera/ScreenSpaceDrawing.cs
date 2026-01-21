using System;
using VRageMath;
using VRageRender;

namespace SETargetCamera
{
    /// <summary>
    /// Utility class for drawing shapes in screen space
    /// </summary>
    public static class ScreenSpaceDrawing
    {
        /// <summary>
        /// Transforms a world position to screen space coordinates
        /// </summary>
        /// <param name="worldPosition">The position in world space</param>
        /// <param name="displayPos">The top-left corner of the display in screen space</param>
        /// <param name="displaySize">The width and height of the display in screen space</param>
        /// <param name="cameraViewMatrix">The camera's view matrix</param>
        /// <param name="projectionMatrix">The camera's projection matrix</param>
        /// <param name="isValid">Output parameter indicating if the point is valid and within bounds</param>
        /// <returns>A Vector2 with the transformed screen space coordinates</returns>
        public static Vector2 WorldToScreenSpace(Vector3D worldPosition, Vector2 displayPos, Vector2 displaySize, MatrixD cameraViewMatrix, Matrix projectionMatrix, out bool isValid)
        {
            // Transform world position to view space
            Vector3D viewSpacePos = Vector3D.Transform(worldPosition, cameraViewMatrix);
            Vector4 projectedPos = Vector4.Transform(new Vector4(viewSpacePos, 1), projectionMatrix);

            // Check if point is behind camera
            if (projectedPos.W <= 0)
            {
                isValid = false;
                return Vector2.Zero;
            }

            // Perspective divide to get normalized device coordinates (NDC)
            Vector3 ndcPos = new Vector3(projectedPos.X / projectedPos.W, projectedPos.Y / projectedPos.W, projectedPos.Z / projectedPos.W);

            // Convert from NDC [-1, 1] to screen space
            float screenX = displayPos.X + (ndcPos.X + 1) * 0.5f * displaySize.X;
            float screenY = displayPos.Y + (1 - ndcPos.Y) * 0.5f * displaySize.Y; // Flip Y

            // Check if point is within display bounds
            isValid = screenX >= displayPos.X && screenX <= displayPos.X + displaySize.X &&
                      screenY >= displayPos.Y && screenY <= displayPos.Y + displaySize.Y;

            return new Vector2(screenX, screenY);
        }

        /// <summary>
        /// Draws a circle in screen space using line segments
        /// </summary>
        /// <param name="center">Center position of the circle in screen space</param>
        /// <param name="radius">Radius of the circle in screen space units</param>
        /// <param name="color">Color of the circle</param>
        /// <param name="segments">Number of line segments to approximate the circle (default: 16)</param>
        public static void Circle(Vector2 center, float radius, Color color, int segments = 16)
        {
            float angleStep = (float)(2 * Math.PI / segments);

            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;

                float x1 = center.X + radius * (float)Math.Cos(angle1);
                float y1 = center.Y + radius * (float)Math.Sin(angle1);
                float x2 = center.X + radius * (float)Math.Cos(angle2);
                float y2 = center.Y + radius * (float)Math.Sin(angle2);

                // Draw line segment
                MyRenderProxy.DebugDrawLine2D(new Vector2(x1, y1), new Vector2(x2, y2), color, color);
            }
        }
    }
}
