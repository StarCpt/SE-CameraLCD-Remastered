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
using Sandbox;
using VRage.Input;
using VRage.Render.Scene;
using VRageRenderAccessor.VRage.Render11.Common;
using VRageRenderAccessor.VRage.Render11.Resources.Textures;
using VRageRenderAccessor.VRageRender;

namespace SETargetCamera
{
    // TODO: damage feedback, text indicator for zoom level azimuth and elevation, highlight ship outline maybe, disable input when in full screen, fix cursor flickering
    public class TargetCamera
    {
        private static Vector2 _pos = Plugin.Settings.Pos;
        private static Vector2I _desiredPos = Plugin.Settings.Pos;
        
        private static Vector2 _size = Plugin.Settings.Size; // TODO: Full screen cam
        private static Vector2I _desiredSize = Plugin.Settings.Size;


        
        private static IMyPlayer LocalPlayer => MyAPIGateway.Session?.Player;
        private static MyCharacter PlayerCharacter => LocalPlayer?.Character as MyCharacter;
        private static IMyEntityController PlayerController => LocalPlayer?.Controller;

        private static MyEntity _targetEntity;
        private static MyEntity _previousTargetEntity;
        private static MyShipController _cockpit;

        private const string TextureName = "TargetCamera";


        private static WcApi _wcApi;
        private static bool _usesWc;
        private static bool _wasJustInCockpit;

        private static bool _fullscreen;
        
        
        private static Vector3D _previousTargetPos;
        private static Vector3D _targetPos;
        private static float _fov;
        private static float _previousFov;
        
        
        private static Vector3D _shipOffset;
        private static Vector3D _targetOffset;


        private static bool _withinRange = false;
        
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

            WeaponCoreInterop.Initialize(); // Initializes our reflection hacks >:) API function missing? Fine. I'll do it myself.
        }

        public static void WorldUnload()
        {
            MyLog.Default.Log(MyLogSeverity.Info,"World unloaded, resetting WC API");
            WeaponCoreInterop.Invalidate();
            _usesWc = false;
        }


        public static void Update()
        {
            UpdateScreenSize();
            DrawTargetPos();
            
            if (!Plugin.Settings.Enabled) return;
            DisplayFrameTimer.Stopwatch.Restart();
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
            _previousTargetEntity = _targetEntity;
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
            
            if (_targetEntity == null)
            {
                _targetOffset = Vector3D.Zero;
                return;
            }

            if (_previousTargetEntity == null || _targetEntity == null || _previousTargetEntity != _targetEntity)
            {
                _easeLerpSpeed = 0;
                _easeLerpSpeed2 = 0;
            }
            
            _shipOffset = cockpit.CubeGrid.WorldMatrix.Translation - cockpit.CubeGrid.PositionComp.WorldAABB.Center;
            _targetOffset = _targetEntity.WorldMatrix.Translation - _targetEntity.PositionComp.WorldAABB.Center;



            if ((_targetEntity.PositionComp.WorldAABB.Center - cockpit.CubeGrid.PositionComp.WorldAABB.Center).Length() <
                Plugin.Settings.MinRange)
            {
                _withinRange = true;
                return;
            }
            

            _withinRange = false;
            // Creating the border

            float border = Plugin.Settings.BorderThickness;
            Color color = Plugin.Settings.BorderColor;

            var x = _pos.X;
            var y = _pos.Y;
            var width = _size.X;
            var height = _size.Y;
            
            
            
            // TODO: Extract function
            DrawRectangle(x, y, width, height, border, color);


            if (_targetEntity is MyCubeGrid grid)
            {
                //WeaponCoreInterop.UpdatePaintedTarget(grid.PositionComp.WorldAABB.Center, grid); // Doing something with this
            }
        }

        private static void DrawRectangle(float x, float y, float width, float height, float border, Color color)
        {
            RectangleF left = new RectangleF(x - border, y - border, border, height + border * 2 );
            RectangleF right = new RectangleF(x + width, y - border, border, height + border * 2);
            RectangleF bottom = new RectangleF(x - border, y - border, width + border * 2, border);
            RectangleF top = new RectangleF(x - border, y + height, width + border * 2, border);
            
            MyRenderProxy.DrawSprite("Textures\\GUI\\Blank.dds", ref left, null, color, 0, false, true);
            MyRenderProxy.DrawSprite("Textures\\GUI\\Blank.dds", ref right, null, color, 0, false, true);
            MyRenderProxy.DrawSprite("Textures\\GUI\\Blank.dds", ref bottom, null, color, 0, false, true);
            MyRenderProxy.DrawSprite("Textures\\GUI\\Blank.dds", ref top, null, color, 0, false, true);
        }

        private static void UpdateScreenSize()
        {
            bool keyHeld = MyAPIGateway.Input.IsKeyPress((MyKeys)Plugin.Settings.FullscreenKey);

            if (keyHeld)
            {
                _desiredPos = new Vector2I(25, 25);
                var screen = MyGuiManager.GetFullscreenRectangle();
                _desiredSize = new Vector2I(screen.Width - 50, screen.Height - 50);
                MySandboxGame.Static.SetMouseVisible(true);
                
                _fullscreen = true;
            }
            else
            {
                _desiredPos = Plugin.Settings.Pos;
                _desiredSize = Plugin.Settings.Size;
                _fullscreen = false;
            }
            
            _pos = Vector2.Lerp(_pos, _desiredPos, 0.5f);
            _size = Vector2.Lerp(_size, _desiredSize, 0.5f);


            if (_fullscreen && MyInput.Static.IsNewLeftMousePressed() && _usesWc)
            {
                Vector3D ray = Project2DToWorldDir(MyInput.Static.GetMousePosition(), _pos, _size, _targetCameraForward,
                    _targetCameraUp, _fov);


                List<IHitInfo> hitResult = new List<IHitInfo>();
                MyAPIGateway.Physics.CastRay(_virtualCameraPos, _virtualCameraPos + ray * 100000, hitResult);
                var colour = new Vector4(255, 255, 255, 255);
                //DebugDraw.DrawLine(_virtualCameraPos, _virtualCameraPos + ray * 100000, colour, 2, 60);
                
                
                // Sort all hits by distance from _virtualCameraPos
                var sortedHits = hitResult
                    .OrderBy(h => Vector3D.DistanceSquared(_virtualCameraPos, h.Position))
                    .ToList();

                // If there's at least one hit and it's on your target entity, update painted target
                if (sortedHits.Count > 0 && _cockpit != null && _cockpit.CubeGrid != null)
                {
                    var closestHit = sortedHits[0];
                    if (_targetEntity != null && closestHit.HitEntity is MyCubeGrid entity &&
                        entity.EntityId == _targetEntity.EntityId)
                    {
                        WeaponCoreInterop.UpdatePaintedTarget(closestHit.Position, entity);
                    }
                    else
                    {
                        _wcApi.SetAiFocus(_cockpit.CubeGrid, (MyEntity)closestHit.HitEntity);
                    }
                }
            }
        }
        


        private static double _easeLerpSpeed = 0;
        private static double _easeLerpSpeed2 = 0;
        private static Vector3D _virtualCameraPos;
        private static MatrixD _targetCameraViewMatrix;
        private static Vector3D _targetCameraForward;
        private static Vector3D _targetCameraUp;

        public static void Draw()
        {
            
            DebugDraw.Draw();
            
            
            if (!Plugin.Settings.Enabled || _withinRange) return;
            bool? ogLods = null;
            bool? ogDrawBillboards = null;
            bool? ogFlares = null;
            bool? ogSSAO = null;
            bool? ogBloom = null;
            Vector2I? ogResolutionI = null;
            MyRenderDebugOverrides debugOverrides = null;
            try
            {
                MyRender11.Settings.SkipGlobalROWMUpdate = true;
                
                var simSpeed = MySandboxGame.SimulationRatio;
                var simTimeMs = DisplayFrameTimer.TimeSinceUpdateMs;
                var maxSimTime = simSpeed * 16.6666666666;
                var t = simTimeMs / maxSimTime;
                
                
                MyCamera renderCamera = MySector.MainCamera;

                if (_targetEntity == null || _cockpit == null || renderCamera == null)
                {
                    return;
                }
                var controlledGrid = _cockpit.CubeGrid;
                
                // MyEntities.TryGetEntityById(cockpit.TargetData.TargetId, out MyEntity targetEntity, true);
                
                // Step 2: Break early if it doesn't exist
                if (controlledGrid == null) return;
                
                #region disble post-processing effects and lod changes

                ogLods = SetLoddingEnabled(false);
                ogDrawBillboards = MyRender11.Settings.DrawBillboards;
                MyRender11.Settings.DrawBillboards = true;
                debugOverrides = MyRender11.DebugOverrides;
                ogFlares = debugOverrides.Flares;
                ogSSAO = debugOverrides.SSAO;
                ogBloom = debugOverrides.Bloom;
                debugOverrides.Flares = true;
                debugOverrides.SSAO = false;
                debugOverrides.Bloom = false;
                float ogFarPLane = renderCamera.FarPlaneDistance;
                #endregion
                
                // Step 3: Get target camera details (near clip, fov, cockpit up)
                
                float targetCameraNearPlane = 5; // Can probably get rid of this
                _targetCameraUp = _cockpit.WorldMatrix.Up;
                
                var shipPos = GetRenderWorldMatrix(_cockpit.CubeGrid).Translation - _shipOffset;
                
                _previousTargetPos = _targetPos;
                var realTargetPos = GetRenderWorldMatrix(_targetEntity).Translation - _targetOffset;
                var lerpFactor = 1 / Plugin.Settings.CameraSmoothing;

                _easeLerpSpeed += lerpFactor;
                _easeLerpSpeed = Math.Min(_easeLerpSpeed, 1);
                _easeLerpSpeed2 += _easeLerpSpeed;
                _easeLerpSpeed2 = Math.Min(_easeLerpSpeed2, 1);
                
                _targetPos = Vector3D.Lerp(_previousTargetPos, realTargetPos, _easeLerpSpeed2);
                
                _targetCameraForward = _targetPos - shipPos;
                var dist = _targetCameraForward.Length();
                var dir = _targetCameraForward / dist;
                
                
                _fov = (float)GetFov(shipPos, _targetCameraForward, dir, _targetEntity);


                _fov = (float)MathHelper.Lerp(_previousFov, _fov, _easeLerpSpeed);
                _previousFov = _fov;
                
                
                _virtualCameraPos = shipPos + dir * controlledGrid.PositionComp.WorldVolume.Radius;

                // Step 4: Create a camera matrix from the current controlled grid, with a near clipping plane that excludes the current grid, pointed at the target, and FOV scaled

                _targetCameraViewMatrix = MatrixD.CreateLookAt(_virtualCameraPos, _targetPos, _targetCameraUp);

                // Step 5: Move the game camera to that matrix, take a image snapshot, then move it back
                ogResolutionI = MyRender11.ResolutionI;

                Vector2I size = (Vector2I)_size;
                
                MyRender11.ViewportResolution = size;
                MyRender11.ResolutionI = size;
                SetCameraViewMatrix(_targetCameraViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, _fov, _fov, targetCameraNearPlane, (float)dist * 2, _virtualCameraPos, 1);

                // Draw the game to the screen
                var backbufferFormat = Patch_MyRender11.RenderTarget.Rtv.Description.Format;
                var borrowedRtv = MyManagers.RwTexturesPool.BorrowRtv(TextureName, size.X, size.Y, backbufferFormat);
                
                MyRender11.DrawGameScene(borrowedRtv, out var debugAmbientOcclusion);
                
                debugAmbientOcclusion.Release();
                
                
                // Placing the actual image onto the screen
                MyRender11.DeviceInstance.ImmediateContext1.CopySubresourceRegion(
                    borrowedRtv.Resource, 0, null, 
                    Patch_MyRender11.RenderTarget.Resource, 0, 
                    (int)_pos.X, (int)_pos.Y
                    );
                borrowedRtv.Release();

                // Restore camera position
                MyRender11.ViewportResolution = (Vector2I)ogResolutionI;
                MyRender11.ResolutionI = (Vector2I)ogResolutionI;
                SetCameraViewMatrix(renderCamera.ViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, renderCamera.FieldOfView, renderCamera.FieldOfView, renderCamera.NearPlaneDistance, renderCamera.FarPlaneDistance, renderCamera.Position, 0);

                #region restore post-processing and lod settings

                SetLoddingEnabled((bool)ogLods);
                MyRender11.Settings.DrawBillboards = (bool)ogDrawBillboards;
                debugOverrides.Flares = (bool)ogFlares;
                debugOverrides.SSAO = (bool)ogSSAO;
                debugOverrides.Bloom = (bool)ogBloom;

                #endregion
            }
            catch (Exception ex)
            {
                MyLog.Default.Log(MyLogSeverity.Critical, ex.ToString());

                if (debugOverrides != null)
                {
                    if (ogLods != null) SetLoddingEnabled((bool)ogLods);
                    if (ogDrawBillboards != null) MyRender11.Settings.DrawBillboards = (bool)ogDrawBillboards;
                    if (ogFlares != null) debugOverrides.Flares = (bool)ogFlares;
                    if (ogSSAO != null) debugOverrides.SSAO = (bool)ogSSAO;
                    if (ogBloom != null) debugOverrides.Bloom = (bool)ogBloom;
                    if (ogResolutionI != null)
                    {
                        MyRender11.ResolutionI = (Vector2I)ogResolutionI;
                        MyRender11.ViewportResolution = (Vector2I)ogResolutionI;
                    }
                }
               
            }
            
        }

        private static void DrawTargetPos()
        {
            if (!_usesWc) return;
            var posMaybe = WeaponCoreInterop.GetPaintedTargetLocalPosition();
            if (posMaybe.HasValue && _targetEntity != null && _targetEntity.Physics != null)
            {
                
                var pos = posMaybe.Value;
                var worldPos = Vector3D.Transform(pos, _targetEntity.WorldMatrix) + _targetEntity.Physics.LinearVelocity / 60;
                MatrixD worldMatrix = MatrixD.CreateTranslation(worldPos);
                Color color = Plugin.Settings.BorderColor;
                float radius = 2f;
                float lineThickness = 0.2f;
                int divisions = 16;
                MySimpleObjectDraw.DrawTransparentSphere(
                    ref worldMatrix,
                    radius,
                    ref color,
                    MySimpleObjectRasterizer.Solid,
                    divisions,
                    MyStringId.GetOrCompute("Debug"),
                    MyStringId.GetOrCompute("Debug"),
                    lineThickness,
                    customViewProjectionMatrix: -1,
                    persistentBillboards: null,
                    blendType: MyBillboard.BlendTypeEnum.AdditiveTop,
                    intensity: 3f
                );
                
            }
        }
        

        private static BoundingBoxD GetRenderWorldAABB(MyEntity entity)
        {
            var renderObjectIds = entity.Render.RenderObjectIDs;
            if (renderObjectIds != null && renderObjectIds.Length > 0)
            {
                uint id = renderObjectIds[0];
                var actor = MyIDTracker<MyActor>.FindByID(id);
                if (actor != null)
                {
                    return actor.WorldAabb;
                }
            }
            return entity.PositionComp.WorldAABB;
            
        }

        private static MatrixD GetRenderWorldMatrix(MyEntity entity)
        {
            var renderObjectIds = entity.Render.RenderObjectIDs;
            if (renderObjectIds != null && renderObjectIds.Length > 0)
            {
                uint id = renderObjectIds[0];
                var actor = MyIDTracker<MyActor>.FindByID(id);
                if (actor != null)
                {
                    return actor.WorldMatrix;
                }
            }
            return entity.WorldMatrix;
        }
        

        private static BoundingBoxD GetRenderLocalAABB(MyEntity entity)
        {
            var renderObjectIds = entity.Render.RenderObjectIDs;
            if (renderObjectIds != null && renderObjectIds.Length > 0)
            {
                uint id = renderObjectIds[0];
                var actor = MyIDTracker<MyActor>.FindByID(id);
                if (actor != null && actor.HasLocalAabb)
                {
                    return actor.LocalAabb;
                }
            }
            return entity.PositionComp.LocalAABB;
        }
        

        public static double GetFov(Vector3D from, Vector3D to, Vector3D dir, MyEntity targetEntity)
        {
            // 1) Setup camera

            // 2) Pull the local AABB and its world matrix
            var localAabb    = targetEntity.PositionComp.LocalAABB;  // in object space
            MatrixD worldMat = GetRenderWorldMatrix(targetEntity);
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
                var dist = v.Length();
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

        private static void SetCameraViewMatrix(MatrixD viewMatrix, Matrix projectionMatrix, Matrix projectionFarMatrix, float fov, float fovSkybox, float nearPlane, float farPlane, Vector3D cameraPosition, int lastMomentUpdateIndex)
        {
            MyCamera renderCamera = MySector.MainCamera;
            MyRenderMessageSetCameraViewMatrix renderMessage = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);
            renderMessage.ViewMatrix = viewMatrix;
            renderMessage.ProjectionMatrix = projectionMatrix;
            renderMessage.ProjectionFarMatrix = projectionFarMatrix;
            renderMessage.FOV = fov;
            renderMessage.FOVForSkybox = fovSkybox;
            renderMessage.NearPlane = nearPlane;
            renderMessage.FarPlane = farPlane;
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
            if (_usesWc) return;
            if (controlledBlock == _cockpit)
            {
                _targetEntity = target;
            }
        }
        
        
        public static Vector3 Project2DToWorldDir(
            Vector2 pos2D, Vector2 displayPos, Vector2 displaySize,
            Vector3 camForward, Vector3 camUp, float fovY)
        {
            // 1) Compute local X,Y in display
            Vector2 localPos = pos2D - displayPos;
            Vector2 norm = new Vector2(
                localPos.X / displaySize.X,
                localPos.Y / displaySize.Y
            );

            // 2) To NDC [-1,1]
            Vector2 ndc = new Vector2(
                norm.X * 2f - 1f,
                1f - norm.Y * 2f   // flip Y
            );

            // 3) Build orthonormal basis
            Vector3 forward = Vector3.Normalize(camForward);
            Vector3 right   = Vector3.Normalize(Vector3.Cross(forward, camUp));
            Vector3 up      = Vector3.Normalize(Vector3.Cross(right, forward));

            // 4) Compute camera-space offsets
            float aspect = displaySize.X / displaySize.Y;
            float tanFov = (float)Math.Tan(fovY * 0.5f);

            Vector3 offset =
                right * (ndc.X * aspect * tanFov) +
                up    * (ndc.Y *            tanFov) +
                forward;                      // z = +1

            // 5) Normalize to get direction
            return Vector3.Normalize(offset);
        }
    }
}