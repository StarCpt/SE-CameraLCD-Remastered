using HarmonyLib;
using SharpDX.Direct3D11;
using System;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor
{
    internal static class ViewBindableAccessor
    {
        public static readonly Type Type_ISrvBindable = AccessTools.TypeByName("VRage.Render11.Resources.ISrvBindable");
        public static readonly Type Type_IRtvBindable = AccessTools.TypeByName("VRage.Render11.Resources.IRtvBindable");
        public static readonly Type Type_IUavBindable = AccessTools.TypeByName("VRage.Render11.Resources.IUavBindable");

        private static readonly Func<object, ShaderResourceView> _ISrvBindable_Srv_Getter = Type_ISrvBindable.PropertyGetter("Srv").CreateInvoker();
        private static readonly Func<object, RenderTargetView> _IRtvBindable_Rtv_Getter = Type_IRtvBindable.PropertyGetter("Rtv").CreateInvoker();
        private static readonly Func<object, UnorderedAccessView> _IUavBindable_Uav_Getter = Type_IUavBindable.PropertyGetter("Uav").CreateInvoker();

        public static ShaderResourceView GetSrv(ISrvBindable srv) => _ISrvBindable_Srv_Getter.Invoke(srv.Instance);
        public static RenderTargetView GetRtv(IRtvBindable rtv) => _IRtvBindable_Rtv_Getter.Invoke(rtv.Instance);
        public static UnorderedAccessView GetUav(IUavBindable uav) => _IUavBindable_Uav_Getter.Invoke(uav.Instance);
    }
}
