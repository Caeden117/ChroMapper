using System;
using UnityEngine;

namespace Beatmap.Info
{
    public class InfoColorScheme
    {
        [Obsolete("This property is used for v2 and v3 only.")]
        public bool UseOverride { get; set; }
        public string ColorSchemeName { get; set; }
        
        public bool OverrideNotes { get; set; }
        public Color SaberAColor { get; set; }
        public Color SaberBColor { get; set; }
        public Color ObstaclesColor { get; set; }
        
        public bool OverrideLights { get; set; }
        public Color EnvironmentColor0 { get; set; }
        public Color EnvironmentColor1 { get; set; }
        public Color EnvironmentColor0Boost { get; set; }
        public Color EnvironmentColor1Boost { get; set; }
    }
}