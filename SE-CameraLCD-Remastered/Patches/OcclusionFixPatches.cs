using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using VRage.Render.Scene;
using VRage.Render11.Culling;
using VRage.Render11.Culling.Occlusion;

namespace CameraLCD.Patches;

/// <summary>
/// Patches to fix SE's occlusion culling system making things invisible in camera view.
/// </summary>
//[HarmonyPatch]
static class OcclusionFixPatches
{
    const int GBUFFER_PASS_VIEW_ID = 0;

    // use a transpiler since the patched method is called pretty often and adding call overhead is not great
    [HarmonyPatch(typeof(MyActor), nameof(MyActor.IsOccluded))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> MyActor_IsOccluded_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // insert this code at the start of the method:
        // if (viewId is GBUFFER_PASS_VIEW_ID && CameraViewRenderer.FixOcclusion)
        // {
        //     return false;
        // }
        // <original method>

        List<CodeInstruction> patch = [];
        
        Label label = generator.DefineLabel();
        
        // load int viewId (0th arg is the MyActor instance)
        patch.Add(new CodeInstruction(OpCodes.Ldarg_1));
        // optimization: since gbuffer pass id is 0, we can jump if viewId is nonzero
        // jump to label if viewId != 0
        patch.Add(new CodeInstruction(OpCodes.Brtrue_S, label));
        
        // load bool CameraViewRenderer.FixOcclusion
        patch.Add(CodeInstruction.LoadField(typeof(CameraViewRenderer), nameof(CameraViewRenderer.FixOcclusion)));
        // jump to label if !CameraViewRenderer.FixOcclusion
        patch.Add(new CodeInstruction(OpCodes.Brfalse_S, label));
        
        // path if viewId == 0 && CameraViewRenderer.FixOcclusion
        // load bool false (int32 0x0)
        patch.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
        // return false;
        patch.Add(new CodeInstruction(OpCodes.Ret));

        // add labels at the start of the original method
        instructions.First().labels.Add(label);
        
        patch.AddRange(instructions);
        return patch;
    }

    [HarmonyPatch(typeof(MyOcclusionTask), nameof(MyOcclusionTask.DoWork))]
    [HarmonyPrefix]
    static bool MyOcclusionTask_DoWork_Prefix(ref int __result, MyCullQuery cullQuery)
    {
        if (cullQuery.ViewId is GBUFFER_PASS_VIEW_ID && CameraViewRenderer.FixOcclusion)
        {
            __result = 0;
            return false;
        }
        return true;
    }
}
