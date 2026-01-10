using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageRender;

namespace CameraLCD.Patches;

[HarmonyPatch]
public static class Patch_MyShadowCascadesStats
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MyShadowCascadesStats), nameof(MyShadowCascadesStats.Init))]
    public static IEnumerable<CodeInstruction> Init_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.operand is (sbyte)19)
            {
                yield return instruction.Clone(20);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MyShadowCascadesStats), nameof(MyShadowCascadesStats.Update))]
    public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.operand is (sbyte)19)
            {
                yield return instruction.Clone(20);
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
