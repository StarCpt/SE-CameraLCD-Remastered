using HarmonyLib;
using System;
using VRageMath;
using VRageRender;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor.VRage.Render11.Culling
{
    public class MyCullQueries
    {
        private delegate void AddForwardPassDel(object target, int index, ref Matrix offsetedViewProjection, ref MatrixD viewProjection, ref Vector3D viewOrigin, ref Matrix projection, ref MyViewport viewport, object dsv, object srvDepth, object rtv0, object rtv1);

        private static readonly Type _MyCullQueries = AccessTools.TypeByName("VRage.Render11.Culling.MyCullQueries");
        private static readonly Action<object> _Reset = _MyCullQueries.Method("Reset").CreateInvoker();
        private static readonly AddForwardPassDel _AddForwardPass = _MyCullQueries.Method("AddForwardPass").CreateInvoker();

        public object Instance { get; }

        internal MyCullQueries(object instance)
        {
            Instance = instance;
        }

        public void Reset() => _Reset.Invoke(Instance);
        public void AddForwardPass(int index, ref Matrix offsetedViewProjection, ref MatrixD viewProjection, ref Vector3D viewOrigin, ref Matrix projection, ref MyViewport viewport, IDsvBindable dsv, ISrvBindable srvDepth, IRtvBindable rtv0, IRtvBindable rtv1) =>
            _AddForwardPass.Invoke(Instance, index, ref offsetedViewProjection, ref viewProjection, ref viewOrigin, ref projection, ref viewport, dsv.Instance, srvDepth.Instance, rtv0.Instance, rtv1.Instance);
    }
}
