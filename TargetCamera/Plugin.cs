using CameraLCD.Gui;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using System;
using System.Reflection;
using DeltaWing.TargetCamera;
using VRage.Plugins;

namespace CameraLCD
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
            new Harmony(nameof(CameraLCD)).PatchAll(Assembly.GetExecutingAssembly());
            TargetCamera.ModLoad();
        }

        public void Update()
        {
            //TargetCamera.Update();
            
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
