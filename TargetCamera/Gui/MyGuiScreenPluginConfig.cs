using System;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Utils;
using VRageMath;

namespace CameraLCD.Gui
{
    public class MyGuiScreenPluginConfig : MyGuiScreenBase
    {
        private const float space = 0.01f;

        private MyGuiControlCombobox ratioCombobox;
        private MyGuiControlLabel rangeLabel;
        

        public MyGuiScreenPluginConfig() : base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, new Vector2(0.6f, 0.4f), false, null, MySandboxGame.Config.UIBkOpacity, MySandboxGame.Config.UIOpacity)
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
            TargetCameraSettings settings = Plugin.Settings;

            MyGuiControlLabel caption = AddCaption("Target Camera Settings");
            Vector2 pos = caption.Position;
            pos.Y += (caption.Size.Y / 2) + space;

            MyGuiControlSeparatorList sperators = new MyGuiControlSeparatorList();
            float sepWidth = Size.Value.X * 0.8f;
            sperators.AddHorizontal(pos - new Vector2(sepWidth / 2, 0), sepWidth);
            Controls.Add(sperators);
            pos.Y += space;
            
            // ENABLED
            MyGuiControlCheckbox enabledCheckbox = new MyGuiControlCheckbox(pos, isChecked: settings.Enabled, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            enabledCheckbox.IsCheckedChanged += IsEnabledCheckedChanged;
            Controls.Add(enabledCheckbox);
            AddCaption(enabledCheckbox, "Enabled");
            pos.Y += enabledCheckbox.Size.Y + space;
            
            // X
            MyGuiControlTextbox xBox = new MyGuiControlTextbox(pos, settings.X.ToString(), 5, type: MyGuiControlTextboxType.DigitsOnly, minNumericValue: -20000, maxNumericValue:20000);
            xBox.TextChanged += XPositionBoxChanged;
            Controls.Add(xBox);
            AddCaption(xBox, "X Position");
            pos.Y += xBox.Size.Y + space;
            
            // Y
            MyGuiControlTextbox yBox = new MyGuiControlTextbox(pos, settings.Y.ToString(), 5, type: MyGuiControlTextboxType.DigitsOnly, minNumericValue: -20000, maxNumericValue:20000);
            yBox.TextChanged += YPositionBoxChanged;
            Controls.Add(yBox);
            AddCaption(yBox, "Y Position");
            pos.Y += yBox.Size.Y + space;
            
            // WIDTH
            MyGuiControlTextbox wBox = new MyGuiControlTextbox(pos, settings.Width.ToString(), 5, type: MyGuiControlTextboxType.DigitsOnly, minNumericValue: 100, maxNumericValue:20000);
            wBox.TextChanged += WidthBoxChanged;
            Controls.Add(wBox);
            AddCaption(wBox, "Width");
            pos.Y += wBox.Size.Y + space;
            
            // HEIGHT
            MyGuiControlTextbox hBox = new MyGuiControlTextbox(pos, settings.Height.ToString(), 5, type: MyGuiControlTextboxType.DigitsOnly, minNumericValue: 100, maxNumericValue:20000);
            hBox.TextChanged += HeightBoxChanged;
            Controls.Add(hBox);
            AddCaption(hBox, "Height");
            pos.Y += hBox.Size.Y + space;
            
            // Bottom
            pos = new Vector2(0, (m_size.Value.Y / 2) - space);
            MyGuiControlButton closeButton = new MyGuiControlButton(pos, text: MyTexts.Get(MyCommonTexts.Close), originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, onButtonClick: OnCloseClicked);
            Controls.Add(closeButton);
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

        void XPositionBoxChanged(MyGuiControlTextbox tb)
        {
            Plugin.Settings.X = int.TryParse(tb.Text, out var result) ? result : 0;
        }
        void YPositionBoxChanged(MyGuiControlTextbox tb)
        {
            Plugin.Settings.Y = int.TryParse(tb.Text, out var result) ? result : 0;
        }
        
        void WidthBoxChanged(MyGuiControlTextbox tb)
        {
            Plugin.Settings.Width = Math.Max(int.TryParse(tb.Text, out var result) ? result : 100, 100);
        }
        void HeightBoxChanged(MyGuiControlTextbox tb)
        {
            Plugin.Settings.Height = Math.Max(int.TryParse(tb.Text, out var result) ? result : 100, 100);
        }
    }
}
