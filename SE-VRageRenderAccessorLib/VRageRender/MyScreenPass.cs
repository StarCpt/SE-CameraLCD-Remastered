using HarmonyLib;
using System;
using VRageRender;
using VRageRenderAccessor.VRage.Render11.RenderContext;

namespace VRageRenderAccessor.VRageRender
{
    public static class MyScreenPass
    {
        private static readonly Type _MyScreenPass = AccessTools.TypeByName("VRageRender.MyScreenPass");
        private static readonly Action<object, MyViewport?, bool> _DrawFullscreenQuad = _MyScreenPass.Method("DrawFullscreenQuad").CreateInvoker();

        public static void DrawFullscreenQuad(MyRenderContext rc, MyViewport? customViewport = null, bool updateVertexBuffer = true) =>
            _DrawFullscreenQuad(rc.Instance, customViewport, updateVertexBuffer);
    }
}
