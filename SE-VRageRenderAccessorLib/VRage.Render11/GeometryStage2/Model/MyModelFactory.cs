using HarmonyLib;
using System;

namespace VRageRenderAccessor.VRage.Render11.GeometryStage2.Model
{
    public class MyModelFactory : IPrivateObjectWrapper
    {
        private static readonly Type _MyModelFactory = AccessTools.TypeByName("VRage.Render11.GeometryStage2.Model.MyModelFactory");
        private static readonly Action<object> _OnLoddingSettingChanged = _MyModelFactory.Method("OnLoddingSettingChanged").CreateInvoker();

        public object Instance { get; }

        internal MyModelFactory(object instance)
        {
            Instance = instance;
        }

        public void OnLoddingSettingChanged() => _OnLoddingSettingChanged.Invoke(Instance);
    }
}