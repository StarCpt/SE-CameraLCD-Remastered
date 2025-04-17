using HarmonyLib;
using System;
using VRageRender;

namespace VRageRenderAccessor.VRage.Render11.GeometryStage2.Rendering
{
    public class MyGeometryRenderer : IPrivateObjectWrapper
    {
        private static readonly Type _MyGeometryRenderer = AccessTools.TypeByName("VRage.Render11.GeometryStage2.Rendering.MyGeometryRenderer");
        private static readonly AccessTools.FieldRef<object, bool> _IsLodUpdateEnabled = _MyGeometryRenderer.FieldRefAccess<bool>(nameof(IsLodUpdateEnabled));
        private static readonly AccessTools.FieldRef<object, MyGlobalLoddingSettings> _m_globalLoddingSettings = _MyGeometryRenderer.FieldRefAccess<MyGlobalLoddingSettings>("m_globalLoddingSettings");

        public object Instance { get; }

        public ref bool IsLodUpdateEnabled => ref _IsLodUpdateEnabled.Invoke(Instance);
        public ref MyGlobalLoddingSettings m_globalLoddingSettings => ref _m_globalLoddingSettings.Invoke(Instance);

        internal MyGeometryRenderer(object instance)
        {
            Instance = instance;
        }
    }
}