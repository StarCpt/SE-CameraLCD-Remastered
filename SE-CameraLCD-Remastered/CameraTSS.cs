using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.Utils;
using VRage.Render.Scene;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;
using VRageRender.Import;
using VRageRender.Messages;

namespace CameraLCD
{
    // different ID to avoid conflicting with the original plugin
    [MyTextSurfaceScript(SCRIPT_ID, "Camera Display")]
    public class CameraTSS : MyTSSCommon
    {
        public const string SCRIPT_ID = "TSS_CameraDisplay_2";

        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;
        public DisplayId Id { get; }
        public bool IsActive { get; private set; } = false;

        private readonly MyTerminalBlock _lcd;
        private readonly MyTextPanelComponent _lcdComponent;
        private readonly int _surfaceId;

        private string _customData;
        private MyCameraBlock _camera;

        public CameraTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _lcd = (MyTerminalBlock)block;
            _lcdComponent = (MyTextPanelComponent)surface;
            _surfaceId = GetSurfaceId(_lcd, _lcdComponent);
            Id = new DisplayId(_lcd.EntityId, _lcdComponent.Area);

            _lcd.CustomDataChanged += _ => UpdateSettings(); // doesn't work if the change occurred locally
            _lcd.IsWorkingChanged += _ => UpdateIsActive();
            _lcd.CubeGridChanged += _ => CubeGridChanged();
            _lcd.OnMarkForClose += Lcd_OnMarkForClose;
            UpdateSettings();
        }

        private static int GetSurfaceId(MyTerminalBlock block, MyTextPanelComponent surface)
        {
            if (block is MyTextPanel)
            {
                return 0;
            }
            else
            {
                return surface.Area;
            }
        }

        public override void Run()
        {
            base.Run();

            if (_lcdComponent.Script != SCRIPT_ID)
                return;

            bool customDataChanged = _customData != _lcd.CustomData;
            if (_camera == null || customDataChanged)
            {
                UpdateSettings();
            }
        }

        private void RegisterCamera(MyCameraBlock camera)
        {
            UnregisterCamera();

            _camera = camera;
            _camera.OnClose += Camera_OnClose;
            _camera.IsWorkingChanged += _ => UpdateIsActive();
            _camera.CubeGridChanged += _ => CubeGridChanged();
            _camera.CustomNameChanged += _ => UpdateSettings();
            UpdateIsActive();
            CameraLcdManager.AddDisplay(Id, this);
        }

        private void UnregisterCamera()
        {
            if (_camera != null)
            {
                CameraLcdManager.RemoveDisplay(Id);
                IsActive = false;
                _camera.OnClose -= Camera_OnClose;
                _camera.IsWorkingChanged -= _ => UpdateIsActive();
                _camera.CubeGridChanged -= _ => CubeGridChanged();
                _camera.CustomNameChanged -= _ => UpdateSettings();
                _camera = null;
                UpdateIsActive();
            }
        }

        private void Camera_OnClose(MyEntity obj) => UnregisterCamera();

        private void CubeGridChanged()
        {
            if (_camera != null && !_camera.CubeGrid.IsSameConstructAs(_lcd.CubeGrid))
            {
                UnregisterCamera();
            }
        }

        private void UpdateIsActive()
        {
            IsActive = _camera != null && _camera.IsWorking && _lcd.IsWorking;
        }

        private void UpdateSettings()
        {
            _customData = _lcd.CustomData;

            if (!TryFindCamera(_customData, out MyCameraBlock newCamera))
            {
                UnregisterCamera();
                return;
            }

            if (_camera == newCamera)
            {
                return;
            }
            
            if (_camera is not null)
            {
                // unregister current camera if changed (and not null)
                UnregisterCamera();
            }

            // is new or changed
            RegisterCamera(newCamera);
        }

        public bool TryFindCamera(string customData, out MyCameraBlock camera)
        {
            camera = null;

            if (string.IsNullOrWhiteSpace(customData))
            {
                return false;
            }

            List<MyCameraBlock> gridCameras = _lcd.CubeGrid.GridSystems.CameraSystem.m_cameras;
            string cameraName = GetCameraName(customData);
            if (!string.IsNullOrWhiteSpace(cameraName))
            {
                camera = TryFindMatch(gridCameras, cameraName);
                return camera != null;
            }
            else // brute force search
            {
                using StringReader sr = new StringReader(customData);
                while (sr.ReadLine() is string line)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    camera = TryFindMatch(gridCameras, line);
                    if (camera != null)
                    {
                        return true;
                    }
                }
            }
            return false;

            static MyCameraBlock? TryFindMatch(List<MyCameraBlock> cameras, string customName)
            {
                foreach (var cameraBlock in cameras)
                {
                    if (cameraBlock.CustomName.EqualsStrFast(customName))
                    {
                        return cameraBlock;
                    }
                }
                return null;
            }
        }

        private string GetCameraName(string customData)
        {
            if (String.IsNullOrWhiteSpace(customData))
                return null;

            string prefix = _surfaceId + ":";
            using (StringReader reader = new StringReader(customData))
            {
                while (reader.ReadLine() is string line)
                {
                    if (line.StartsWith(prefix) && line.Length > prefix.Length)
                    {
                        string name = line.Substring(prefix.Length);
                        if (!string.IsNullOrWhiteSpace(name))
                            return name;
                    }
                }
            }
            return null;
        }

        private struct RendererState
        {
            public bool Lodding;
            public bool DrawBillboards;
            public bool EyeAdaption;
            public bool Flares;
            public bool SSAO;
            public bool Bloom;
            public bool ShadowCameraFrozen;
            public Vector2I ViewportResolution;
            public Vector2I ResolutionI;

            private static readonly RendererState _cameraViewState = new()
            {
                Lodding = false,
                DrawBillboards = true,
                EyeAdaption = true, // when turned off, makes the image too bright when surface is lit by sunlight
                Flares = false,
                SSAO = false,
                Bloom = false,
                ShadowCameraFrozen = true, // don't update shadow camera as it causes flickering for distant shadows
                //ViewportResolution = ,
                //ResolutionI = ,
            };

            public static RendererState GetCameraViewState(Vector2I surfaceResolution)
            {
                return _cameraViewState with
                {
                    ViewportResolution = surfaceResolution,
                    ResolutionI = surfaceResolution,
                };
            }
        }

        private struct CameraState
        {
            public MatrixD ViewMatrix;
            public MatrixD ProjMatrix;
            public MatrixD ProjFarMatrix;
            public float Fov;
            public float NearPlane;
            public float FarPlane;
            public float ProjOffsetX;
            public float ProjOffsetY;
            public Vector3D CameraPos;

            public static CameraState From(MyEnvironmentMatrices matrices)
            {
                return new CameraState
                {
                    ViewMatrix = matrices.ViewD,
                    ProjMatrix = matrices.OriginalProjection,
                    ProjFarMatrix = matrices.OriginalProjectionFar,
                    Fov = matrices.FovH,
                    NearPlane = matrices.NearClipping,
                    FarPlane = matrices.FarClipping,
                    ProjOffsetX = matrices.Projection.M31,
                    ProjOffsetY = matrices.Projection.M32,
                    CameraPos = matrices.CameraPosition,
                };
            }
        }

        public bool Draw()
        {
            if (!IsActive || !_lcdComponent.m_textureGenerated || _lcdComponent.ContentType != ContentType.SCRIPT || _lcdComponent.Script != SCRIPT_ID)
                return false;

            MyCamera renderCamera = MySector.MainCamera;
            if (renderCamera is null || renderCamera.GetDistanceFromPoint(_lcd.WorldMatrix.Translation) > Plugin.Settings.Range)
                return false;

            // frustum test
            if (MyRender11.Environment.Matrices.ViewFrustumClippedD.Contains(_lcd.PositionComp.WorldAABB) is ContainmentType.Disjoint)
                return false;

            if (!TryGetRenderTexture(out IUserGeneratedTexture surfaceRtv))
                return false;

            var originalRendererState = new RendererState
            {
                Lodding = MyCommon.LoddingSettings.Global.IsUpdateEnabled,
                DrawBillboards = MyRender11.Settings.DrawBillboards,
                EyeAdaption = MyRender11.Postprocess.EnableEyeAdaptation,
                Flares = MyRender11.DebugOverrides.Flares,
                SSAO = MyRender11.DebugOverrides.SSAO,
                Bloom = MyRender11.DebugOverrides.Bloom,
                ShadowCameraFrozen = MyRender11.Settings.ShadowCameraFrozen,
                ViewportResolution = MyRender11.ViewportResolution,
                ResolutionI = MyRender11.ResolutionI,
            };

            var originalCameraState = CameraState.From(MyRender11.Environment.Matrices);

            {
                // set state for CameraLCD rendering
                SetRendererState(RendererState.GetCameraViewState(surfaceRtv.Size));
                GetCameraViewMatrixAndPosition(_camera, out MatrixD cameraViewMatrix, out Vector3D cameraPos);
                SetCameraViewMatrix(originalCameraState with
                {
                    ViewMatrix = cameraViewMatrix,
                    Fov = _camera.GetFov(),
                    NearPlane = renderCamera.NearPlaneDistance,
                    FarPlane = renderCamera.FarPlaneDistance,
                    CameraPos = cameraPos,
                    ProjOffsetX = 0,
                    ProjOffsetY = 0,
                }, renderCamera.FarFarPlaneDistance, 1, false);

                CameraViewRenderer.Draw(surfaceRtv);

                // restore camera settings
                SetRendererState(originalRendererState);
                SetCameraViewMatrix(originalCameraState, renderCamera.FarFarPlaneDistance, 0, false);
            }

            MyRender11.RC.GenerateMips(surfaceRtv);

            return true;
        }

        private static void SetRendererState(RendererState state)
        {
            SetLoddingEnabled(state.Lodding);
            MyRender11.Settings.DrawBillboards = state.DrawBillboards;
            MyRender11.Postprocess.EnableEyeAdaptation = state.EyeAdaption;
            MyRender11.DebugOverrides.Flares = state.Flares;
            MyRender11.DebugOverrides.SSAO = state.SSAO;
            MyRender11.DebugOverrides.Bloom = state.Bloom;
            MyRender11.Settings.ShadowCameraFrozen = state.ShadowCameraFrozen;

            MyRender11.ViewportResolution = state.ViewportResolution;
            MyRender11.m_resolution = state.ResolutionI;

            static bool SetLoddingEnabled(bool enabled)
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
        }

        private static void SetCameraViewMatrix(CameraState state, float farFarPlane, int lastMomentUpdateIndex, bool smooth)
        {
            MyRenderMessageSetCameraViewMatrix renderMessage = null;
            try
            {
                renderMessage = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);
                renderMessage.ViewMatrix = state.ViewMatrix;
                renderMessage.ProjectionMatrix = state.ProjMatrix;
                renderMessage.ProjectionFarMatrix = state.ProjFarMatrix;
                renderMessage.FOV = state.Fov;
                renderMessage.FOVForSkybox = state.Fov;
                renderMessage.NearPlane = state.NearPlane;
                renderMessage.FarPlane = state.FarPlane;
                renderMessage.FarFarPlane = farFarPlane;
                renderMessage.CameraPosition = state.CameraPos;
                renderMessage.LastMomentUpdateIndex = lastMomentUpdateIndex;
                renderMessage.ProjectionOffsetX = state.ProjOffsetX;
                renderMessage.ProjectionOffsetY = state.ProjOffsetY;
                renderMessage.Smooth = smooth;
                MyRender11.SetupCameraMatrices(renderMessage);
            }
            finally
            {
                renderMessage?.Dispose();
            }
        }

        private static void GetCameraViewMatrixAndPosition(MyCameraBlock camera, out MatrixD viewMatrix, out Vector3D position)
        {
            // same as MyCameraBlock.GetViewMatrix() but using a custom matrix

            // use camera's render object matrix (if available) since the entity's simulation matrix may be desynced
            MatrixD matrix = TryGetActor(camera, out MyActor actor) ? actor.WorldMatrix : camera.WorldMatrix;
            matrix.Translation += matrix.Forward * 0.2;
            
            if (camera.Model.Dummies != null)
            {
                foreach (KeyValuePair<string, MyModelDummy> dummy in camera.Model.Dummies)
                {
                    if (dummy.Value.Name == "camera")
                    {
                        Quaternion rotation = Quaternion.CreateFromForwardUp(matrix.Forward, matrix.Up);
                        matrix.Translation += MatrixD.Transform(dummy.Value.Matrix, rotation).Translation;
                        break;
                    }
                }
            }

            position = matrix.Translation;
            MatrixD.Invert(ref matrix, out viewMatrix);
        }

        private static bool TryGetActor(MyEntity entity, out MyActor actor)
        {
            actor = null;
            if (entity?.Render is not MyRenderComponentBase renderComp)
            {
                return false;
            }

            try
            {
                uint actorId = renderComp.GetRenderObjectID();
                actor = actorId != uint.MaxValue ? MyIDTracker<MyActor>.FindByID(actorId) : null;
                return actor != null;
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetRenderTexture(out IUserGeneratedTexture texture)
        {
            string name;
            try
            {
                name = _lcdComponent.GetRenderTextureName();
            }
            catch (NullReferenceException)
            {
                texture = null;
                return false;
            }

            return MyManagers.FileTextures.TryGetTexture(name, out texture) && texture != null;
        }

        private void Lcd_OnMarkForClose(MyEntity obj) => Dispose();

        public override void Dispose()
        {
            base.Dispose();

            UnregisterCamera();
            _lcd.CustomDataChanged -= _ => UpdateSettings();
            _lcd.IsWorkingChanged -= _ => UpdateIsActive();
            _lcd.CubeGridChanged -= _ => CubeGridChanged();
            _lcd.OnMarkForClose -= Lcd_OnMarkForClose;
        }
    }
}
