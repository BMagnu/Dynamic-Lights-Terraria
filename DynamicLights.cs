using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace DynamicLights
{
	public class DynamicLights : Mod
	{

		public static bool use_lighting = false;
		public static int shadow_res_power = 9; // 6 - 12, rec 9
		public static float cutoff = 1.0f; // 0 - Inf (10-ish?)
		public static int maxlights = 0;
		public static float shadow_smoothness = 5.0f; // 0 - Inf (10-ish?)
		public static float brightness_dist = 1.0f; // 0 - Inf (5-ish?)
		public static float dark_brightness = 0.75f; // 0 - 1
		public static float bright_brightness = 3.0f; // 1 - Inf (10-ish?)
		public static float brightness_growth_base = 1.2f; // 1 - Inf (5-ish?)
		public static float brightness_growth_rate = 1.5f; // 0 - Inf (10-ish?)
		public static bool increase_surface = true;
		public static bool show_sun = false;

		public static bool rebuild = false;

		//The Alpha channel of the shadowmap corresponds to what can cast shadows
		public RenderTarget2D shadowmap;
		public RenderTarget2D shadowcaster;
		public RenderTarget2D mappedshadows;
		public RenderTarget2D[] shadowreducer;
		public RenderTarget2D lightmap;
		public RenderTarget2D lightmapswap;
		public RenderTarget2D screen;

		bool clearNextFrame = false;

		public struct Light
        {
			public int x;
			public int y;
			public Vector3 color;
			public bool isSun;
			public Light(int x, int y, Vector3 color, bool isSun = false)
            {
				this.x = x;
				this.y = y;
				this.color = color;
				this.isSun = isSun;
            }
        }
		public List<Light> lights = new List<Light>();

		public static Effect FancyLights;



		public static Effect Disco;

		public override void Load()
		{
			if (!Main.dedServ)
			{
				FancyLights = ModContent.Request<Effect>("DynamicLights/Effects/Lights", (AssetRequestMode)2).Value;
			}
			On.Terraria.Graphics.Effects.FilterManager.EndCapture += FilterManager_EndCapture;
			On.Terraria.Graphics.Light.LightingEngine.AddLight += LightingEngine_AddLight;
			On.Terraria.Graphics.Light.LightingEngine.ApplyPerFrameLights += LightingEngine_Clear;
			On.Terraria.Main.LoadWorlds += Main_LoadWorlds;
			Main.OnResolutionChanged += Main_OnResolutionChanged;
		}

		public override void PostSetupContent()
		{
			if (!Main.dedServ)
			{
				FancyLights = ModContent.Request<Effect>("DynamicLights/Effects/Lights", (AssetRequestMode)2).Value;
			}
		}

		public override void Unload()
		{
			On.Terraria.Graphics.Effects.FilterManager.EndCapture -= FilterManager_EndCapture;
			On.Terraria.Main.LoadWorlds -= Main_LoadWorlds;
		}

		private void Main_LoadWorlds(On.Terraria.Main.orig_LoadWorlds orig)
		{
			orig();
			if (screen == null)
			{
				GraphicsDevice gd = Main.instance.GraphicsDevice;
				screen = new RenderTarget2D(gd, gd.PresentationParameters.BackBufferWidth , gd.PresentationParameters.BackBufferHeight , false, gd.PresentationParameters.BackBufferFormat, (DepthFormat)0);
				lightmap = new RenderTarget2D(gd, gd.PresentationParameters.BackBufferWidth, gd.PresentationParameters.BackBufferHeight, false, gd.PresentationParameters.BackBufferFormat, (DepthFormat)0);
				lightmapswap = new RenderTarget2D(gd, gd.PresentationParameters.BackBufferWidth, gd.PresentationParameters.BackBufferHeight, false, gd.PresentationParameters.BackBufferFormat, (DepthFormat)0);
				shadowmap = new RenderTarget2D(gd, gd.PresentationParameters.BackBufferWidth + Main.offScreenRange * 2, gd.PresentationParameters.BackBufferHeight + Main.offScreenRange * 2, false, gd.PresentationParameters.BackBufferFormat, (DepthFormat)0);
				shadowcaster = new RenderTarget2D(gd, 1 << shadow_res_power, 1 << shadow_res_power, false, gd.PresentationParameters.BackBufferFormat, (DepthFormat)0);
				mappedshadows = new RenderTarget2D(gd, 1 << shadow_res_power, 1 << shadow_res_power, false, gd.PresentationParameters.BackBufferFormat, (DepthFormat)0);
				shadowreducer = new RenderTarget2D[shadow_res_power - 1];
				for(int i = 1; i < shadow_res_power; i++)
                {
					shadowreducer[i - 1] = new RenderTarget2D(gd, 1 << i, 1 << shadow_res_power, false, gd.PresentationParameters.BackBufferFormat, (DepthFormat)0);
				}
			}
		}

		private void Main_OnResolutionChanged(Vector2 obj)
		{
			screen = new RenderTarget2D(Main.instance.GraphicsDevice, Main.screenWidth , Main.screenHeight );
			lightmap = new RenderTarget2D(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
			lightmapswap = new RenderTarget2D(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight);
			shadowmap = new RenderTarget2D(Main.instance.GraphicsDevice , Main.screenWidth + Main.offScreenRange * 2, Main.screenHeight + Main.offScreenRange * 2);
			shadowcaster = new RenderTarget2D(Main.instance.GraphicsDevice, 1 << shadow_res_power, 1 << shadow_res_power);
			mappedshadows = new RenderTarget2D(Main.instance.GraphicsDevice, 1 << shadow_res_power, 1 << shadow_res_power);
			shadowreducer = new RenderTarget2D[shadow_res_power - 1];
			for (int i = 1; i < shadow_res_power; i++)
			{
				shadowreducer[i - 1] = new RenderTarget2D(Main.instance.GraphicsDevice, 1 << i, 1 << shadow_res_power);
			}
		}

		private void LightingEngine_AddLight(On.Terraria.Graphics.Light.LightingEngine.orig_AddLight orig, LightingEngine self, int x, int y, Vector3 color)
		{
			orig(self, x, y, color);
			if (use_lighting)
			{
				lights.Add(new Light(x * 16 + 8, y * 16 + 8, color));
			}
		}

		private void LightingEngine_Clear(On.Terraria.Graphics.Light.LightingEngine.orig_ApplyPerFrameLights orig, LightingEngine self)
		{
			orig(self);
			if (use_lighting)
			{
				clearNextFrame = true;
			}
		}

		private void FilterManager_EndCapture(On.Terraria.Graphics.Effects.FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
		{
			GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;

            if (rebuild)
            {
				screen = new RenderTarget2D(graphicsDevice, Main.screenWidth, Main.screenHeight);
				lightmap = new RenderTarget2D(graphicsDevice, Main.screenWidth, Main.screenHeight);
				lightmapswap = new RenderTarget2D(graphicsDevice, Main.screenWidth, Main.screenHeight);
				shadowmap = new RenderTarget2D(graphicsDevice, Main.screenWidth + Main.offScreenRange * 2, Main.screenHeight + Main.offScreenRange * 2);
				shadowcaster = new RenderTarget2D(graphicsDevice, 1 << shadow_res_power, 1 << shadow_res_power);
				mappedshadows = new RenderTarget2D(graphicsDevice, 1 << shadow_res_power, 1 << shadow_res_power);
				shadowreducer = new RenderTarget2D[shadow_res_power - 1];
				for (int i = 1; i < shadow_res_power; i++)
				{
					shadowreducer[i - 1] = new RenderTarget2D(graphicsDevice, 1 << i, 1 << shadow_res_power);
				}
				rebuild = false;
			}

			LightingEngine lightingEngine = typeof(Lighting).GetField("_activeEngine", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as LightingEngine;

			if (use_lighting && lightingEngine != null)
            {
				TileLightScanner tileScanner = typeof(LightingEngine).GetField("_tileScanner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lightingEngine) as TileLightScanner;

				SpriteBatch spriteBatch = Main.spriteBatch;
				TileDrawing tilesRenderer = Main.instance.TilesRenderer;

				//Save Swap
				graphicsDevice.SetRenderTarget(Main.screenTargetSwap);
				graphicsDevice.Clear(Color.Transparent);
				spriteBatch.Begin((SpriteSortMode)0, BlendState.AlphaBlend);
				spriteBatch.Draw(Main.screenTarget, Vector2.Zero, Color.White);
				spriteBatch.End();

				//Draw Tile Shadowmap
				graphicsDevice.SetRenderTarget(shadowmap);
				graphicsDevice.Clear(Color.Transparent);
				spriteBatch.Begin();
				tilesRenderer.PreDrawTiles(true, true, true);
				tilesRenderer.Draw(true, true, true);
				spriteBatch.End();

				//Do PerLightShading
				graphicsDevice.SetRenderTarget(lightmapswap);
				graphicsDevice.Clear(new Color(0, 0, 0, 0));
				graphicsDevice.SetRenderTarget(lightmap);
				graphicsDevice.Clear(new Color(0, 0, 0, 0));
				BlendState blendState = new BlendState();
				blendState.ColorBlendFunction = BlendFunction.Max;
				blendState.AlphaBlendFunction = BlendFunction.Max;
				blendState.ColorSourceBlend = Blend.One;
				blendState.AlphaSourceBlend = Blend.One;
				blendState.ColorDestinationBlend = Blend.One;
				blendState.AlphaDestinationBlend = Blend.One;
				blendState.ColorWriteChannels = ColorWriteChannels.All;

				Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
				Vector2 vector = new Vector2((float)Main.offScreenRange, (float)Main.offScreenRange);

				object[] args = new object[] { unscaledPosition, vector + (Main.Camera.UnscaledPosition - Main.Camera.ScaledPosition), null, null, null, null };
				typeof(TileDrawing).GetMethod("GetScreenDrawArea", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(tilesRenderer, args);

				int cnt = 0;

				List<Light> lightsSort = new List<Light>();

				for (int i = (int) args[4]; i < (int) args[5] + 4; i++)
				{
					for (int j = (int) args[2] - 2; j < (int) args[3] + 2; j++)
					{
						Tile tile = Main.tile[j, i];
						if (tile == null)
							continue;

						if (!Main.tileLighted[tile.TileType])
							continue;

						Vector3 color;
						tileScanner.GetTileLight(j, i, out color);

						lightsSort.Add(new Light(j * 16 + 8, i * 16 + 8, color));
					}
				}

				int xmin = ((int)args[2] - 2) * 16 + 8;
				int xmax = ((int)args[3] + 2) * 16 + 8;
				int ymin = ((int)args[4]) * 16 + 8;
				int ymax = ((int)args[5] + 4) * 16 + 8;
				foreach (Light light in lights){
					if (light.x >= xmin && light.x <= xmax && light.y >= ymin && light.y <= ymax)
						lightsSort.Add(light);
                }

                if (show_sun)
                {
					Vector2 sunPos = GetSunPos();
					float sunFactor = 0.1f;
					Vector3 sunColor = new Vector3(Main.ColorOfTheSkies.R * sunFactor, Main.ColorOfTheSkies.G * sunFactor, Main.ColorOfTheSkies.B * sunFactor);
					lightsSort.Add(new Light((int) Main.screenPosition.X + (int)sunPos.X, (int)sunPos.Y, sunColor, true));
                }

				lightsSort.Sort((l, r) => GetBrightness(r).CompareTo(GetBrightness(l)));

				int targetCnt = (maxlights == 0 || lightsSort.Count < maxlights) ? lightsSort.Count : maxlights;
				for (int i = 0; i < targetCnt; i++)
                {
					bool rendered = PerLightShading(ref graphicsDevice, ref spriteBatch, blendState, lightsSort[i]);
					if (!rendered)
						cnt++;
				}

				//Main.NewText($"Cut: {cnt}", 255, 240, 20);

				if (clearNextFrame)
                {
					clearNextFrame = false;
					lights.Clear();
                }

				//Render Swap and Screen to Final
				graphicsDevice.SetRenderTarget(Main.screenTarget);
				graphicsDevice.Clear(Color.Transparent);
				spriteBatch.Begin((SpriteSortMode)1, BlendState.Opaque);

				FancyLights.Parameters["darkBrightness"].SetValue(increase_surface ? (dark_brightness + (1 - dark_brightness) * 0.8f / (1f + (float)Math.Exp(0.01 * (double)(Main.screenPosition.Y + Main.screenHeight / 2 - Main.worldSurface * 16.0f)))) : dark_brightness);
				FancyLights.Parameters["brightBrightness"].SetValue(bright_brightness);
				FancyLights.Parameters["brightnessGrowthBase"].SetValue(brightness_growth_base);
				FancyLights.Parameters["brightnessGrowthRate"].SetValue(brightness_growth_rate);
				FancyLights.Parameters["blurDistance"].SetValue(new Vector2(16f * (1.0f/5.0f) * shadow_smoothness / (float)Main.screenWidth, 16f * (1.0f / 5.0f) * shadow_smoothness / (float)Main.screenHeight));
				FancyLights.Parameters["lightMapTexture"].SetValue(lightmap);
				FancyLights.CurrentTechnique.Passes["CompositeFinal"].Apply();
				spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
				spriteBatch.End();
			}

			orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
		}

		private bool PerLightShading(ref GraphicsDevice graphicsDevice, ref SpriteBatch spriteBatch, BlendState lightBlend, Light light, bool isBlock = false)
        {
			float percievedBright = GetBrightness(light);

			if (percievedBright < cutoff)
				return false;

			int lightDistance = light.isSun ? 5000 : (int)(percievedBright * 200.0f * brightness_dist);
			graphicsDevice.SetRenderTarget(shadowcaster);
			graphicsDevice.Clear(Color.White);
			spriteBatch.Begin((SpriteSortMode)1, BlendState.Opaque);
			FancyLights.Parameters["lightCenter"].SetValue(new Vector2((float)((int)light.x - Main.screenPosition.X + Main.offScreenRange) / (float) ((Main.screenWidth) + Main.offScreenRange * 2) , (float)(light.y - (int)Main.screenPosition.Y + Main.offScreenRange) / (float) ((Main.screenHeight ) + Main.offScreenRange * 2)) );
			FancyLights.Parameters["sizeMult"].SetValue(new Vector2((float)((Main.screenWidth) + Main.offScreenRange * 2) / (float) (lightDistance), (float)((Main.screenHeight) + Main.offScreenRange * 2) / (float)(lightDistance)));
			FancyLights.Parameters["sizeBlock"].SetValue(isBlock ? new Vector2(8.0f / (float)lightDistance, 8.0f / (float)lightDistance) : new Vector2(-1, -1));
			FancyLights.CurrentTechnique.Passes["DistanceToShadowcaster"].Apply();
			spriteBatch.Draw(shadowmap, new Rectangle(0, 0, 1 << shadow_res_power, 1 << shadow_res_power), new Rectangle(light.x - (int) Main.screenPosition.X + (int)(Main.offScreenRange ) - lightDistance, light.y - (int) Main.screenPosition.Y + (int)(Main.offScreenRange ) - lightDistance, (int)(lightDistance * 2), (int)(lightDistance * 2)), Color.White);
			spriteBatch.End();

			graphicsDevice.SetRenderTarget(mappedshadows);
			graphicsDevice.Clear(Color.White);
			spriteBatch.Begin((SpriteSortMode)1, BlendState.Opaque);
			FancyLights.CurrentTechnique.Passes["DistortEquidistantAngle"].Apply();
			spriteBatch.Draw(shadowcaster, Vector2.Zero, Color.White);
			spriteBatch.End();

			int step = shadow_res_power - 2;

			while (step >= 0)
			{
				RenderTarget2D d = shadowreducer[step];
				RenderTarget2D s = (step == shadow_res_power - 2) ? mappedshadows : shadowreducer[step + 1];

				graphicsDevice.SetRenderTarget(d);
				graphicsDevice.Clear(Color.White);

				spriteBatch.Begin((SpriteSortMode)1, BlendState.Opaque);
				FancyLights.Parameters["texWidth"].SetValue(1.0f / ((float)(s.Width)));
				FancyLights.CurrentTechnique.Passes["HorizontalReduce"].Apply();
				spriteBatch.Draw(s, Vector2.Zero, Color.White);
				spriteBatch.End();

				step--;
			}

			graphicsDevice.SetRenderTarget(lightmap);
			graphicsDevice.Clear(new Color(0, 0, 0, 0));
			spriteBatch.Begin(SpriteSortMode.Immediate, lightBlend);
			spriteBatch.Draw(lightmapswap, Vector2.Zero, Color.White);
			FancyLights.Parameters["lightColor"].SetValue(light.color);
			FancyLights.Parameters["shadowMapTexture"].SetValue(shadowreducer[0]);
			FancyLights.CurrentTechnique.Passes["ApplyShadow"].Apply();
			spriteBatch.Draw(shadowcaster, new Rectangle((int)((light.x - Main.screenPosition.X - lightDistance) * Main.GameViewMatrix.Zoom.X - Main.screenWidth * (Main.GameViewMatrix.Zoom.X - 1) * 0.5) , (int)((light.y - Main.screenPosition.Y - lightDistance) * Main.GameViewMatrix.Zoom.Y - Main.screenHeight * (Main.GameViewMatrix.Zoom.Y - 1) * 0.5), (int)(lightDistance *2 * Main.GameViewMatrix.Zoom.Y), (int)(lightDistance*2 * Main.GameViewMatrix.Zoom.Y)), Color.White);
			spriteBatch.End();

			graphicsDevice.SetRenderTarget(lightmapswap);
			graphicsDevice.Clear(new Color(0,0,0,0));
			spriteBatch.Begin((SpriteSortMode)1, lightBlend);
			spriteBatch.Draw(lightmap, Vector2.Zero, Color.White);
			spriteBatch.End();

			return true;
		}

		private float GetBrightness (Light light)
        {
			//Real Luma
			//float percievedBright = 0.299f * light.color.X + 0.587f * light.color.Y + 0.114f * light.color.Z;

			//Fake Luma
			return 0.333f * light.color.X + 0.333f * light.color.Y + 0.333f * light.color.Z;
		}

		private Vector2 GetSunPos()
		{
			float bgTop = (int)((0.0 - (double)Main.screenPosition.Y) / (Main.worldSurface * 16.0 - 600.0) * 200.0);
			float height = 0;
			if (Main.dayTime)
			{
				if (Main.time < 27000.0)
				{
					height = bgTop + (float)Math.Pow(1.0 - Main.time / 54000.0 * 2.0, 2.0) * 250.0f + 180.0f;
				}
				else
				{
					height = bgTop + (float)Math.Pow((Main.time / 54000.0 - 0.5) * 2.0, 2.0) * 250.0f + 180.0f;
				}
			}
			return new Vector2((float)Main.time / (Main.dayTime ? 54000.0f : 32400.0f) * (float)(Main.screenWidth + 200f) - 100f, height + Main.sunModY);
		}

	}
}