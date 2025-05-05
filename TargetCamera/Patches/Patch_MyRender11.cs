using HarmonyLib;
using VRageRenderAccessor.VRage.Render11.Resources;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;

namespace CameraLCD.Patches
{
    [HarmonyPatch]
    public static class Patch_MyRender11
    {
        private static bool _drawingCameraLcds = false;

        [HarmonyPatch("VRageRender.MyRender11", "DrawGameScene")]
        [HarmonyPostfix]
        public static void MyRender11_DrawGameScene_Postfix(object renderTarget)
        {
            if (!Plugin.Settings.Enabled || _drawingCameraLcds)
                return;

            RenderTarget = new MyRtvTexture(renderTarget);
            
            _drawingCameraLcds = true;
            CameraLcdManager.Draw();

            RenderTarget = null;
            _drawingCameraLcds = false;
        }

        public static IRtvBindable RenderTarget { get; private set; }
    }
}
