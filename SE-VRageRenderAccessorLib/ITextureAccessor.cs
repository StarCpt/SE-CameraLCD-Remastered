using HarmonyLib;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor
{
    internal static class ITextureAccessor
    {
        public static readonly Type _ITexture = AccessTools.TypeByName("VRage.Render11.Resources.ITexture");

        private static readonly Func<object, Format> _Format_Getter = _ITexture.PropertyGetter("Format").CreateInvoker();
        private static readonly Func<object, int> _MipLevels_Getter = _ITexture.PropertyGetter("MipLevels").CreateInvoker();

        public static Format GetFormat(this ITexture texture) => _Format_Getter.Invoke(texture.Instance);
        public static int GetMipLevels(this ITexture texture) => _MipLevels_Getter.Invoke(texture.Instance);
    }
}
