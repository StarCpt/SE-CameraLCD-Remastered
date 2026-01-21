using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Render11.RenderContext;
using VRage.Render11.Resources;
using VRageRender;

namespace SETargetCamera.Patches
{
    [HarmonyPatch]
    public static class Patch_MyShadowCascades
    {
        [HarmonyPatch(typeof(MyShadowCascades), nameof(MyShadowCascades.PostProcess))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PostProcess_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo targetMethod = AccessTools.Method(typeof(MyShadowCascades), nameof(MyShadowCascades.Gather));
            MethodInfo patchMethod = AccessTools.Method(typeof(Patch_MyShadowCascades), nameof(Patch_MyShadowCascades.Gather_Patch));

            foreach (var instruction in instructions)
            {
                if (instruction.operand as MethodInfo == targetMethod)
                {
                    instruction.operand = patchMethod;
                }

                yield return instruction;
            }
        }

        [HarmonyPatch(typeof(MyShadowCascades), nameof(MyShadowCascades.Gather))]
        [HarmonyPrefix]
        public static bool Gather_Prefix(MyShadowCascades __instance, MyRenderContext rc, ref MyCommon.MyScreenLayout layout, ISrvBindable srvDepth, int viewId)
        {
            Gather_Patch(__instance, rc, ref layout, srvDepth, viewId);
            return false;
        }

        static void Gather_Patch(MyShadowCascades @this, MyRenderContext rc, ref MyCommon.MyScreenLayout layout, ISrvBindable srvDepth, int viewId)
        {
            if (@this.Enabled && !Patch_MyRender11.DrawingCameraLcds)
            {
                @this.m_cascadeStats.Gather(rc, @this.m_csmConstants, ref layout, srvDepth, viewId);
            }
        }

    }
}
