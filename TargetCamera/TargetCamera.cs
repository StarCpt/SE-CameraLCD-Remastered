using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.Utils;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Import;
using VRageRender.Messages;

using SETargetCamera;
using SETargetCamera.Patches;
using CoreSystems.Api;
using VRageRenderAccessor.VRage.Render11.Common;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;
using VRageRenderAccessor.VRageRender;

namespace SETargetCamera
{
    public class TargetCamera
    {
        private static IMyPlayer LocalPlayer => MyAPIGateway.Session?.Player;
        private static MyCharacter PlayerCharacter => LocalPlayer?.Character as MyCharacter;
        private static IMyEntityController PlayerController => LocalPlayer?.Controller;

        private static MyEntity _targetEntity;
        private static MyShipController _cockpit;

        private const string TextureName = "TargetCamera";


        private static WcApi _wcApi;
        private static bool _usesWc;
        private static bool _wasJustInCockpit;
        public static void ModLoad()
        {
            MyLog.Default.Log(MyLogSeverity.Info, "Target Camera binding MySession events");
        }
        public static void WorldLoad()
        {
            MyLog.Default.Log(MyLogSeverity.Info,"World loaded, attempting to load WC API...");
            _wcApi = new WcApi();
            _wcApi.Load(WCReadyCallback);
        }

        public static Action WCReadyCallback => WcCallback;
        private static void WcCallback()
        {
            MyLog.Default.Log(MyLogSeverity.Info, "WC exists");
            _usesWc = true;
            
        }

        public static void WorldUnload()
        {
            MyLog.Default.Log(MyLogSeverity.Info,"World unloaded, resetting WC API");
            _usesWc = false;
        }


        public static void Update()
        {
            // Step 1: Get the targeted entity and controlled grid
            
            var controlledEntity = PlayerController?.ControlledEntity;


            
            if (!(controlledEntity?.Entity is MyShipController cockpit))
            {
                TargetCamera._cockpit = null;
                _targetEntity = null;
                _wasJustInCockpit = false;
                return;
            }

            TargetCamera._cockpit = cockpit;
            
            if (_usesWc)
            {
                _targetEntity = _wcApi.GetAiFocus(cockpit.CubeGrid);
                
            }
            else if (!_wasJustInCockpit)
            {
                var targetData = cockpit.TargetData;
                _targetEntity = targetData.TargetId is 0 || !targetData.IsTargetLocked ? null : MyEntities.GetEntityById(targetData.TargetId);
                _wasJustInCockpit = true;
            }

            
        }

        // TODO: Credit Lurking StarCpt for doing most of the heavy lifting with the camera API
        public static void Draw()
        {
            try
            {
                MyCamera renderCamera = MySector.MainCamera;

                if (_targetEntity == null || _cockpit == null || renderCamera == null) return;
                var controlledGrid = _cockpit.CubeGrid;
                
                // MyEntities.TryGetEntityById(cockpit.TargetData.TargetId, out MyEntity targetEntity, true);
                
                // Step 2: Break early if it doesn't exist
                if (controlledGrid == null) return;
                
                
                #region disble post-processing effects and lod changes

                bool ogLods = SetLoddingEnabled(false);
                bool ogDrawBillboards = MyRender11.Settings.DrawBillboards;
                MyRender11.Settings.DrawBillboards = true;
                MyRenderDebugOverrides debugOverrides = MyRender11.DebugOverrides;
                bool ogFlares = debugOverrides.Flares;
                bool ogSSAO = debugOverrides.SSAO;
                bool ogBloom = debugOverrides.Bloom;
                debugOverrides.Flares = true;
                debugOverrides.SSAO = false;
                debugOverrides.Bloom = false;

                #endregion
                
                // Step 3: Get target camera details (near clip, fov, cockpit up)
                
                float targetCameraNearPlane = 5; // Can probably get rid of this
                var targetCameraUp = _cockpit.WorldMatrix.Up;
                var shipPos = _cockpit.CubeGrid.PositionComp.WorldVolume.Center;
                var targetPos = _targetEntity.PositionComp.WorldVolume.Center;
                var to = targetPos - shipPos;
                var dist = to.Length();
                if (dist < Plugin.Settings.MinRange) return;
                var dir = to / dist;
                float targetCameraFov = (float)GetFov(shipPos, to, dir, dist, _targetEntity);
                
                
                var targetCameraPos = shipPos + dir * controlledGrid.PositionComp.WorldVolume.Radius;

                // Step 4: Create a camera matrix from the current controlled grid, with a near clipping plane that excludes the current grid, pointed at the target, and FOV scaled

                var targetCameraViewMatrix = MatrixD.CreateLookAt(targetCameraPos, targetPos, targetCameraUp);

                // Step 5: Move the game camera to that matrix, take a image snapshot, then move it back
                Vector2I ogResolutionI = MyRender11.ResolutionI;

                Vector2I Size = new Vector2I(Plugin.Settings.Width, Plugin.Settings.Height);
                
                MyRender11.ViewportResolution = Size;
                MyRender11.ResolutionI = Size;
                SetCameraViewMatrix(targetCameraViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, targetCameraFov, targetCameraFov, targetCameraNearPlane, targetCameraPos, 1);

                // Draw the game to the screen
                var backbufferFormat = Patch_MyRender11.RenderTarget.Rtv.Description.Format;
                var borrowedRtv = MyManagers.RwTexturesPool.BorrowRtv(TextureName, Size.X, Size.Y, backbufferFormat);
                
                MyRender11.DrawGameScene(borrowedRtv, out var debugAmbientOcclusion);
                debugAmbientOcclusion.Release();

                MyRender11.DeviceInstance.ImmediateContext1.CopySubresourceRegion(
                    borrowedRtv.Resource, 0, null, 
                    Patch_MyRender11.RenderTarget.Resource, 0, 
                    Plugin.Settings.X, Plugin.Settings.Y
                    );
                borrowedRtv.Release();

                // Restore camera position
                MyRender11.ViewportResolution = ogResolutionI;
                MyRender11.ResolutionI = ogResolutionI;
                SetCameraViewMatrix(renderCamera.ViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, renderCamera.FieldOfView, renderCamera.FieldOfView, renderCamera.NearPlaneDistance, renderCamera.Position, 0);

                #region restore post-processing and lod settings

                SetLoddingEnabled(ogLods);
                MyRender11.Settings.DrawBillboards = ogDrawBillboards;
                debugOverrides.Flares = ogFlares;
                debugOverrides.SSAO = ogSSAO;
                debugOverrides.Bloom = ogBloom;

                #endregion
            }
            catch (Exception ex)
            {
                MyLog.Default.Log(MyLogSeverity.Critical, ex.ToString());
            }
            
        }

        public static double GetFov(Vector3D from, Vector3D to, Vector3D dir, double dist, MyEntity targetEntity)
        {
            // 1) Setup camera

            // 2) Pull the local AABB and its world matrix
            var localAabb    = targetEntity.PositionComp.LocalAABB;  // in object space
            MatrixD worldMat = targetEntity.WorldMatrix;

            // 3) Prepare to track the maximum half‑angle
            double maxTheta = 0.0;

            // 4) For each of the 8 local‑space corners...
            for (int sx = 0; sx <= 1; sx++)
            for (int sy = 0; sy <= 1; sy++)
            for (int sz = 0; sz <= 1; sz++)
            {
                // pick min or max on each axis
                Vector3D localCorner = new Vector3D(
                    sx == 0 ? localAabb.Min.X : localAabb.Max.X,
                    sy == 0 ? localAabb.Min.Y : localAabb.Max.Y,
                    sz == 0 ? localAabb.Min.Z : localAabb.Max.Z
                );

                // 5) transform into world space
                Vector3D worldCorner = Vector3D.Transform(localCorner, worldMat);

                // 6) compute half‑angle to that corner
                Vector3D v = worldCorner - from;
                if (dist <= 1e-6) continue;

                double cosTheta = Vector3D.Dot(v / dist, dir);
                cosTheta = MathHelper.Clamp(cosTheta, -1.0, 1.0);
                double theta = Math.Acos(cosTheta);

                if (theta > maxTheta)
                    maxTheta = theta;
            }

            // 7) full FOV
            return 2.0 * maxTheta;
        }



        
        private static bool SetLoddingEnabled(bool enabled)
        {
            // Reference: MyRender11.ProcessMessageInternal(MyRenderMessageBase message, int frameId)
            //              case MyRenderMessageEnum.UpdateNewLoddingSettings

            MyNewLoddingSettings loddingSettings = MyCommon.LoddingSettings;
            MyGlobalLoddingSettings globalSettings = loddingSettings.Global;
            bool initial = globalSettings.IsUpdateEnabled;
            if (initial == enabled)
                return initial;

            globalSettings.IsUpdateEnabled = enabled;
            loddingSettings.Global = globalSettings;
            MyManagers.GeometryRenderer.IsLodUpdateEnabled = enabled;
            MyManagers.GeometryRenderer.m_globalLoddingSettings = globalSettings;
            MyManagers.ModelFactory.OnLoddingSettingChanged();
            return initial;
        }

        private static void SetCameraViewMatrix(MatrixD viewMatrix, Matrix projectionMatrix, Matrix projectionFarMatrix, float fov, float fovSkybox, float nearPlane, Vector3D cameraPosition, int lastMomentUpdateIndex)
        {
            MyCamera renderCamera = MySector.MainCamera;
            MyRenderMessageSetCameraViewMatrix renderMessage = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);
            renderMessage.ViewMatrix = viewMatrix;
            renderMessage.ProjectionMatrix = projectionMatrix;
            renderMessage.ProjectionFarMatrix = projectionFarMatrix;
            renderMessage.FOV = fov;
            renderMessage.FOVForSkybox = fovSkybox;
            renderMessage.NearPlane = nearPlane;
            renderMessage.FarPlane = renderCamera.FarPlaneDistance;
            renderMessage.FarFarPlane = renderCamera.FarFarPlaneDistance;
            renderMessage.CameraPosition = cameraPosition;
            renderMessage.LastMomentUpdateIndex = lastMomentUpdateIndex;
            renderMessage.ProjectionOffsetX = 0;
            renderMessage.ProjectionOffsetY = 0;
            renderMessage.Smooth = false;
            MyRender11.SetupCameraMatrices(renderMessage);
        }

        public static void SetTarget(IMyTargetingCapableBlock controlledBlock, MyEntity target)
        {
            if (controlledBlock == _cockpit)
            {
                _targetEntity = target;
            }
        }
    }
}