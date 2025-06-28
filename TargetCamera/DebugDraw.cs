using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;
using VRage.Utils;

namespace SETargetCamera
{
    public static class DebugDraw
    {
        private class DebugLine
        {
            public Vector3D From;
            public Vector3D To;
            public Color Color;
            public float Thickness;
            public DateTime Expiry;
        }

        private static readonly List<DebugLine> _lines = new List<DebugLine>();

        /// <summary>
        /// Draw a line for a given duration in seconds.
        /// </summary>
        public static void DrawLine(Vector3D from, Vector3D to, Color color, float thickness = 0.05f, double duration = 0.0)
        {
            var line = new DebugLine
            {
                From = from,
                To = to,
                Color = color,
                Thickness = thickness,
                Expiry = DateTime.UtcNow.AddSeconds(duration)
            };

            _lines.Add(line);
        }

        /// <summary>
        /// Call this from your SessionComponent's Draw() every frame.
        /// </summary>
        public static void Draw()
        {
            DateTime now = DateTime.UtcNow;
            for (int i = _lines.Count - 1; i >= 0; i--)
            {
                var line = _lines[i];
                if (now > line.Expiry)
                {
                    _lines.RemoveAt(i);
                    continue;
                }

                var color2 = (Vector4)line.Color;
                
                MySimpleObjectDraw.DrawLine(line.From, line.To, MyStringId.GetOrCompute("Debug"), ref color2, line.Thickness);
                MyLog.Default.WriteLine("Drawing line :)");
            }
        }
    }
}