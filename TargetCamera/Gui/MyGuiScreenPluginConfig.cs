using System;
using System.Text;
using Sandbox;
using Sandbox.Graphics.GUI;
using SETargetCamera.GUI;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace SETargetCamera.Gui
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
        private const float space = 0.01f;

        private MyGuiControlCombobox ratioCombobox;
        private MyGuiControlLabel rangeLabel;
        private MyGuiControlParent contentPanel;
        

        public MyGuiScreenPluginConfig() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.6f, 0.8f), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
        {
            EnabledBackgroundFade = true;
            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenModConfig";
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            contentPanel = new MyGuiControlParent()
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Position = Vector2.Zero,
                Size = new Vector2(0.51f, 1.2f),
            };

            var scrollPanel = new MyGuiControlScrollablePanel(contentPanel)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER,
                Position = new Vector2(0f, 0.00f),
                Size = new Vector2(0.55f, 0.6f),
                ScrollbarVEnabled = true,
                CanFocusChildren = true,
                ScrolledAreaPadding = new MyGuiBorderThickness(0.005f),
            };
            Controls.Add(scrollPanel);
            
            TargetCameraSettings settings = Plugin.Settings;

            MyGuiControlLabel caption = AddCaption("Target Camera Settings");
            Vector2 pos = new Vector2(contentPanel.Size.X / 2 - space * 2, -contentPanel.Size.Y / 2);
            
            
            MyGuiControlSeparatorList seperators = new MyGuiControlSeparatorList();
            float sepWidth = Size.Value.X * 0.8f;
            seperators.AddHorizontal(pos - new Vector2(sepWidth / 2, 0), sepWidth);
            contentPanel.Controls.Add(seperators);
            pos.Y += space;
            
            // ENABLED
            pos = AddCheckbox(pos, settings.Enabled, "Enabled", IsEnabledCheckedChanged);

            // POSITION & SIZE
            pos = AddTextbox(pos, settings.Pos.X.ToString(), "X Position", -20000, 20000, tb => SetPosComponent(tb, true));
            pos = AddTextbox(pos, settings.Pos.Y.ToString(), "Y Position", -20000, 20000, tb => SetPosComponent(tb, false));
            pos = AddTextbox(pos, settings.Size.X.ToString(), "Width", 1, 20000, tb => SetSizeComponent(tb, true, 1));
            pos = AddTextbox(pos, settings.Size.Y.ToString(), "Height", 1, 20000, tb => SetSizeComponent(tb, false, 1));

            // CAMERA SETTINGS
            pos = AddTextbox(pos, settings.MinRange.ToString(), "Minimum Range", 0, decimal.MaxValue, tb => SetNumericSetting(tb, v => Plugin.Settings.MinRange = (float)v, 0));
            pos = AddTextbox(pos, settings.CameraSmoothing.ToString(), "Camera Smoothing", 1, decimal.MaxValue, tb => SetNumericSetting(tb, v => Plugin.Settings.CameraSmoothing = v, 1));
            pos = AddTextbox(pos, settings.BorderThickness.ToString(), "Border Thickness", 0, decimal.MaxValue, tb => SetNumericSetting(tb, v => Plugin.Settings.BorderThickness = (float)v, 0));
            pos = AddCheckbox(pos, settings.DamageFeedbackEnabled, "Damage Feedback", cb => Plugin.Settings.DamageFeedbackEnabled = cb.IsChecked);

            // BORDER COLOUR
            pos = AddColorControl(pos, settings.BorderColor, "Border Color", BorderColourChanged);
            pos = AddColorControl(pos, settings.DamageFeedbackColor, "Damage Feedback Color", DamageFeedbackColourChanged);
            // TARGET INDICATOR SETTINGS
            pos = AddTextbox(pos, settings.TargetIndicatorRadiusMin.ToString(), "Target Indicator Radius Min", 0, decimal.MaxValue, tb => SetNumericSetting(tb, v => Plugin.Settings.TargetIndicatorRadiusMin = (float)v, 0));
            pos = AddTextbox(pos, settings.TargetIndicatorRadiusMax.ToString(), "Target Indicator Radius Max", 0, decimal.MaxValue, tb => SetNumericSetting(tb, v => Plugin.Settings.TargetIndicatorRadiusMax = (float)v, 0));            
            // FULLSCREEN KEY BINDING
            pos = AddKeyBinding(pos, settings.FullscreenKey);

            // Bottom
            Vector2 closeButtonPos = new Vector2(0, (m_size.Value.Y / 2) - space);
            MyGuiControlButton closeButton = new MyGuiControlButton(closeButtonPos, text: MyTexts.Get(MyCommonTexts.Close), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, onButtonClick: OnCloseClicked);
            Controls.Add(closeButton);
        }

        private void OnBindingKeyClick(MyGuiControlButton button)
        {
            MyKeys key = (MyKeys)Plugin.Settings.FullscreenKey;
            MyPluginBinderMessageBox myGuiControlAssignKeyMessageBox = new MyPluginBinderMessageBox(key, new StringBuilder("Press desired Zoom key."), new StringBuilder("Zoom Binding"));
            myGuiControlAssignKeyMessageBox.Closed += delegate
            {
                Plugin.Settings.FullscreenKey = (byte)myGuiControlAssignKeyMessageBox.OutKey;
                this.RecreateControls(false);
            };
            MyGuiSandbox.AddScreen(myGuiControlAssignKeyMessageBox);
        }

        private void OnBindingKeySecondaryClick(MyGuiControlButton button)
        {
            Plugin.Settings.FullscreenKey = (byte)MyKeys.None;
            this.RecreateControls(false);
        }


        private void OnCloseClicked(MyGuiControlButton btn)
        {
            CloseScreen();
        }

        protected override void OnClosed()
        {
            Plugin.Settings.Save();
        }

        private void AddCaption(MyGuiControlBase control, string caption, bool offsetWidth = false)
        {

            var pos = new Vector2(-contentPanel.Size.X / 2 + space * 2, control.PositionY);
            
            contentPanel.Controls.Add(new MyGuiControlLabel(pos, text: caption, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP));
        }

        private Vector2 AddCheckbox(Vector2 pos, bool isChecked, string label, Action<MyGuiControlCheckbox> onChanged)
        {
            var checkbox = new MyGuiControlCheckbox(pos, isChecked: isChecked, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP);
            checkbox.IsCheckedChanged += onChanged;
            contentPanel.Controls.Add(checkbox);
            AddCaption(checkbox, label);
            return pos + new Vector2(0, checkbox.Size.Y + space);
        }

        private Vector2 AddTextbox(Vector2 pos, string text, string label, decimal minValue, decimal maxValue, Action<MyGuiControlTextbox> onChanged)
        {
            var textbox = new MyGuiControlTextbox(pos, text, 5, type: MyGuiControlTextboxType.DigitsOnly, minNumericValue: minValue, maxNumericValue: maxValue);
            textbox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            textbox.TextChanged += onChanged;
            textbox.Size = new Vector2(0.1f, textbox.Size.Y);
            contentPanel.Controls.Add(textbox);
            AddCaption(textbox, label, true);
            return pos + new Vector2(0, textbox.Size.Y + space);
        }

        private Vector2 AddColorControl(Vector2 pos, Color color, string label, Action<MyGuiControlColor> onChanged)
        {
            
            var size = new Vector2(0.30f, 0.04f);
            var pos2 = pos - new Vector2(size.X / 2, 0); // Colour control positions are handled weirdly. Thankskeen
            var colorControl = new MyGuiControlColor("", 0.95f, pos, color, Color.White, MyCommonTexts.DialogAmount_SetValueCaption, true, isAutoscaleEnabled: false);
            colorControl.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP;
            colorControl.Size = size;
            colorControl.OnChange += onChanged;
            
            contentPanel.Controls.Add(colorControl);
            AddCaption(colorControl, label, true);
            return pos + new Vector2(0, colorControl.Size.Y + space * 2);
        }

        private Vector2 AddKeyBinding(Vector2 pos, byte keyCode)
        {
            var keyName = ((MyKeys)keyCode == MyKeys.None) ? "None" : MyInput.Static.GetKeyName((MyKeys)keyCode);
            var keybindBox = new MyGuiControlButton(position: pos, originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, text: new StringBuilder(keyName), visualStyle: VRage.Game.MyGuiControlButtonStyleEnum.ControlSetting, onButtonClick: OnBindingKeyClick, onSecondaryButtonClick: OnBindingKeySecondaryClick, toolTip: "Click to edit.\nRight click to clear.");
            contentPanel.Controls.Add(keybindBox);
            AddCaption(keybindBox, "Fullscreen");
            return pos + new Vector2(0, keybindBox.Size.Y + space);
        }

        void IsEnabledCheckedChanged(MyGuiControlCheckbox cb)
        {
            Plugin.Settings.Enabled = cb.IsChecked;
        }

        private void SetPosComponent(MyGuiControlTextbox tb, bool isX)
        {
            var pos = Plugin.Settings.Pos;
            if (int.TryParse(tb.Text, out var result))
            {
                if (isX) pos.X = result;
                else pos.Y = result;
                Plugin.Settings.Pos = pos;
            }
        }

        private void SetSizeComponent(MyGuiControlTextbox tb, bool isWidth, int minValue)
        {
            var size = Plugin.Settings.Size;
            if (int.TryParse(tb.Text, out var result))
            {
                result = Math.Max(result, minValue);
                if (isWidth) size.X = result;
                else size.Y = result;
                Plugin.Settings.Size = size;
            }
        }

        private void SetNumericSetting(MyGuiControlTextbox tb, Action<double> setter, double minValue)
        {
            if (double.TryParse(tb.Text, out var result))
            {
                setter(Math.Max(result, minValue));
            }
        }
        
        private void BorderColourChanged(MyGuiControlColor cb)
        {
            Plugin.Settings.BorderColor = cb.Color;
        }

        private void DamageFeedbackColourChanged(MyGuiControlColor cb)
        {
            Plugin.Settings.DamageFeedbackColor = cb.Color;
        }
    }
}
