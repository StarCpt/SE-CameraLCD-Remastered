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


        private static byte[] buffer = Array.Empty<byte>();

        public static string textureName = "TargetCamera";
        
        static Vector2 UVOffset = new Vector2(0);
        static MyBillboard Billboard = new MyBillboard()
        {
            Material = MyStringId.GetOrCompute(textureName),
            Color = Color.White,
            UVSize = new Vector2(0.5f, 1), // important
            BlendType = MyBillboard.BlendTypeEnum.Standard,
            DistanceSquared = 1f,
            LocalType = MyBillboard.LocalTypeEnum.Custom,
            Reflectivity = 0f,
            ColorIntensity = 1f,
            AlphaCutout = 0f,
            CustomViewProjection = -1,
            ParentID = uint.MaxValue,
            SoftParticleDistanceScale = 0f,
        };
        
        
        public static void Update()
        {
            // Step 1: Get the targeted entity and controlled grid
            
            var controlledEntity = PlayerController?.ControlledEntity;
            
            if (!(controlledEntity?.Entity is MyCockpit cockpit))
            {
                return;
            }

            TargetCamera.cockpit = cockpit;
            targetEntity = MyEntities.GetEntityById(cockpit.TargetData.TargetId);
        }
        // TODO: Credit Lurking StarCpt for doing most of the heavy lifting with the camera API
        public static void Draw()
        {
            if (targetEntity == null || cockpit == null) return;
            var controlledGrid = cockpit.CubeGrid;
            MyLog.Default.Log(MyLogSeverity.Info, cockpit.TargetData.TargetId.ToString());
            
            // MyEntities.TryGetEntityById(cockpit.TargetData.TargetId, out MyEntity targetEntity, true);
            
            MyLog.Default.Log(MyLogSeverity.Info, "Got controlled grid");
            MyLog.Default.Log(MyLogSeverity.Info, $"Controlled grid exists: {controlledGrid != null}");
            MyLog.Default.Log(MyLogSeverity.Info, $"Target entity exists: {targetEntity != null}");
            // Step 2: Break early if it doesn't exist
            if (controlledGrid == null || targetEntity == null) return;
            
            MyLog.Default.Log(MyLogSeverity.Info, "Got controlled grid and target entity");
            
            
            // Step 3: Get target camera details (near clip, fov, cockpit up)
            float fov = (float)GetFov(controlledGrid, targetEntity);
            fov = 90; //TODO: Temp, remove this
            float nearClip = (float)controlledGrid.PositionComp.WorldVolume.Radius;
            var up = cockpit.WorldMatrix.Up;
            
            MyLog.Default.Log(MyLogSeverity.Info, "Got param values");

            // Step 4: Create a camera matrix from the current controlled grid, with a near clipping plane that excludes the current grid, pointed at the target, and FOV scaled

            var cameraViewMatrix = GetPointAtTarget(controlledGrid, targetEntity, up);
            
            MyLog.Default.Log(MyLogSeverity.Info, "Got point at");

            // Step 5: Move the game camera to that matrix, take a image snapshot, then move it back
            
            MyCamera renderCamera = MySector.MainCamera;
            MyLog.Default.Log(MyLogSeverity.Info, "Got main camera");
            SetCameraViewMatrix(cameraViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, fov, fov, nearClip, renderCamera.FarPlaneDistance, renderCamera.FarFarPlaneDistance, controlledGrid.PositionComp.WorldVolume.Center, lastMomentUpdateIndex: 1, smooth: false);


            Vector2I ogResolutionI = MyRender11.ResolutionI;
                
            // Draw the game to the screen
            var backbufferFormat = Patch_MyRender11.RenderTarget.Rtv.Description.Format;
            var borrowedRtv = MyManagers.RwTexturesPool.BorrowRtv(textureName, Size.X, Size.Y, backbufferFormat);
            
            MyRender11.DrawGameScene(borrowedRtv, out var debugAmbientOcclusion);
            
            MyRender11.DeviceInstance.ImmediateContext1.CopySubresourceRegion(
                borrowedRtv.Resource, 0, null, 
                Patch_MyRender11.RenderTarget.Resource, 0, 
                ogResolutionI.X - Size.X, ogResolutionI.Y - Size.Y
                );
                

            
            
            borrowedRtv.Release();
            MyLog.Default.Log(MyLogSeverity.Info, "Took an image snapshot");
            // Restore camera position
            SetCameraViewMatrix(renderCamera.ViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, renderCamera.FieldOfView, renderCamera.FieldOfView, renderCamera.NearPlaneDistance, renderCamera.FarPlaneDistance, renderCamera.FarFarPlaneDistance, renderCamera.Position, lastMomentUpdateIndex: 0, smooth: false);

            MyLog.Default.Log(MyLogSeverity.Info, "Reset camera");
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
        
        
        public static MatrixD GetPointAtTarget(MyCubeGrid controlledGrid, MyEntity targetEntity, Vector3D up)
        {
            Vector3D camPos = controlledGrid.PositionComp.WorldVolume.Center;
            Vector3D targetPos = targetEntity.PositionComp.WorldVolume.Center;
            
            
            MatrixD viewMatrix = GetLookAtMatrix(camPos, targetPos, up);

            viewMatrix.Forward = -viewMatrix.Forward;
            MyLog.Default.Log(MyLogSeverity.Info, $"Fromto: {(targetPos - camPos).Normalized()} , Matrix: {viewMatrix.Forward}, dot: {(targetPos - camPos).Normalized().Dot(viewMatrix.Forward)}");

            return viewMatrix;
        }
        
        
        public static MatrixD GetLookAtMatrix(Vector3D fromPos, Vector3D toPos, Vector3D upHint)
        {
            // In Keen, Forward is -Z, so we point -Z toward the target
            Vector3D forward = Vector3D.Normalize(toPos - fromPos);

            // Protect against up being parallel to forward
            if (Vector3D.IsZero(Vector3D.Cross(upHint, forward)))
                upHint = Vector3D.CalculatePerpendicularVector(forward);

            Vector3D right = Vector3D.Normalize(Vector3D.Cross(upHint, forward));
            Vector3D up = Vector3D.Normalize(Vector3D.Cross(forward, right));

            // Now — here's the matrix in Keen's axis layout:
            MatrixD matrix = new MatrixD
            {
                M11 = right.X, M12 = right.Y, M13 = right.Z, M14 = 0,
                M21 = up.X,    M22 = up.Y,    M23 = up.Z,    M24 = 0,
                M31 = forward.X, M32 = forward.Y, M33 = forward.Z, M34 = 0,
                M41 = fromPos.X, M42 = fromPos.Y, M43 = fromPos.Z, M44 = 1
            };

            return matrix;
        }
        
        
        
        
        private static void SetCameraViewMatrix(MatrixD viewMatrix, Matrix projectionMatrix, Matrix projectionFarMatrix, float fov, float fovSkybox, float nearPlane, float farPlane, float farFarPlane, Vector3D cameraPosition, float projectionOffsetX = 0f, float projectionOffsetY = 0f, int lastMomentUpdateIndex = 1, bool smooth = true)
        {
            MyRenderMessageSetCameraViewMatrix renderMessage = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);
            renderMessage.ViewMatrix = viewMatrix;
            renderMessage.ProjectionMatrix = projectionMatrix;
            renderMessage.ProjectionFarMatrix = projectionFarMatrix;
            renderMessage.FOV = fov;
            renderMessage.FOVForSkybox = fovSkybox;
            renderMessage.NearPlane = nearPlane;
            renderMessage.FarPlane = farPlane;
            renderMessage.FarFarPlane = farFarPlane;
            renderMessage.CameraPosition = cameraPosition;
            renderMessage.LastMomentUpdateIndex = lastMomentUpdateIndex;
            renderMessage.ProjectionOffsetX = projectionOffsetX;
            renderMessage.ProjectionOffsetY = projectionOffsetY;
            renderMessage.Smooth = smooth;
            MyRender11.SetupCameraMatrices(renderMessage);
        }
        
        
        
    }
}