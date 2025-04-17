using HarmonyLib;
using System;
using VRageRenderAccessor.VRage.Render11.Culling;
using VRageRenderAccessor.VRage.Render11.GeometryStage2.Model;
using VRageRenderAccessor.VRage.Render11.GeometryStage2.Rendering;
using VRageRenderAccessor.VRage.Render11.Render;
using VRageRenderAccessor.VRage.Render11.Resources;

namespace VRageRenderAccessor.VRage.Render11.Common
{
    public static class MyManagers
    {
        private static readonly Type _MyManagers = AccessTools.TypeByName("VRage.Render11.Common.MyManagers");

        public static readonly MyFileTextureManager FileTextures = new MyFileTextureManager(_MyManagers.Field(nameof(FileTextures)).GetValue(null));
        public static readonly MyBorrowedRwTextureManager RwTexturesPool = new MyBorrowedRwTextureManager(_MyManagers.Field(nameof(RwTexturesPool)).GetValue(null));
        public static readonly MyModelFactory ModelFactory = new MyModelFactory(_MyManagers.Field(nameof(ModelFactory)).GetValue(null));
        public static readonly MyCullManager Cull = new MyCullManager(_MyManagers.Field(nameof(Cull)).GetValue(null));
        public static readonly MyGeometryRenderer GeometryRenderer = new MyGeometryRenderer(_MyManagers.Field(nameof(GeometryRenderer)).GetValue(null));
        public static readonly MyRenderScheduler RenderScheduler = new MyRenderScheduler(_MyManagers.Field(nameof(RenderScheduler)).GetValue(null));
    }
}
