using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using VRageRender;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor.VRageRender
{
    public static class MyCopyToRT
    {
        private static readonly Type _MyCopyToRT = AccessTools.TypeByName("VRageRender.MyCopyToRT");
        private static readonly Action<object, object, bool, MyViewport?, bool> _Run = _MyCopyToRT.Method("Run").CreateInvoker();
        private static readonly Func<object> _m_copyPs = _MyCopyToRT.Field("m_copyPs").CreateStaticGetter<object>();

        public static unsafe MyPixelShaders.Id CopyPs
        {
            get
            {
                var boxedId = _m_copyPs.Invoke();
                return Unsafe.As<object, Box<MyPixelShaders.Id>>(ref boxedId).Value;
            }
        }

        public static void Run(IRtvBindable destination, ISrvBindable source, bool alphaBlended = false, MyViewport? customViewport = null, bool shouldStretch = false) =>
            _Run.Invoke(destination.Instance, source.Instance, alphaBlended, customViewport, shouldStretch);
    }
}
