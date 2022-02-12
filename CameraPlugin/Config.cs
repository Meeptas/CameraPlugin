using Dalamud.Configuration;

namespace CameraPlugin
{
    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public float ZoomMax { get; set; } = 20f;
        public float ZoomMin { get; set; } = 1.5f;
        public float FovMax { get; set; } = 0.78f;
        public float FovCurrent { get; set; } = 0.78f;
    }
}