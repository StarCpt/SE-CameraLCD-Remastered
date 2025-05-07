using HarmonyLib;
using Sandbox.Game.Entities;

namespace SETargetCamera
{
    public static class MyCameraBlockAccessor
    {
        private static readonly AccessTools.FieldRef<MyCameraBlock, float> _m_fov = typeof(MyCameraBlock).FieldRefAccess<float>("m_fov");

        public static float GetFov(this MyCameraBlock camera) => _m_fov.Invoke(camera);
        public static void SetFov(this MyCameraBlock camera, float fovInRadians) => _m_fov.Invoke(camera) = fovInRadians;
    }
}
