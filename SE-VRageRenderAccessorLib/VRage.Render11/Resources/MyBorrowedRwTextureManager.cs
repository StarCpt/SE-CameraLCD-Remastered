using HarmonyLib;
using SharpDX.DXGI;
using System;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;

namespace VRageRenderAccessor.VRage.Render11.Resources
{
    public class MyBorrowedRwTextureManager : IPrivateObjectWrapper
    {
        private static readonly Type _MyBorrowedRwTextureManager = AccessTools.TypeByName("VRage.Render11.Resources.MyBorrowedRwTextureManager");
        private static readonly Func<object, string, int, int, Format, int, int, object> _Method_BorrowRtv = _MyBorrowedRwTextureManager.Method(
            nameof(BorrowRtv),
            new Type[] { typeof(string), typeof(int), typeof(int), typeof(Format), typeof(int), typeof(int) })
            .CreateInvoker();

        public object Instance { get; }

        internal MyBorrowedRwTextureManager(object instance)
        {
            Instance = instance;
        }

        public MyBorrowedRtvTexture BorrowRtv(string debugName, int width, int height, Format format, int samplesCount = 1, int samplesQuality = 0)
        {
            return new MyBorrowedRtvTexture(_Method_BorrowRtv.Invoke(Instance, debugName, width, height, format, samplesCount, samplesQuality));
        }
    }
}