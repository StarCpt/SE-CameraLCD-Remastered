using System.Diagnostics;
using HarmonyLib;
using NLog.Targets;
using VRage.Utils;
using VRageRenderAccessor.VRage.Render11.Resources;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;

namespace SETargetCamera.Patches
{
    [HarmonyPatch]
    public static class Patch_MyRender11
    {
        private static bool _drawingCameraLcds = false;

        [HarmonyPatch("VRageRender.MyRender11", "DrawGameScene")]
        [HarmonyPrefix]
        public static void MyRender11_DrawGameScene_Prefix(object renderTarget)
        {
            DisplayFrameTimer.TimeSinceUpdateMs = DisplayFrameTimer.Stopwatch.Elapsed.TotalMilliseconds;
        }

        [HarmonyPatch("VRageRender.MyRender11", "DrawGameScene")]
        [HarmonyPostfix]
        public static void MyRender11_DrawGameScene_Postfix(object renderTarget)
        {
            if (!Plugin.Settings.Enabled || _drawingCameraLcds)
                return;

            RenderTarget = new MyRtvTexture(renderTarget);

            _drawingCameraLcds = true;
            TargetCamera.Draw();

            RenderTarget = null;
            _drawingCameraLcds = false;
        }
        public static IRtvBindable RenderTarget { get; private set; }
    }
}