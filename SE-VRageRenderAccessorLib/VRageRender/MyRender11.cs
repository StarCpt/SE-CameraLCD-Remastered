using HarmonyLib;
using SharpDX.Direct3D11;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using VRageMath;
using VRageRender;
using VRageRender.Messages;
using VRageRenderAccessor.VRage.Render11.RenderContext;
using VRageRenderAccessor.VRage.Render11.Resources;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;

namespace VRageRenderAccessor.VRageRender
{
    public static class MyRender11
    {
        private static readonly Type _MyRender11 = AccessTools.TypeByName("VRageRender.MyRender11");
        private static readonly Type _MyEnvironment = AccessTools.TypeByName("VRageRender.MyEnvironment");

        // Methods
        private static readonly MethodInfo _DrawGameScene = _MyRender11.Method("DrawGameScene");
        private static readonly Action _PrepareGameScene = _MyRender11.Method("PrepareGameScene").CreateInvoker();
        private static readonly Action<MyRenderMessageSetCameraViewMatrix> _SetupCameraMatrices = _MyRender11.Method("SetupCameraMatrices").CreateInvoker();

        // Properties
        private static readonly Func<Device1> _DeviceInstance_Getter = _MyRender11.PropertyGetter("DeviceInstance").CreateInvoker();
        private static readonly Func<MyRenderDebugOverrides> _DebugOverrides_Getter = _MyRender11.PropertyGetter("DebugOverrides").CreateInvoker();
        private static readonly Func<object> _RC_Getter = _MyRender11.PropertyGetter("RC").CreateInvoker();
        private static readonly Func<bool> _FxaaEnabled_Getter = _MyRender11.PropertyGetter("FxaaEnabled").CreateInvoker();

        // Fields
        private static readonly AccessTools.FieldRef<object> _Environment = _MyRender11.StaticFieldRefAccess<object>("Environment");
        private static readonly AccessTools.FieldRef<object, object> _MyEnvironment_Matrices = _MyEnvironment.FieldRefAccess<object>("Matrices");
        private static readonly AccessTools.FieldRef<object, Vector2I> _m_resolution = _MyRender11.FieldRefAccess<Vector2I>("m_resolution");
        private static readonly AccessTools.FieldRef<object, MyPostprocessSettings> _Postprocess = _MyRender11.FieldRefAccess<MyPostprocessSettings>("Postprocess");
        private static readonly AccessTools.FieldRef<object, MyRenderSettings> _Settings = _MyRender11.FieldRefAccess<MyRenderSettings>("Settings");
        private static readonly AccessTools.FieldRef<object, Vector2I> _ViewportResolution_BackingField = _MyRender11.BackingFieldRefAccess<Vector2I>("ViewportResolution");

        public static Device1 DeviceInstance => _DeviceInstance_Getter.Invoke();
        public static MyRenderDebugOverrides DebugOverrides => _DebugOverrides_Getter.Invoke();
        public static ref Vector2I ResolutionI => ref _m_resolution.Invoke();
        public static Vector2 ResolutionF => (Vector2)ResolutionI;
        public static ref Vector2I ViewportResolution => ref _ViewportResolution_BackingField.Invoke();
        public static MyRenderContext RC => new MyRenderContext(_RC_Getter.Invoke());
        public static ref MyPostprocessSettings Postprocess => ref _Postprocess.Invoke();
        public static bool FxaaEnabled => _FxaaEnabled_Getter.Invoke();
        public static ref MyRenderSettings Settings => ref _Settings.Invoke();

        public static MyEnvironmentMatrices GetEnvironmentMatrices()
        {
            var myEnvironmentInstance = _Environment.Invoke();
            var myEnvironmentMatricesInstance = _MyEnvironment_Matrices.Invoke(myEnvironmentInstance);
            return Unsafe.As<MyEnvironmentMatrices>(myEnvironmentMatricesInstance);
        }

        public static void DrawGameScene(IRtvBindable renderTarget, out IBorrowedRtvTexture debugAmbientOcclusion)
        {
            object[] parameters =
            {
                renderTarget.Instance, null,
            };
            _DrawGameScene.Invoke(null, parameters);
            debugAmbientOcclusion = parameters[1] != null ? new MyBorrowedRtvTexture(parameters[1]) : null;
        }

        public static void PrepareGameScene() => _PrepareGameScene.Invoke();
        public static void SetupCameraMatrices(MyRenderMessageSetCameraViewMatrix renderMessage) => _SetupCameraMatrices.Invoke(renderMessage);
    }
}
