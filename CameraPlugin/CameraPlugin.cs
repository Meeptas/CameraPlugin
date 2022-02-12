using Dalamud.Game.Command;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;

namespace CameraPlugin
{
    public class CameraPlugin : IDalamudPlugin
    {
        public string Name => "CameraMan";
        private const string CameraCommand = "/camera";

        [PluginService]
        public DalamudPluginInterface Interface { get; set; }
        
        [PluginService]
        public CommandManager CommandManager { get; set; }
        
        [PluginService]
        public SigScanner SigScanner { get; set; }
        
        private IntPtr CameraAddress { get; set; }
        
        internal bool IsImguiSetupOpen = false;
        private readonly Config _config;

        public CameraPlugin()
        {
            Interface.UiBuilder.Draw += OnDraw;
            Interface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

            CommandManager.AddHandler(CameraCommand,
                new CommandInfo(CommandHandler)
                    {HelpMessage = "Opens camera configuration settings", ShowInHelp = true});

            var cameraDistance = SigScanner.GetStaticAddressFromSig("74 05 E8 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8", 7);
            CameraAddress = Marshal.ReadIntPtr(cameraDistance) + 0x114;

            _config = Interface.GetPluginConfig() as Config ?? new Config();
            SetFromConfig(_config);
        }

        private void OnOpenConfigUi()
        {
            IsImguiSetupOpen = true;
        }

        public void Dispose()
        {
            SaveToConfig();
            SetFromConfig(new Config());
            
            CommandManager.RemoveHandler(CameraCommand);
            Interface.UiBuilder.Draw -= OnDraw;
        }

        private unsafe void SaveToConfig()
        {
            var mem = (CameraMemory*)CameraAddress;
            if (mem != null)
            {
                _config.ZoomMax = mem->zoomMax;
                _config.ZoomMin = mem->zoomMin;
                _config.FovCurrent  = mem->fovCurrent;
                _config.FovMax  = mem->fovMax;
            }
            else
            {
                PluginLog.Debug("---Error Saving---");
            }
            
            Interface.SavePluginConfig(_config);
        }

        private void CommandHandler(string command, string args) => IsImguiSetupOpen = true;

        private unsafe void OnDraw()
        {
            if (!IsImguiSetupOpen)
                return;

            ImGui.SetNextWindowSize(new Vector2(375, 165), ImGuiCond.Always);
            ImGui.Begin("Zoom Setup", ref IsImguiSetupOpen, ImGuiWindowFlags.NoResize);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 5));

            var mem = (CameraMemory*)CameraAddress;
            if (mem != null)
            {
                ImGui.SliderFloat("Zoom Current", ref mem->zoomCurrent, 0, mem->zoomMax);
                ImGui.SliderFloat("Zoom Min", ref mem->zoomMin, 0, 10);
                if (ImGui.SliderFloat("Zoom Max", ref mem->zoomMax, 0, 500))
                {
                    if (mem->zoomMax < mem->zoomCurrent)
                        mem->zoomCurrent = mem->zoomMax;
                }
                ImGui.SliderFloat("FoV Current", ref mem->fovCurrent, 0.6f, mem->fovMax);
                if (ImGui.SliderFloat("FoV Max", ref mem->fovMax, .6f, 1.3f))
                {
                    if (mem->fovMax < mem->fovCurrent)
                        mem->fovCurrent = mem->fovMax;
                }
            }
            else
            {
                ImGui.Text($"---Error---");
            }

            ImGui.PopStyleVar();

            ImGui.End();
        }

        private unsafe void SetFromConfig(Config config)
        {
            var mem = (CameraMemory*)CameraAddress;
            if (mem != null)
            {
                mem->zoomMax = config.ZoomMax;
                mem->zoomMin = config.ZoomMin;
                mem->fovCurrent = config.FovCurrent;
                mem->fovMax = config.FovMax;
                
                if (mem->zoomMax < mem->zoomCurrent)
                    mem->zoomCurrent = mem->zoomMax;
            }
            else
            {
                PluginLog.Debug("---Error---");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CameraMemory
        {
            public float zoomCurrent;
            public float zoomMin;
            public float zoomMax;
            public float fovCurrent;
            public float fovInterval;
            public float fovMax;
            public float unkCurrent;
            public float unkInterval;
            public float unkMax;
        }
    }
}
