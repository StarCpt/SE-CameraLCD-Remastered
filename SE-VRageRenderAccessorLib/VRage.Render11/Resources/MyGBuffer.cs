using HarmonyLib;
using SharpDX.DXGI;
using System;
using System.Data;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public class MyGBuffer : IPrivateObjectWrapper
    {
        private static readonly Type _MyGBuffer = AccessTools.TypeByName("VRage.Render11.Resources.MyGBuffer");
        private static readonly AccessTools.FieldRef<object> _Main = _MyGBuffer.StaticFieldRefAccess<object>("Main");
        private static readonly AccessTools.FieldRef<object, object> _m_lbuffer = _MyGBuffer.FieldRefAccess<object>("m_lbuffer");
        private static readonly AccessTools.FieldRef<object, object> _m_gbuffer0 = _MyGBuffer.FieldRefAccess<object>("m_gbuffer0");
        private static readonly AccessTools.FieldRef<object, object> _m_gbuffer1 = _MyGBuffer.FieldRefAccess<object>("m_gbuffer1");
        private static readonly AccessTools.FieldRef<object, object> _m_gbuffer2 = _MyGBuffer.FieldRefAccess<object>("m_gbuffer2");

        public static MyGBuffer Main => new MyGBuffer(_Main.Invoke());

        public object Instance { get; }
        public MyRtvTexture LBuffer => new MyRtvTexture(_m_lbuffer.Invoke(Instance));
        public MyRtvTexture GBuffer0 => new MyRtvTexture(_m_gbuffer0.Invoke(Instance));
        public MyRtvTexture GBuffer1 => new MyRtvTexture(_m_gbuffer1.Invoke(Instance));
        public MyRtvTexture GBuffer2 => new MyRtvTexture(_m_gbuffer2.Invoke(Instance));

        public static Format LBufferFormat => Main.LBuffer.Format;

        internal MyGBuffer(object instance)
        {
            Instance = instance;
        }
    }
}
