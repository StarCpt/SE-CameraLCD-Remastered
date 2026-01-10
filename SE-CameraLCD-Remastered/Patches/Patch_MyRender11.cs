using HarmonyLib;
using VRage.Render11.Culling;
using VRage.Render11.Render;
using VRage.Render11.Resources;
using VRageRender;

namespace CameraLCD.Patches
{
    //[HarmonyPatch]
    //public static class Patch_MyRender11
    //{
    //    [HarmonyPatch(typeof(MyRender11), nameof(MyRender11.DrawGameScene))]
    //    [HarmonyPrefix]
    //    public static void MyRender11_DrawGameScene_Prefix()
    //    {
    //        if (!Plugin.Settings.Enabled)
    //            return;
    //
    //        // don't draw cameralcd if a screenshot is being taken
    //        if (MyRender11.m_screenshot.HasValue)
    //            return;
    //
    //        CameraLcdManager.Draw();
    //    }
    //}

    [HarmonyPatch]
    public static class Patch_MyCullManager
    {
        [HarmonyPrefix, HarmonyPatch(typeof(MyCullManager), nameof(MyCullManager.InitFrame))]
        public static void InitFrame_Prefix(MyCullManager __instance)
        {
            if (!Plugin.Settings.Enabled)
                return;

            // don't draw cameralcd if a screenshot is being taken
            if (MyRender11.m_screenshot.HasValue)
                return;

            CameraLcdManager.Draw();
        }

        public static IBorrowedRtvTexture CameraViewRtv;
        public static IUserGeneratedTexture CameraLcdTexture;

        [HarmonyPostfix, HarmonyPatch(typeof(MyRenderScheduler), nameof(MyRenderScheduler.Done))]
        public static void MyRenderScheduler_Done_Postfix()
        {
            if (CameraViewRtv != null)
            {
                MyCopyToRT.Run(CameraLcdTexture, CameraViewRtv);
                MyRender11.RC.SetRtvNull();

                MyRender11.RC.GenerateMips(CameraLcdTexture);

                CameraViewRtv = null;
                CameraLcdTexture = null;
            }
        }
    }
}
