using System.Diagnostics;
using HarmonyLib;
using VRage.Render11.Resources;
using VRage.Utils;
using VRageRender;

namespace SETargetCamera.Patches
{
    [HarmonyPatch]
    public static class Patch_MyRender11
    {
        private static bool _drawingCameraLcds = false;

        [HarmonyPatch(typeof(MyRender11), nameof(MyRender11.DrawGameScene))]
        [HarmonyPrefix]
        public static void MyRender11_DrawGameScene_Prefix(IRtvBindable renderTarget)
        {
            DisplayFrameTimer.TimeSinceUpdateMs = DisplayFrameTimer.Stopwatch.Elapsed.TotalMilliseconds;
        }

        [HarmonyPatch(typeof(MyRender11), nameof(MyRender11.DrawGameScene))]
        [HarmonyPostfix]
        public static void MyRender11_DrawGameScene_Postfix(IRtvBindable renderTarget)
        {
            if (!Plugin.Settings.Enabled || _drawingCameraLcds)
                return;

            RenderTarget = renderTarget;

            _drawingCameraLcds = true;
            TargetCamera.Draw();

            RenderTarget = null;
            _drawingCameraLcds = false;
        }
        public static IRtvBindable RenderTarget { get; private set; }
    }
}