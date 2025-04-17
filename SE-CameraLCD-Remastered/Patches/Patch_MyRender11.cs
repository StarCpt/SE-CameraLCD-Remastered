using HarmonyLib;

namespace CameraLCD.Patches
{
    [HarmonyPatch]
    public static class Patch_MyRender11
    {
        private static bool _drawingCameraLcds = false;

        [HarmonyPatch("VRageRender.MyRender11", "DrawGameScene")]
        [HarmonyPostfix]
        public static void MyRender11_DrawGameScene_Postfix()
        {
            if (!Plugin.Settings.Enabled || _drawingCameraLcds)
                return;
        
            _drawingCameraLcds = true;
            CameraLcdManager.Draw();
            _drawingCameraLcds = false;
        }
    }
}
