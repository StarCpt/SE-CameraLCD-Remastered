using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageRender;

namespace CameraLCD.Patches;

[HarmonyPatch]
public static class Patch_MyGeometryRendererOld
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(MyGeometryRendererOld), nameof(MyGeometryRendererOld.InitFrame))]
    public static IEnumerable<CodeInstruction> InitFrame_Transpiler(IEnumerable<CodeInstruction> instructions)
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
