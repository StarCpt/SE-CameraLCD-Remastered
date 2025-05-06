using System;
using System.Collections.Generic;
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

using CameraLCD;
using CameraLCD.Patches;
using VRageRenderAccessor.VRage.Render11.Common;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;
using VRageRenderAccessor.VRageRender;

namespace DeltaWing.TargetCamera
{
    public class TargetCamera
    {
        public static Vector2I Size = new Vector2I(640, 360);
        
        public static IMyPlayer LocalPlayer => MyAPIGateway.Session?.Player;
        public static MyCharacter PlayerCharacter => LocalPlayer?.Character as MyCharacter;
        public static IMyEntityController PlayerController => LocalPlayer?.Controller;

        public static MyEntity targetEntity;
        public static MyCockpit cockpit;

        public static string textureName = "TargetCamera";
        
        public static void Update()
        {
            // Step 1: Get the targeted entity and controlled grid
            
            var controlledEntity = PlayerController?.ControlledEntity;
            
            if (!(controlledEntity?.Entity is MyCockpit cockpit))
            {
                TargetCamera.cockpit = null;
                targetEntity = null;
                return;
            }

            TargetCamera.cockpit = cockpit;
            var targetData = cockpit.TargetData;
            targetEntity = targetData.TargetId is 0 || !targetData.IsTargetLocked ? null : MyEntities.GetEntityById(targetData.TargetId);
        }

        // TODO: Credit Lurking StarCpt for doing most of the heavy lifting with the camera API
        public static void Draw()
        {
            MyCamera renderCamera = MySector.MainCamera;

            if (targetEntity == null || cockpit == null || renderCamera == null) return;
            var controlledGrid = cockpit.CubeGrid;
            MyLog.Default.Log(MyLogSeverity.Info, cockpit.TargetData.TargetId.ToString());
            
            // MyEntities.TryGetEntityById(cockpit.TargetData.TargetId, out MyEntity targetEntity, true);
            
            MyLog.Default.Log(MyLogSeverity.Info, "Got controlled grid");
            MyLog.Default.Log(MyLogSeverity.Info, $"Controlled grid exists: {controlledGrid != null}");
            MyLog.Default.Log(MyLogSeverity.Info, $"Target entity exists: {targetEntity != null}");
            // Step 2: Break early if it doesn't exist
            if (controlledGrid == null) return;
            
            MyLog.Default.Log(MyLogSeverity.Info, "Got controlled grid and target entity");
            
            #region disble post-processing effects and lod changes

            bool ogLods = SetLoddingEnabled(false);
            bool ogDrawBillboards = MyRender11.Settings.DrawBillboards;
            MyRender11.Settings.DrawBillboards = false;
            MyRenderDebugOverrides debugOverrides = MyRender11.DebugOverrides;
            bool ogFlares = debugOverrides.Flares;
            bool ogSSAO = debugOverrides.SSAO;
            bool ogBloom = debugOverrides.Bloom;
            debugOverrides.Flares = false;
            debugOverrides.SSAO = false;
            debugOverrides.Bloom = false;

            #endregion
            
            // Step 3: Get target camera details (near clip, fov, cockpit up)
            float targetCameraFov = (float)GetFov(controlledGrid, targetEntity);
            float targetCameraNearPlane = (float)controlledGrid.PositionComp.WorldVolume.Radius;
            var targetCameraUp = cockpit.WorldMatrix.Up;
            var targetCameraPos = controlledGrid.PositionComp.WorldVolume.Center;

            // Step 4: Create a camera matrix from the current controlled grid, with a near clipping plane that excludes the current grid, pointed at the target, and FOV scaled

            var targetCameraViewMatrix = MatrixD.CreateLookAt(targetCameraPos, targetEntity.PositionComp.WorldVolume.Center, targetCameraUp);

            // Step 5: Move the game camera to that matrix, take a image snapshot, then move it back
            Vector2I ogResolutionI = MyRender11.ResolutionI;

            MyRender11.ViewportResolution = Size;
            MyRender11.ResolutionI = Size;
            SetCameraViewMatrix(targetCameraViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, targetCameraFov, targetCameraFov, targetCameraNearPlane, targetCameraPos, 1);

            // Draw the game to the screen
            var backbufferFormat = Patch_MyRender11.RenderTarget.Rtv.Description.Format;
            var borrowedRtv = MyManagers.RwTexturesPool.BorrowRtv(textureName, Size.X, Size.Y, backbufferFormat);
            
            MyRender11.DrawGameScene(borrowedRtv, out var debugAmbientOcclusion);
            debugAmbientOcclusion.Release();

            MyRender11.DeviceInstance.ImmediateContext1.CopySubresourceRegion(
                borrowedRtv.Resource, 0, null, 
                Patch_MyRender11.RenderTarget.Resource, 0, 
                ogResolutionI.X - Size.X, ogResolutionI.Y - Size.Y
                );

            borrowedRtv.Release();
            MyLog.Default.Log(MyLogSeverity.Info, "Took an image snapshot");

            // Restore camera position
            MyRender11.ViewportResolution = ogResolutionI;
            MyRender11.ResolutionI = ogResolutionI;
            SetCameraViewMatrix(renderCamera.ViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, renderCamera.FieldOfView, renderCamera.FieldOfView, renderCamera.NearPlaneDistance, renderCamera.Position, 0);
            MyLog.Default.Log(MyLogSeverity.Info, "Reset camera");

            #region restore post-processing and lod settings

            SetLoddingEnabled(ogLods);
            MyRender11.Settings.DrawBillboards = ogDrawBillboards;
            debugOverrides.Flares = ogFlares;
            debugOverrides.SSAO = ogSSAO;
            debugOverrides.Bloom = ogBloom;

            #endregion
        }

        public static double GetFov(MyCubeGrid controlledGrid, MyEntity targetEntity)
        {
            // Get world positions of both grid and target
            Vector3D from = controlledGrid.PositionComp.WorldVolume.Center;
            Vector3D to = targetEntity.PositionComp.WorldVolume.Center;

            // Get distance between them
            double distance = Vector3D.Distance(from, to);

            // Get radius of the target's bounding sphere
            double radius = targetEntity.PositionComp.WorldVolume.Radius;

            // If distance is too close, avoid division by zero
            if (distance <= 0)
                return MathHelper.PiOver2; // 90 degrees in radians as a fallback

            // Calculate angular FOV needed to contain the sphere (in radians)
            double fov = 2 * Math.Atan(radius / distance);

            return fov;
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
    }
}