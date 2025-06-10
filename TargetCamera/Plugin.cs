using HarmonyLib;
using Sandbox.Graphics.GUI;
using System;
using System.Reflection;
using SETargetCamera.Gui;
using VRage.Plugins;
using VRageMath;
using VRageRender;

namespace SETargetCamera
{
    public class Plugin : IPlugin
    {
        public static TargetCameraSettings Settings { get; private set; }

        public Plugin()
        {
            Settings = TargetCameraSettings.Load();
        }

        public void Init(object gameInstance)
        {
            new Harmony(nameof(SETargetCamera)).PatchAll(Assembly.GetExecutingAssembly());
            TargetCamera.ModLoad();
        }

        public void Update()
        {
            
        }

        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenPluginConfig());
        }

        public void Dispose()
        {
        }

        public void Load()
        {
            
        }
    }
}
