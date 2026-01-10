using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using VRage.Render.Scene;

namespace CameraLCD.Patches;

[HarmonyPatch]
public static class Patch_MyActor
{
    [HarmonyTranspiler, HarmonyPatch(typeof(MyActor), MethodType.Constructor, typeof(MyScene))]
    public static IEnumerable<CodeInstruction> ctor_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeInstruction[] instructions2 = instructions.ToArray();
        for (int i = 0; i < instructions2.Length; i++)
        {
            CodeInstruction item = instructions2[i];
            if (item.opcode == OpCodes.Ldc_I4_S && item.operand is (sbyte)19 && i < (instructions2.Length - 1) && instructions2[i + 1].opcode == OpCodes.Newarr)
            {
                instructions2[i] = item.Clone(20);
            }
        }
        return instructions2;
    }

    //[HarmonyPrefix, HarmonyPatch(typeof(MyActor), nameof(MyActor.IsOccluded))]
    //public static void IsOccluded_Prefix(MyActor __instance, int viewId)
    //{
    //    if (viewId >= 19)
    //    {
    //        if (viewId >= __instance.FrameInView.Length || viewId >= __instance.OccludedState.Length)
    //        {
    //
    //        }
    //    }
    //}
}
