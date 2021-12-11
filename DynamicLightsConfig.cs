using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;

namespace DynamicLights
{
    class DynamicLightsConfig : ModConfig
    {
        [Label("Use Lighting")]
        [DefaultValue(true)]
        public bool UseLighting;

        [Label("Shadow Quality")]
        [Range(6, 15)]
        [Increment(1)]
        [DefaultValue(9)]
        [Slider]
        public int ShadowRes;

        [Label("Brightness Cutoff (helps performance)")]
        [Range(0.0f, 2.0f)]
        [DefaultValue(0.49)]
        [Slider]
        public float Cutoff;

        [Label("Maximum Light Cap (0 = no Limit)")]
        [Range(0, 0xffff)]
        [DefaultValue(1024)]
        public int LightCap;

        [Label("Shadow Smoothness")]
        [Range(0.0f, 5.0f)]
        [DefaultValue(1.5f)]
        [Slider]
        public float ShadowSmooth;

        [Label("Shine Distance")]
        [Range(0.0f, 5.0f)]
        [DefaultValue(1f)]
        [Slider]
        public float DistanceMult;

        [Label("Darkest Brightness")]
        [Range(0.0f, 1.0f)]
        [DefaultValue(0.5f)]
        [Slider]
        public float DarkBrightness;

        [Label("Brightest Brightness")]
        [Range(1.0f, 10.0f)]
        [DefaultValue(2.5f)]
        [Slider]
        public float BrightBrightness;

        [Label("Brightness Falloff")]
        [Range(1.0f, 5.0f)]
        [DefaultValue(1.75f)]
        [Slider]
        public float GrowthBase;

        [Label("Increase Surface Lighting")]
        [DefaultValue(true)]
        public bool IncreaseSurfaceLight;

        [Label("Show Sun")]
        [DefaultValue(false)]
        public bool ShowSun;

        /*[Label("Brightness Scale")]
        [Range(0.0f, 10.0f)]
        [DefaultValue(1.5f)]
        [Slider]
        public float GrowthRate;*/
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnChanged()
        {
            DynamicLights.use_lighting = UseLighting;
            DynamicLights.shadow_res_power = ShadowRes;
            DynamicLights.cutoff = Cutoff;
            DynamicLights.maxlights = LightCap;
            DynamicLights.shadow_smoothness = ShadowSmooth;
            DynamicLights.brightness_dist = DistanceMult;
            DynamicLights.dark_brightness = DarkBrightness;
            DynamicLights.bright_brightness = BrightBrightness;
            DynamicLights.brightness_growth_base = GrowthBase;
            DynamicLights.brightness_growth_rate = 1.5f;
            DynamicLights.increase_surface = IncreaseSurfaceLight;
            DynamicLights.show_sun = ShowSun;

            DynamicLights.rebuild = true;
        }
    }
}
