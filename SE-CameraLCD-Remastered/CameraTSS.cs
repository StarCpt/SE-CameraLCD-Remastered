using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRage.Game.Entity;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Import;
using VRageRender.Messages;
using VRageRenderAccessor.VRage.Render11.Common;
using VRageRenderAccessor.VRage.Render11.Resources;
using VRageRenderAccessor.VRage.Render11.Resources.Internal;
using VRageRenderAccessor.VRageRender;

namespace CameraLCD
{
    [MyTextSurfaceScript("TSS_CameraDisplay_2", "Camera Display (New)")]
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

        public bool Draw()
        {
            if (!IsActive || _lcdComponent.ContentType != ContentType.SCRIPT)
                return false;

            MyCamera renderCamera = MySector.MainCamera;
            if (renderCamera is null || renderCamera.GetDistanceFromPoint(_lcd.WorldMatrix.Translation) > Plugin.Settings.Range)
                return false;

            if (!TryGetRenderTextureName(out string renderTextureName))
                return false;

            MyManagers.FileTextures.TryGetTexture(renderTextureName, out MyUserGeneratedTexture surfaceRtv);
            if (surfaceRtv is null)
                return false;

            Vector2I ogViewportResolution = MyRender11.ViewportResolution;
            Vector2I ogResolutionI = MyRender11.ResolutionI;
            bool ogLods = true;

            if (!Plugin.Settings.UpdateLOD)
                ogLods = SetLoddingEnabled(false);
            bool ogDrawBillboards = MyRender11.Settings.DrawBillboards;
            MyRender11.Settings.DrawBillboards = false;
            MyRenderDebugOverrides debugOverrides = MyRender11.DebugOverrides;
            bool ogFlares = debugOverrides.Flares;
            bool ogSSAO = debugOverrides.SSAO;
            bool ogBloom = debugOverrides.Bloom;
            bool ogFxaa = debugOverrides.Fxaa;
            debugOverrides.Flares = false;
            debugOverrides.SSAO = false;
            debugOverrides.Bloom = false;
            //debugOverrides.Fxaa = false;

            {
                MyRender11.ViewportResolution = surfaceRtv.Size;
                MyRender11.ResolutionI = surfaceRtv.Size;
                MatrixD viewMatrix = CreateViewMatrix(_camera);
                float fov = _camera.GetFov();
                SetCameraViewMatrix(viewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, fov, fov, GetCameraPosition(_camera), 1);

                var borrowedRtv = MyManagers.RwTexturesPool.BorrowRtv("CameraLCD_TempRtv", surfaceRtv.Size.X, surfaceRtv.Size.Y, surfaceRtv.Format);

                MyRender11.DrawGameScene(borrowedRtv, out var debugAmbientOcclusion);
                debugAmbientOcclusion?.Release();

                MyRender11.RC.ClearRtv(surfaceRtv, new RawColor4(0, 0, 0, 0));
                CopyReplaceNoAlpha(surfaceRtv, borrowedRtv);

                MyRender11.ViewportResolution = ogViewportResolution;
                MyRender11.ResolutionI = ogResolutionI;
                SetCameraViewMatrix(renderCamera.ViewMatrix, renderCamera.ProjectionMatrix, renderCamera.ProjectionMatrixFar, renderCamera.FieldOfView, renderCamera.FieldOfView, renderCamera.Position, 0);
            }

            if (!Plugin.Settings.UpdateLOD)
                SetLoddingEnabled(ogLods);
            MyRender11.Settings.DrawBillboards = ogDrawBillboards;
            debugOverrides.Flares = ogFlares;
            debugOverrides.SSAO = ogSSAO;
            debugOverrides.Bloom = ogBloom;
            //debugOverrides.Fxaa = ogFxaa;

            MyRender11.DeviceInstance.ImmediateContext1.GenerateMips(surfaceRtv.Srv);

            return true;
        }

        private void CopyReplaceNoAlpha(IRtvBindable destination, ISrvBindable source)
        {
            MyRender11.RC.SetBlendState(MyBlendStateManager.BlendReplaceNoAlphaChannel);

            MyRender11.RC.SetInputLayout(null);
            MyRender11.RC.PixelShader.Set(MyCopyToRT.CopyPs);

            MyRender11.RC.SetRtv(destination);
            MyRender11.RC.SetDepthStencilState(MyDepthStencilStateManager.IgnoreDepthStencil);
            MyRender11.RC.PixelShader.SetSrv(0, source);
            MyScreenPass.DrawFullscreenQuad(MyRender11.RC, new MyViewport(destination.Size));
        }

        private bool SetLoddingEnabled(bool enabled)
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

        private void SetCameraViewMatrix(MatrixD viewMatrix, Matrix projMatrix, Matrix projFarMatrix, float fov, float fovSkybox, Vector3D cameraPosition, int lastMomentUpdateIndex)
        {
            MyCamera renderCamera = MySector.MainCamera;
            MyRenderMessageSetCameraViewMatrix renderMessage = MyRenderProxy.MessagePool.Get<MyRenderMessageSetCameraViewMatrix>(MyRenderMessageEnum.SetCameraViewMatrix);
            renderMessage.ViewMatrix = viewMatrix;
            renderMessage.ProjectionMatrix = projMatrix;
            renderMessage.ProjectionFarMatrix = projFarMatrix;
            renderMessage.FOV = fov;
            renderMessage.FOVForSkybox = fovSkybox;
            renderMessage.NearPlane = renderCamera.NearPlaneDistance;
            renderMessage.FarPlane = renderCamera.FarPlaneDistance;
            renderMessage.FarFarPlane = renderCamera.FarFarPlaneDistance;
            renderMessage.CameraPosition = cameraPosition;
            renderMessage.LastMomentUpdateIndex = lastMomentUpdateIndex;
            renderMessage.ProjectionOffsetX = 0;
            renderMessage.ProjectionOffsetY = 0;
            renderMessage.Smooth = false;
            MyRender11.SetupCameraMatrices(renderMessage);
        }

        private static MatrixD CreateViewMatrix(MyCameraBlock camera)
        {
            return camera.GetViewMatrix();
        }

        private static Vector3D GetCameraPosition(MyCameraBlock camera)
        {
            MatrixD matrix = camera.WorldMatrix;
            matrix.Translation += camera.WorldMatrix.Forward * 0.20000000298023224;
            if (camera.Model.Dummies != null)
            {
                foreach (KeyValuePair<string, MyModelDummy> dummy in camera.Model.Dummies)
                {
                    if (dummy.Value.Name == "camera")
                    {
                        Quaternion rotation = Quaternion.CreateFromForwardUp(camera.WorldMatrix.Forward, camera.WorldMatrix.Up);
                        matrix.Translation += MatrixD.Transform(dummy.Value.Matrix, rotation).Translation;
                        break;
                    }
                }
            }
            return matrix.Translation;
        }

        private bool TryGetRenderTextureName(out string name)
        {
            try
            {
                name = _lcdComponent.GetRenderTextureName();
                return true;
            }
            catch (NullReferenceException)
            {
                name = null;
                return false;
            }
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
