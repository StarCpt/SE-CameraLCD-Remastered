using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using System.Text;
using VRageMath;

namespace CameraLCD.Patches
{
    [HarmonyPatch(typeof(MyCameraBlock), "CreateTerminalControls")]
    public static class Patch_MyCameraBlock
    {
        public static void Prefix(ref bool __state)
        {
            __state = MyTerminalControlFactory.AreControlsCreated<MyCameraBlock>();
        }

        public static void Postfix(bool __state)
        {
            if (!__state)
            {
                MyTerminalControlSlider<MyCameraBlock> slider = new MyTerminalControlSlider<MyCameraBlock>("Zoom", MySpaceTexts.ControlDescCameraZoom, MySpaceTexts.Blank)
                {
                    Enabled = x => x.IsWorking,
                    Setter = MyCameraBlockAccessor.SetFov,
                    Getter = MyCameraBlockAccessor.GetFov,
                    Writer = WriteCameraFov,
                };
                slider.SetLimits(GetZoomMin, GetZoomMax);
                slider.EnableActions(MathHelper.ToRadians(10), slider.Enabled, slider.Enabled);
                MyTerminalControlFactory.AddControl(slider);
            }

        }

        private static void WriteCameraFov(MyCameraBlock block, StringBuilder writeTo)
        {
            writeTo.Append((int)MathHelper.ToDegrees(block.GetFov()));
        }

        private static float GetZoomMin(MyCameraBlock block) => block.BlockDefinition.MinFov;
        private static float GetZoomMax(MyCameraBlock block) => block.BlockDefinition.MaxFov;
    }
}
