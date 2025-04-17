using HarmonyLib;
using System;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor
{
    internal class IBorrowedSrvTextureAccessor
    {
        public static readonly Type _IBorrowedSrvTexture = AccessTools.TypeByName("VRage.Render11.Resources.IBorrowedSrvTexture");
        private static readonly Action<object> _Method_AddRef = _IBorrowedSrvTexture.Method("AddRef").CreateInvoker();
        private static readonly Action<object> _Method_Release = _IBorrowedSrvTexture.Method("Release").CreateInvoker();

        public static void AddRef(IBorrowedSrvTexture myBorrowedRtvTexture) => _Method_AddRef.Invoke(myBorrowedRtvTexture.Instance);
        public static void Release(IBorrowedSrvTexture myBorrowedRtvTexture) => _Method_Release.Invoke(myBorrowedRtvTexture.Instance);
    }
}