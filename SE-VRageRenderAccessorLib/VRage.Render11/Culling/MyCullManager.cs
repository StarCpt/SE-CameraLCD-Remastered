using HarmonyLib;
using System;

namespace VRageRenderAccessor.VRage.Render11.Culling
{
    public class MyCullManager : IPrivateObjectWrapper
    {
        private static readonly Type _MyCullManager = AccessTools.TypeByName("VRage.Render11.Culling.MyCullManager");
        private static readonly Action<object> _OnFrameEnd = _MyCullManager.Method("OnFrameEnd").CreateInvoker();

        public object Instance { get; }

        internal MyCullManager(object instance)
        {
            Instance = instance;
        }

        public void OnFrameEnd() => _OnFrameEnd.Invoke(Instance);
    }
}