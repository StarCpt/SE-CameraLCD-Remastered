using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Render11.Scene;

namespace CameraLCD.Patches;

[HarmonyPatch]
public static class Patch_MyScene11
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MyScene11), nameof(MyScene11.AllocateGroupData))]
    public static IEnumerable<CodeInstruction> AllocateGroupData_Transpiler(IEnumerable<CodeInstruction> instructions)
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
