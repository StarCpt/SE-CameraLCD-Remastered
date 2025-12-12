using Sandbox;
using Sandbox.Graphics.GUI;
using System;
using VRage;
using VRage.Utils;
using VRageMath;

namespace CameraLCD.Gui
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
        private const float space = 0.01f;

        public MyGuiScreenPluginConfig() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.47f, 0.4f), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
        {
            EnabledBackgroundFade = true;
            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName() => GetType().Name;

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(false);
        }

        public override void RecreateControls(bool constructor)
        {
            CameraLCDSettings settings = Plugin.Settings;

            MyGuiControlLabel caption = AddCaption("Camera LCD Settings");
            Vector2 pos = caption.Position;
            pos.Y += (caption.Size.Y / 2) + space;

            MyGuiControlSeparatorList seperators = new MyGuiControlSeparatorList();
            float sepWidth = Size.Value.X * 0.8f;
            seperators.AddHorizontal(pos - new Vector2(sepWidth / 2, 0), sepWidth);
            Controls.Add(seperators);
            pos.Y += space;

            MyGuiControlCheckbox enabledCheckbox = new MyGuiControlCheckbox(pos, isChecked: settings.Enabled, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            enabledCheckbox.IsCheckedChanged += IsEnabledCheckedChanged;
            Controls.Add(enabledCheckbox);
            AddCaption(enabledCheckbox, "Enabled");
            pos.Y += enabledCheckbox.Size.Y + space;

            pos.X -= 0.06f;

            MyGuiControlSlider ratioSlider = new MyGuiControlSlider(pos, 2, 30, 0.2f, settings.Ratio, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, intValue: true);
            ratioSlider.SetToolTip("Render camera view every nth frame.");
            ratioSlider.ValueChanged += OnRenderRatioChanged;
            Controls.Add(ratioSlider);
            AddCaption(ratioSlider, "Render ratio");
            AddCustomSliderLabel(ratioSlider, val => $"{val}x");
            pos.Y += ratioSlider.Size.Y + space;

            MyGuiControlSlider rangeSlider = new MyGuiControlSlider(pos, 10, 500, 0.2f, settings.Range, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, intValue: true);
            rangeSlider.SetToolTip("Stops rendering camera view if the distance between\nthe view position and lcd screen exceeds this value.");
            rangeSlider.ValueChanged += RangeValueChanged;
            Controls.Add(rangeSlider);
            AddCaption(rangeSlider, "Render range");
            AddCustomSliderLabel(rangeSlider, val => $"{val}m");
            pos.Y += rangeSlider.Size.Y + space;

            //MyGuiControlCheckbox headFixCheckbox = new MyGuiControlCheckbox(pos, isChecked: settings.HeadFix, toolTip: "Fix to render your own head in camera view", originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            //headFixCheckbox.IsCheckedChanged += IsHeadfixCheckedChanged;
            //Controls.Add(headFixCheckbox);
            //AddCaption(headFixCheckbox, "Head fix");
            //pos.Y += headFixCheckbox.Size.Y + space;

            // Bottom
            pos = new Vector2(0, (m_size.Value.Y / 2) - space);
            MyGuiControlButton closeButton = new MyGuiControlButton(pos, text: MyTexts.Get(MyCommonTexts.Close), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, onButtonClick: OnCloseClicked);
            Controls.Add(closeButton);
        }

        private void AddCustomSliderLabel(MyGuiControlSlider slider, Func<float, string> valueToTextFunc)
        {
            MyGuiControlLabel label = new()
            {
                Position = slider.Position + new Vector2(slider.Size.X + space, slider.Size.Y / 2),
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                Text = valueToTextFunc(slider.Value)
            };
            slider.ValueChanged += s => label.Text = valueToTextFunc(s.Value);
            Controls.Add(label);
        }

        private void OnCloseClicked(MyGuiControlButton btn)
        {
            CloseScreen();
        }

        protected override void OnClosed()
        {
            Plugin.Settings.Save();
        }

        private void AddCaption(MyGuiControlBase control, string caption)
        {
            Controls.Add(new MyGuiControlLabel(control.Position + new Vector2(-space, control.Size.Y / 2), text: caption, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER));
        }

        void IsEnabledCheckedChanged(MyGuiControlCheckbox cb)
        {
            Plugin.Settings.Enabled = cb.IsChecked;
        }

        private void OnRenderRatioChanged(MyGuiControlSlider slider)
        {
            Plugin.Settings.Ratio = (int)slider.Value;
        }

        void RangeValueChanged(MyGuiControlSlider slider)
        {
            Plugin.Settings.Range = (int)slider.Value;
        }

        void IsHeadfixCheckedChanged(MyGuiControlCheckbox cb)
        {
            Plugin.Settings.HeadFix = cb.IsChecked;
        }
    }
}
