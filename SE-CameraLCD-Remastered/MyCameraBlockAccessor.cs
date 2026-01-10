using HarmonyLib;
using Sandbox.Game.Entities;

namespace CameraLCD
{
    public static class MyCameraBlockAccessor
    {
        public static float GetFov(this MyCameraBlock camera) => camera.m_fov;
        public static void SetFov(this MyCameraBlock camera, float fovInRadians) => camera.m_fov = fovInRadians;
    }
}
