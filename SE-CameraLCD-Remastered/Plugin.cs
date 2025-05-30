﻿using CameraLCD.Gui;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using System;
using System.Reflection;
using VRage.Plugins;

namespace CameraLCD
{
    public class Plugin : IPlugin
    {
        public static CameraLCDSettings Settings { get; private set; }

        public Plugin()
        {
            Settings = CameraLCDSettings.Load();
        }

        public void Init(object gameInstance)
        {
            new Harmony(nameof(CameraLCD)).PatchAll(Assembly.GetExecutingAssembly());
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
    }
}
