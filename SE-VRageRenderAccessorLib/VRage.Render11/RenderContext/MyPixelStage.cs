using HarmonyLib;
using SharpDX.Direct3D11;
using System;

namespace VRageRenderAccessor.VRage.Render11.RenderContext
{
    public class MyPixelStage : MyCommonStage
    {
        private static readonly Type _MyPixelStage = AccessTools.TypeByName("VRage.Render11.RenderContext.MyPixelStage");
        private static readonly Action<object, PixelShader> _Set = _MyPixelStage.Method("Set").CreateInvoker();

        internal MyPixelStage(object instance) : base(instance)
        {
        }

        public void Set(PixelShader shader) => _Set(Instance, shader);
    }
}
