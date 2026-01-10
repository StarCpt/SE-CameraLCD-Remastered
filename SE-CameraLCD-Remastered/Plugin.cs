using CameraLCD.Gui;
using HarmonyLib;
using ParallelTasks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Reflection;
using VRage;
using VRage.Plugins;
using VRage.Render.Scene;
using VRage.Render11.Common;
using VRage.Render11.GeometryStage2.Rendering;
using VRage.Render11.Render;
using VRage.Render11.Scene;
using VRage.Render11.Tools;
using VRageRender;

namespace CameraLCD
{
    public class Plugin : IPlugin
    {
        public static CameraLCDSettings Settings { get; private set; }
        public static Boxed<(uint CharacterActorId, string[] MaterialsDisabledInFirst)>? FirstPersonCharacter = null;

        public Plugin()
        {
            Settings = CameraLCDSettings.Load();
        }

        public void Init(object gameInstance)
        {
            string[] viewNames = MyViewIds.ViewNames;
            Array.Resize(ref viewNames, MyViewIds.MAX_VIEW_COUNT + 1);
            viewNames[19] = "CameraLCD";
            MyViewIds.ViewNames = viewNames;

            Array.Resize(ref MyManagers.GeometryRenderer.m_visibleStaticGroups, 20);
            MyManagers.GeometryRenderer.m_visibleStaticGroups[19] = [];

            Array.Resize(ref MyManagers.GeometryRenderer.m_visibleInstances, 20);
            MyManagers.GeometryRenderer.m_visibleInstances[19] = [];

            Array.Resize(ref MyManagers.GeometryRenderer.m_instanceRenderData, 20);
            MyManagers.GeometryRenderer.m_instanceRenderData[19] = new MyRenderData();

            Array.Resize(ref MyManagers.GeometryRenderer.m_staticGroupsRenderData, 20);
            MyManagers.GeometryRenderer.m_staticGroupsRenderData[19] = [];

            Array.Resize(ref AccessTools.FieldRefAccess<MyGeometryRenderer, MyPassLoddingSetting[]>(nameof(MyGeometryRenderer.m_loddingSetting)).Invoke(MyManagers.GeometryRenderer), 20);
            MyManagers.GeometryRenderer.m_loddingSetting[19] = MyManagers.GeometryRenderer.m_loddingSetting[18];

            Array.Resize(ref AccessTools.FieldRefAccess<MyRenderScheduler, MyRendererStats.MyCullStats[]>(nameof(MyRenderScheduler.m_lastCullStats)).Invoke(MyManagers.RenderScheduler), 20);
            Array.Resize(ref AccessTools.FieldRefAccess<MyRenderScheduler, DependencyResolver.JobToken[]>(nameof(MyRenderScheduler.m_oldPrepareJob)).Invoke(MyManagers.RenderScheduler), 20);
            Array.Resize(ref AccessTools.StaticFieldRefAccess<MyRendererStats, MyRendererStats.MyCullStats[]>(nameof(MyRendererStats.ViewCullStats)), 20);
            Array.Resize(ref AccessTools.StaticFieldRefAccess<MyRendererStats, MyRendererStats.MyRenderStats[]>(nameof(MyRendererStats.m_viewRenderStats)), 20);

            foreach (var actor in MyActorFactory.m_pool.m_unused)
            {
                Array.Resize(ref AccessTools.FieldRefAccess<MyActor, long[]>(nameof(MyActor.FrameInView)).Invoke(actor), 20);
                Array.Resize(ref AccessTools.FieldRefAccess<MyActor, bool[]>("<OccludedState>k__BackingField").Invoke(actor), 20);
            }
            
            foreach (var data in MyScene11.m_groupDataPool.m_unused)
            {
                if (data.RenderCullData is not null && data.RenderCullData.Length < 20)
                {
                    Array.Resize(ref data.RenderCullData, 20);
                }
            }

            AccessTools.FieldRefAccess<MyShadowCascadesStats, uint[,]>(nameof(MyShadowCascadesStats.m_pixelCountsPerView)).Invoke(MyManagers.Shadows.ShadowCascades.m_cascadeStats) = new uint[20, 8];
            AccessTools.FieldRefAccess<MyShadowCascadesStats, MyShadowCascadesStats.Issued[]>(nameof(MyShadowCascadesStats.m_finishedItems)).Invoke(MyManagers.Shadows.ShadowCascades.m_cascadeStats) = new MyShadowCascadesStats.Issued[20];
            for (int i = 0; i < 20; i++)
            {
                MyManagers.Shadows.ShadowCascades.m_cascadeStats.m_finishedItems[i].ViewId = -1;
            }

            new Harmony(nameof(CameraLCD)).PatchAll(Assembly.GetExecutingAssembly());
        }

        private uint _counter = 0;
        public void Update()
        {
            if (++_counter % 10 != 0 || !Settings.Enabled)
                return;

            if (MySession.Static?.CameraController?.Entity is MyCharacter character && (character.IsInFirstPersonView || character.ForceFirstPersonCamera))
            {
                FirstPersonCharacter = new((character.Render.GetRenderObjectID(), character.Definition.MaterialsDisabledIn1st));
            }
            else
            {
                FirstPersonCharacter = null;
            }
        }

        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenPluginConfig());
        }

        public void Dispose()
        {
        }
    }
}
