using HarmonyLib;
using System;
using System.Reflection;
using VRageRenderAccessor.VRage.Render11.RenderContext;
using VRageRenderAccessor.VRage.Render11.Resources;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;

namespace VRageRenderAccessor.VRageRender
{
    public static class MyEyeAdaptation
    {
        private static readonly Type _MyEyeAdaptation = AccessTools.TypeByName("VRageRender.MyEyeAdaptation");
        private static readonly Func<object> _Method_GetExposure = _MyEyeAdaptation.Method("GetExposure").CreateInvoker();
        private static readonly Action<object> _Method_ConstantExposure = _MyEyeAdaptation.Method("ConstantExposure").CreateInvoker();
        private static readonly MethodInfo _Method_Run = _MyEyeAdaptation.Method("Run");

        public static MyRtvTexture GetExposure() => new MyRtvTexture(_Method_GetExposure.Invoke());
        public static void ConstantExposure(MyRenderContext rc) => _Method_ConstantExposure.Invoke(rc.Instance);
        public static void Run(MyRenderContext rc, ISrvTexture src, bool createDebugHistogram, out IBorrowedRtvTexture debugHistogram)
        {
            object[] parameters =
            {
                rc.Instance, src.Instance, createDebugHistogram, null,
            };
            _Method_Run.Invoke(null, parameters);
            debugHistogram = parameters[3] != null ? new MyBorrowedRtvTexture(parameters[3]) : null;
        }
    }
}
