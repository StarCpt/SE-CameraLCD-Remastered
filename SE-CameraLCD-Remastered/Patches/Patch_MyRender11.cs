using HarmonyLib;
using VRageRender;

namespace CameraLCD.Patches
{
    [HarmonyPatch]
    public static class Patch_MyRender11
    {
        [HarmonyPatch(typeof(MyRender11), nameof(MyRender11.DrawGameScene))]
        [HarmonyPostfix]
        public static void MyRender11_DrawGameScene_Postfix()
        {
            if (!Plugin.Settings.Enabled)
                return;

            // don't draw cameralcd if a screenshot is being taken
            if (MyRender11.m_screenshot.HasValue)
                return;

            CameraLcdManager.Draw();
        }
    }
}
