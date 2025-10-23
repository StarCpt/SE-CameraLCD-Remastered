using HarmonyLib;
using VRageRender;

namespace CameraLCD.Patches
{
    [HarmonyPatch]
    public static class Patch_MyRender11
    {
        private static bool _drawingCameraLcds = false;

        [HarmonyPatch(typeof(MyRender11), nameof(MyRender11.DrawGameScene))]
        [HarmonyPostfix]
        public static void MyRender11_DrawGameScene_Postfix()
        {
            if (!Plugin.Settings.Enabled || _drawingCameraLcds)
                return;

            // don't draw cameralcd if a screenshot is being taken
            if (MyRender11.m_screenshot.HasValue)
                return;

            _drawingCameraLcds = true;
            CameraLcdManager.Draw();
            _drawingCameraLcds = false;
        }
    }
}
