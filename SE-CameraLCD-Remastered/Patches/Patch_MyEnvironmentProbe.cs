using HarmonyLib;
using VRage.Render11.LightingStage.EnvironmentProbe;

namespace CameraLCD.Patches;

[HarmonyPatch(typeof(MyEnvironmentProbe))]
static class Patch_MyEnvironmentProbe
{
    [HarmonyPatch(nameof(MyEnvironmentProbe.UpdateCullQuery))]
    [HarmonyPrefix]
    static bool UpdateCullQuery_Prefix()
    {
        return !CameraViewRenderer.IsDrawing;
    }

    [HarmonyPatch(nameof(MyEnvironmentProbe.FinalizeEnvProbes))]
    [HarmonyPrefix]
    static bool FinalizeEnvProbes_Prefix()
    {
        return !CameraViewRenderer.IsDrawing;
    }

    [HarmonyPatch(nameof(MyEnvironmentProbe.UpdateProbe))]
    [HarmonyPrefix]
    static bool UpdateProbe_Prefix()
    {
        return !CameraViewRenderer.IsDrawing;
    }
}
