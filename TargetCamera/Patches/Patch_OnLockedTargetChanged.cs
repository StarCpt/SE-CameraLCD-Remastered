
using HarmonyLib;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Entity;

namespace SETargetCamera.Patches
{
    
    [HarmonyPatch(typeof(MyTargetLockingComponent), "OnLockedTargetChanged")]
    public static class Patch_OnLockedTargetChanged
    {
        public static void Postfix(MyTargetLockingComponent __instance, IMyTargetingCapableBlock controlledBlock, MyEntity target)
        {
            TargetCamera.SetTarget(controlledBlock, target);
        }
    }
}