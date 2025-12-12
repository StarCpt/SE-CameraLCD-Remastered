using CameraLCD.Gui;
using HarmonyLib;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Reflection;
using VRage;
using VRage.Plugins;

namespace CameraLCD
{
    public class Plugin : IPlugin
    {
        public static CameraLCDSettings Settings { get; private set; }
        public static Boxed<(uint CharacterActorId, string[] MaterialsDisabledInFirst)>? FirstPersonCharacter = null;

        public Plugin()
        {
            Settings = CameraLCDSettings.Load();
        }

        public void Init(object gameInstance)
        {
            new Harmony(nameof(CameraLCD)).PatchAll(Assembly.GetExecutingAssembly());
        }

        private uint _counter = 0;
        public void Update()
        {
            if (++_counter % 10 != 0 || !Settings.Enabled)
                return;

            if (MySession.Static?.CameraController?.Entity is MyCharacter character && (character.IsInFirstPersonView || character.ForceFirstPersonCamera))
            {
                FirstPersonCharacter = new((character.Render.GetRenderObjectID(), character.Definition.MaterialsDisabledIn1st));
            }
            else
            {
                FirstPersonCharacter = null;
            }
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
