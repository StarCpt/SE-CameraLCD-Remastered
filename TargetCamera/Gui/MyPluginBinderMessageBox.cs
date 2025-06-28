using Sandbox;
using Sandbox.Game.GUI;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Audio;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace SETargetCamera.GUI
{
    internal class MyPluginBinderMessageBox : MyGuiScreenMessageBox
    {
        public MyPluginBinderMessageBox(MyKeys previousKey, StringBuilder text, StringBuilder caption)
            : base (MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.NONE, text, caption, default, default, default, default, null, 0, ResultEnum.YES, true, null, 0f, 0f, null, true, false, null)
        {
            base.DrawMouseCursor = false;
            this.m_isTopMostScreen = false;
            this.OutKey = previousKey;
            MyInput.Static.GetListOfPressedKeys(this.m_oldPressedKeys);
            this.m_closeOnEsc = false;
            base.CanBeHidden = true;
        }

        public override string GetFriendlyName() => "MyPluginBinderMessageBox_Zoom";

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape) || MyControllerHelper.IsControl(MyControllerHelper.CX_GUI, MyControlsGUI.CANCEL, MyControlStateType.NEW_PRESSED, false, false))
            {
                this.Canceling();
            }
            if (base.State == MyGuiScreenState.CLOSING || base.State == MyGuiScreenState.HIDING)
            {
                return;
            }

            this.HandleKey();
        }

        private void HandleKey()
        {
            this.ReadPressedKeys();

            foreach(MyKeys key in this.m_newPressedKeys)
            {
                if (!this.m_oldPressedKeys.Contains(key))
                {
                    if (!MyInput.Static.IsKeyValid(key))
                    {
                        this.ShowControlIsNotValidMessageBox();
                        break;
                    }
                    MyGuiAudio.PlaySound(MyGuiSounds.HudMouseClick);
                    OutKey = key;
                    CloseScreen(false);
                    break;
                }
            }

            this.m_oldPressedKeys.Clear();
            MyUtils.Swap<List<MyKeys>>(ref this.m_oldPressedKeys, ref this.m_newPressedKeys);
        }

        private void ReadPressedKeys()
        {
            MyInput.Static.GetListOfPressedKeys(this.m_newPressedKeys);
            this.m_newPressedKeys.Remove(MyKeys.Control);
            this.m_newPressedKeys.Remove(MyKeys.Shift);
            this.m_newPressedKeys.Remove(MyKeys.Alt);
            if (this.m_newPressedKeys.Contains(MyKeys.LeftControl) && this.m_newPressedKeys.Contains(MyKeys.RightAlt))
            {
                this.m_newPressedKeys.Remove(MyKeys.LeftControl);
            }
        }

        private void ShowControlIsNotValidMessageBox()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, MyTexts.Get(MyCommonTexts.ControlIsNotValid), MyTexts.Get(MyCommonTexts.CanNotAssignControl), null, null, null, null, null, 0, MyGuiScreenMessageBox.ResultEnum.YES, true, null, true, null, true, false, null));
        }

        private MyGuiScreenMessageBox MakeControlIsAlreadyAssignedDialog(MyControl controlAlreadySet, StringBuilder controlButtonName)
        {
            return MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.YES_NO, new StringBuilder(string.Format(MyTexts.GetString(MyCommonTexts.ControlAlreadyAssigned), controlButtonName, MyTexts.Get(controlAlreadySet.GetControlName()))), MyTexts.Get(MyCommonTexts.CanNotAssignControl), null, null, null, null, null, 0, MyGuiScreenMessageBox.ResultEnum.YES, true, null, true, null, true, false, null);
        }

        private void OverwriteAssignment(MyKeys key)
        {
            OutKey = key;
        }

        private void AnywayAssignment(MyControl controlAlreadySet, MyKeys key)
        {
            OutKey = key;
        }

        public override bool CloseScreen(bool isUnloading = false)
        {
            base.DrawMouseCursor = true;
            return base.CloseScreen(isUnloading);
        }

        public MyKeys OutKey = MyKeys.None;

        private List<MyKeys> m_newPressedKeys = new List<MyKeys>();

        private List<MyKeys> m_oldPressedKeys = new List<MyKeys>();
    }
}