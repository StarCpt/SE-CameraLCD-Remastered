using HarmonyLib;
using System;
using VRageRender;

namespace VRageRenderAccessor.VRageRender
{
    public static class MyCommon
    {
        private static readonly Type _MyCommon = AccessTools.TypeByName("VRageRender.MyCommon");
        private static readonly AccessTools.FieldRef<MyNewLoddingSettings> _LoddingSettings_BackingField = _MyCommon.StaticBackingFieldRefAccess<MyNewLoddingSettings>("LoddingSettings");

        public static ref MyNewLoddingSettings LoddingSettings => ref _LoddingSettings_BackingField.Invoke();
    }
}
