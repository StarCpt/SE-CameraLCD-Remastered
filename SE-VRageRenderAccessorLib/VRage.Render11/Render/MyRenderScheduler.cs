using HarmonyLib;
using System;

namespace VRageRenderAccessor.VRage.Render11.Render
{
    public class MyRenderScheduler : IPrivateObjectWrapper
    {
        private static readonly Type _MyRenderScheduler = AccessTools.TypeByName("VRage.Render11.Render.MyRenderScheduler");
        private static readonly Action<object> _Init = _MyRenderScheduler.Method("Init").CreateInvoker();
        private static readonly Action<object> _Execute = _MyRenderScheduler.Method("Execute").CreateInvoker();
        private static readonly Action<object> _Done = _MyRenderScheduler.Method("Done").CreateInvoker();

        public object Instance { get; }

        internal MyRenderScheduler(object instance)
        {
            Instance = instance;
        }

        public void Init() => _Init.Invoke(Instance);
        public void Execute() => _Execute.Invoke(Instance);
        public void Done() => _Done.Invoke(Instance);
    }
}