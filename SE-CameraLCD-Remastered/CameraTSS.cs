using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using System;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.Utils;
using VRage.Render.Scene;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace CameraLCD
{
    // different ID to avoid conflicting with the original plugin
    [MyTextSurfaceScript("TSS_CameraDisplay_2", "Camera Display")]
    public class CameraTSS : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;
        public DisplayId Id { get; }
        public bool IsActive { get; private set; } = false;

        private readonly MyTerminalBlock _lcd;
        private readonly MyTextPanelComponent _lcdComponent;

        private MyCameraBlock _camera;

        public CameraTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _lcd = (MyTerminalBlock)block;
            _lcdComponent = (MyTextPanelComponent)surface;
            Id = new DisplayId(_lcd.EntityId, _lcdComponent.Area);

            _lcd.CustomDataChanged += OnCustomDataChanged;
            _lcd.IsWorkingChanged += _ => UpdateIsActive();
            _lcd.CubeGridChanged += _ => CubeGridChanged();
            _lcd.OnMarkForClose += Lcd_OnMarkForClose;
            OnCustomDataChanged(_lcd);
        }

        public override void Run()
        {
            base.Run();

            if (_camera == null)
            {
                OnCustomDataChanged(_lcd);
            }
        }

        private void RegisterCamera(MyCameraBlock camera)
        {
            UnregisterCamera();

            _camera = camera;
            _camera.OnClose += Camera_OnClose;
            _camera.IsWorkingChanged += _ => UpdateIsActive();
            _camera.CubeGridChanged += _ => CubeGridChanged();
            _camera.CustomNameChanged += _ => OnCustomDataChanged(_lcd);
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
                _camera.CustomNameChanged -= _ => OnCustomDataChanged(_lcd);
                _camera = null;
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
            IsActive = _camera != null && _lcd.IsWorking && _camera.IsWorking;
        }

        private void OnCustomDataChanged(MyTerminalBlock lcd)
        {
            string cameraBlockName = GetCameraName(lcd.CustomData);
            if (String.IsNullOrWhiteSpace(cameraBlockName))
            {
                UnregisterCamera();
                return;
            }

            if (_camera != null && _camera.CustomName.EqualsStrFast(cameraBlockName))
            {
                return;
            }

            MyCameraBlock camera = (MyCameraBlock)lcd.CubeGrid.GridSystems.TerminalSystem.Blocks.FirstOrDefault(i => i is MyCameraBlock && i.CustomName.EqualsStrFast(cameraBlockName));
            
            if (camera != null)
            {
                RegisterCamera(camera);
            }
            else
            {
                UnregisterCamera();
            }
        }

        private string GetCameraName(string customData)
        {
            if (String.IsNullOrWhiteSpace(customData))
                return null;

            string prefix = _lcdComponent.Area + ":";
            using (StringReader reader = new StringReader(customData))
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith(prefix) && line.Length > prefix.Length)
                        return line.Substring(prefix.Length);
                    line = reader.ReadLine();
                }
            }
            return customData;
        }

        private struct RendererState
        {
            public bool Lodding;
            public bool DrawBillboards;
            public bool EyeAdaption;
            public bool Flares;
            public bool SSAO;
            public bool Bloom;
            public Vector2I ViewportResolution;
            public Vector2I ResolutionI;
        }

        private static readonly RendererState _rendererStateForCameraLCD = new RendererState
        {
            Lodding = false,
            DrawBillboards = true,
            EyeAdaption = true, // when turned off, makes the image too bright when surface is lit by sunlight
            Flares = false,
            SSAO = false,
            Bloom = false,
            //ViewportResolution = ,
            //ResolutionI = ,
        };

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
            if (!IsActive || _lcdComponent.ContentType != ContentType.SCRIPT)
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
                ViewportResolution = MyRender11.ViewportResolution,
                ResolutionI = MyRender11.ResolutionI,
            };

            var originalCameraState = CameraState.From(MyRender11.Environment.Matrices);

            {
                // set state for CameraLCD rendering
                SetRendererState(_rendererStateForCameraLCD with
                {
                    ViewportResolution = surfaceRtv.Size,
                    ResolutionI = surfaceRtv.Size,
                    DrawBillboards = true,
                });
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

            MyRender11.DeviceInstance.ImmediateContext1.GenerateMips(surfaceRtv.Srv);

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
            MyRenderMessageSetCameraViewMatrix renderMessage = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);
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

        private static void GetCameraViewMatrixAndPosition(MyCameraBlock camera, out MatrixD viewMatrix, out Vector3D position)
        {
            viewMatrix = camera.GetViewMatrix();

            // get camera's render object position since the entity's position may be desynced
            if (camera.Render != null && MyIDTracker<MyActor>.FindByID(camera.Render.GetRenderObjectID()) is MyActor actor)
            {
                position = actor.WorldMatrix.Translation;
            }
            else
            {
                position = camera.WorldMatrix.Translation;
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
            _lcd.CustomDataChanged -= OnCustomDataChanged;
            _lcd.IsWorkingChanged -= _ => UpdateIsActive();
            _lcd.CubeGridChanged -= _ => CubeGridChanged();
            _lcd.OnMarkForClose -= Lcd_OnMarkForClose;
        }
    }
}
