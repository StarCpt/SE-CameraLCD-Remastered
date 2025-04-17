using HarmonyLib;
using System;
using System.Reflection;
using VRageRenderAccessor.VRage.Render11.Resources;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;

namespace VRageRenderAccessor.VRageRender
{
    public static class MyToneMapping
    {
        private static readonly Type _MyToneMapping = AccessTools.TypeByName("VRageRender.MyToneMapping");
        private static readonly MethodInfo _Method_Run = _MyToneMapping.Method("Run");

        public static MyBorrowedCustomTexture Run(ISrvBindable src, ISrvBindable avgLum, ISrvBindable bloom, bool enableTonemapping, string dirtTexture, bool needsAlphaLuminance)
        {
            object[] parameters =
            {
                src.Instance, avgLum.Instance, bloom.Instance, enableTonemapping, dirtTexture, needsAlphaLuminance,
            };
            return new MyBorrowedCustomTexture(_Method_Run.Invoke(null, parameters));
        }
    }
}
