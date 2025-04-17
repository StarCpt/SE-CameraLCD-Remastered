using HarmonyLib;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor.VRage.Render11.RenderContext
{
    public class MyRenderContext : IPrivateObjectWrapper
    {
        private static readonly Type _MyRenderContext = AccessTools.TypeByName("VRage.Render11.RenderContext.MyRenderContext");
        public static readonly Type _IRtvBindable = AccessTools.TypeByName("VRage.Render11.Resources.IRtvBindable");
        public static readonly Type _IDsvBindable = AccessTools.TypeByName("VRage.Render11.Resources.IDsvBindable");

        // Methods
        private static readonly Action<object> _ClearState = _MyRenderContext.Method("ClearState").CreateInvoker();
        private static readonly Action<object, object, RawColor4> _ClearRtv = _MyRenderContext.Method("ClearRtv").CreateInvoker();
        private static readonly Action<object, InputLayout> _SetInputLayout = _MyRenderContext.Method("SetInputLayout").CreateInvoker();
        private static readonly Action<object, object, RawColor4?> _SetBlendState = _MyRenderContext.Method("SetBlendState").CreateInvoker();
        private static readonly Action<object, object, int> _SetDepthStencilState = _MyRenderContext.Method("SetDepthStencilState").CreateInvoker();
        private static readonly Action<object, object> _SetRtv1 = _MyRenderContext.Method("SetRtv", new Type[] { _IRtvBindable }).CreateInvoker();
        private static readonly Action<object, object> _SetRtv2 = _MyRenderContext.Method("SetRtv", new Type[] { _IDsvBindable }).CreateInvoker();
        private static readonly Action<object, object, object> _SetRtv3 = _MyRenderContext.Method("SetRtv", new Type[] { _IDsvBindable, _IRtvBindable }).CreateInvoker();

        // Fields
        private static readonly AccessTools.FieldRef<object, object> _m_pixelShaderStage = _MyRenderContext.FieldRefAccess<object>("m_pixelShaderStage");
        private static readonly AccessTools.FieldRef<object, object> _m_vertexShaderStage = _MyRenderContext.FieldRefAccess<object>("m_vertexShaderStage");
        private static readonly AccessTools.FieldRef<object, object> _m_computeShaderStage = _MyRenderContext.FieldRefAccess<object>("m_computeShaderStage");

        public object Instance { get; }
        public MyPixelStage PixelShader => new MyPixelStage(_m_pixelShaderStage.Invoke(Instance));

        internal MyRenderContext(object instance)
        {
            Instance = instance;
        }

        public void ClearState() => _ClearState.Invoke(Instance);
        public void ClearRtv(IRtvBindable rtv, RawColor4 colorRGBA) => _ClearRtv.Invoke(Instance, rtv.Instance, colorRGBA);
        public void SetInputLayout(InputLayout il) => _SetInputLayout.Invoke(Instance, il);
        public void SetBlendState(IBlendState bs, RawColor4? blendFactor = null) => _SetBlendState.Invoke(Instance, bs.Instance, blendFactor);
        public void SetDepthStencilState(IDepthStencilState dss, int stencilRef = 0) => _SetDepthStencilState.Invoke(Instance, dss.Instance, stencilRef);
        public void SetRtv(IRtvBindable rtv) => _SetRtv1.Invoke(Instance, rtv.Instance);
        public void SetRtv(IDsvBindable dsv) => _SetRtv2.Invoke(Instance, dsv.Instance);
        public void SetRtv(IDsvBindable dsv, IRtvBindable rtv) => _SetRtv3.Invoke(Instance, dsv.Instance, rtv.Instance);
    }
}
