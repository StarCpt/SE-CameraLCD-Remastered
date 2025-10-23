using HarmonyLib;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using System.Reflection;

namespace CameraLCD.Patches
{
    [HarmonyPatch(typeof(MyTextSurfaceScriptFactory), nameof(MyTextSurfaceScriptFactory.LoadScripts))]
    public static class Patch_LoadScripts
    {
        public static void Postfix()
        {
            MyTextSurfaceScriptFactory.Instance.RegisterFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
