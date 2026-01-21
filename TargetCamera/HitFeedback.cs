using System;
using System.Collections.Generic;
using VRageMath;
using VRageRender;
using VRage.Utils;
using Sandbox.Graphics;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using VRage.Game;

namespace SETargetCamera
{
    /// <summary>
    /// Represents a single hit feedback indicator that shrinks over time
    /// </summary>
    public class HitFeedback
    {
        public Vector3D GridRelativePosition { get; set; }
        public MyCubeGrid TargetGrid { get; set; }
        public float MaxLifespan { get; set; }
        public float RemainingLifespan { get; set; }
        public Color Color { get; set; }
        public float MaxRadius { get; set; }

        public HitFeedback(Vector3D gridRelativePosition, MyCubeGrid targetGrid, float lifespan = 1.0f, Color? color = null, float radius = 2.0f)
        {
            GridRelativePosition = gridRelativePosition;
            TargetGrid = targetGrid;
            MaxLifespan = lifespan;
            RemainingLifespan = lifespan;
            Color = color ?? Color.Red;
            MaxRadius = radius;
        }

        /// <summary>
        /// Gets the world position of this hit feedback based on the grid's current position
        /// </summary>
        public Vector3D GetWorldPosition()
        {
            if (TargetGrid == null || TargetGrid.Closed)
                return GridRelativePosition;

            return Vector3D.Transform(GridRelativePosition, TargetGrid.WorldMatrix);
        }

        /// <summary>
        /// Updates the feedback, reducing remaining lifespan
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update in seconds</param>
        /// <returns>True if feedback is still alive, false if lifespan expired</returns>
        public bool Update(float deltaTime)
        {
            RemainingLifespan -= deltaTime;
            return RemainingLifespan > 0;
        }

        /// <summary>
        /// Gets the current radius based on remaining lifespan (shrinks over time)
        /// </summary>
        public float GetCurrentRadius()
        {
            return MaxRadius;
            if (MaxLifespan <= 0) return MaxRadius;
            float progress = RemainingLifespan / MaxLifespan;
            return MaxRadius * progress;
        }

        /// <summary>
        /// Gets the current alpha based on remaining lifespan (fades out)
        /// </summary>
        public float GetCurrentAlpha()
        {
            if (MaxLifespan <= 0) return 1.0f;
            return RemainingLifespan / MaxLifespan;
        }
    }

    /// <summary>
    /// Manager for all active hit feedback indicators
    /// </summary>
    public static class HitFeedbackManager
    {
        private static List<HitFeedback> _activeFeedback = new List<HitFeedback>();

        public static void AddHitFeedback(Vector3D gridRelativePosition, MyCubeGrid targetGrid, float lifespan = 1.0f, Color? color = null, float radius = 2.0f)
        {
            if (targetGrid == null || targetGrid.Closed)
                return;

            _activeFeedback.Add(new HitFeedback(gridRelativePosition, targetGrid, lifespan, color, radius));
        }

        public static void Update(float deltaTime)
        {
            for (int i = _activeFeedback.Count - 1; i >= 0; i--)
            {
                if (!_activeFeedback[i].Update(deltaTime))
                {
                    _activeFeedback.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Draws all hit feedback as circles in screen space on the target camera display
        /// </summary>
        public static void DrawAllScreenSpace(Vector2 displayPos, Vector2 displaySize, MatrixD cameraViewMatrix, Matrix projectionMatrix, Vector3D cameraPosition)
        {
            // Create a snapshot to avoid collection modified exceptions
            var feedbackSnapshot = new List<HitFeedback>(_activeFeedback);
            foreach (var feedback in feedbackSnapshot)
            {
                DrawHitFeedbackScreenSpace(feedback, displayPos, displaySize, cameraViewMatrix, projectionMatrix, cameraPosition);
            }
        }

        private static void DrawHitFeedbackScreenSpace(HitFeedback feedback, Vector2 displayPos, Vector2 displaySize, MatrixD cameraViewMatrix, Matrix projectionMatrix, Vector3D cameraPosition)
        {
            Vector3D worldPosition = feedback.GetWorldPosition();

            // Transform world position to screen space
            Vector2 screenSpacePoint = ScreenSpaceDrawing.WorldToScreenSpace(worldPosition, displayPos, displaySize, cameraViewMatrix, projectionMatrix, out bool isValid);

            // Check if point is valid and within display bounds
            if (!isValid)
                return;

            // Draw circle
            float radius = feedback.GetCurrentRadius() * 5; // Scale for screen space visibility
            Color color = feedback.Color;
            color.A = (byte)(feedback.GetCurrentAlpha() * 255);

            ScreenSpaceDrawing.Circle(screenSpacePoint, radius, color);
        }

        public static void Clear()
        {
            _activeFeedback.Clear();
        }

        public static int GetActiveCount()
        {
            return _activeFeedback.Count;
        }
    }
}
