using HarmonyLib;
using System;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor.VRage.Render11.RenderContext
{
    public class MyCommonStage : IPrivateObjectWrapper
    {
        private static readonly Type _MyCommonStage = AccessTools.TypeByName("VRage.Render11.RenderContext.MyCommonStage");
        private static readonly Action<object, int, object> _SetSrv = _MyCommonStage.Method("SetSrv").CreateInvoker();

        public object Instance { get; }

        internal MyCommonStage(object instance)
        {
            Instance = instance;
        }

        public void SetSrv(int slot, ISrvBindable srvBind) => _SetSrv(Instance, slot, srvBind.Instance);
    }
}