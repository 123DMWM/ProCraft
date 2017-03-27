// ProCraft Copyright 2014-2016 Joseph Beauvais <123DMWM@gmail.com>
using System;
using System.IO;
using ServiceStack.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace fCraft {

    public sealed class EnvPresets {


        public string Name { get; set; }

        // EnvColors
        public string SkyColor { get; set; }
        public string CloudColor { get; set; }
        public string FogColor { get; set; }
        public string ShadowColor { get; set; }
        public string LightColor { get; set; }
        // EnvMapAppearance
        public string TextureURL { get; set; }
        public byte BorderBlock { get; set; }
        public byte HorizonBlock { get; set; }
        public short HorizonLevel { get; set; }
        // EnvMapAppearance v.2
        public short CloudLevel { get; set; }
        public short MaxViewDistance { get; set; }
        // EnvWeatherType
        public byte WeatherType { get; set; }



        public static List<EnvPresets> Presets = new List<EnvPresets>();

        public static EnvPresets Find(string name) {
            foreach (EnvPresets env in Presets) {
                if (env.Name.CaselessEquals(name)) return env;
            }
            return null;
        }

        public static bool exists(string name) {
            foreach (EnvPresets env in Presets) {
                if (env.Name.CaselessEquals(name)) return true;
            }
            return false;
        }

        public static void CreateEnvPreset(World world, string name) {
            EnvPresets preset = new EnvPresets();
            preset.Name = name.ToLower();
            preset.SkyColor = world.SkyColor;
            preset.CloudColor = world.CloudColor;
            preset.FogColor = world.FogColor;
            preset.ShadowColor = world.ShadowColor;
            preset.LightColor = world.LightColor;
            preset.TextureURL = world.Texture;
            preset.BorderBlock = (byte)world.EdgeBlock;
            preset.HorizonBlock = (byte)world.HorizonBlock;
            preset.HorizonLevel = (world.EdgeLevel == world.map.Height/2 ? (short)-1 : world.EdgeLevel);
            preset.CloudLevel = (world.CloudsHeight == world.map.Height+2 ? short.MinValue : world.CloudsHeight);
            preset.MaxViewDistance = world.MaxFogDistance;
            preset.WeatherType = world.Weather;
            Presets.Add(preset);
            SaveAll();
        }

        public static void LoadupEnvPreset(World world, string name) {
            EnvPresets preset = Find(name);
            world.SkyColor = preset.SkyColor;
            world.CloudColor = preset.CloudColor;
            world.FogColor = preset.FogColor;
            world.ShadowColor = preset.ShadowColor;
            world.LightColor = preset.LightColor;
            world.Texture = preset.TextureURL;
            Block Border, Horizon;
            Map.GetBlockByName(world, preset.BorderBlock.ToString(), false, out Border);
            if (Border == Block.None) Border = Block.Admincrete;
            Map.GetBlockByName(world, preset.HorizonBlock.ToString(), false, out Horizon);
            if (Horizon == Block.None) Horizon = Block.Water;
            world.EdgeBlock = (byte)Border;
            world.HorizonBlock = (byte)Horizon;
            world.EdgeLevel = preset.HorizonLevel;
            world.CloudsHeight = preset.CloudLevel;
            world.MaxFogDistance = preset.MaxViewDistance;
            world.Weather = preset.WeatherType;
            foreach (Player p in world.Players) {
                p.SendEnvSettings();
            }
            WorldManager.SaveWorldList();
        }

        public static void RemoveEnvPreset(string name) {
            EnvPresets preset = Find(name);
            if (preset != null) {
                Presets.Remove(preset);
            }
        }

        public static void ReloadAll() {
            Presets.Clear();
            LoadAll();
        }

        public static void SaveAll() {
            try {
                Stopwatch sw = Stopwatch.StartNew();
                using (Stream s = File.Create(Paths.EnvPresetsFileName)) {
                    JsonSerializer.SerializeToStream(Presets.ToArray(), s);
                }
                Logger.Log(LogType.Debug, "EnvPresets.Save: Saved Env presets in {0}ms", sw.ElapsedMilliseconds);
            } catch (Exception ex) {
                Logger.Log(LogType.Error, "EnvPresets.Save: " + ex);
            }
        }

        public static void LoadAll() {
            if (!File.Exists(Paths.EnvPresetsFileName)) return;

            try {
                using (Stream s = File.OpenRead(Paths.EnvPresetsFileName)) {
                    Presets = (List<EnvPresets>)
                        JsonSerializer.DeserializeFromStream(typeof(List<EnvPresets>), s);
                }
                int count = 0;
                for (int i = 0; i < Presets.Count; i++) {
                    if (Presets[i] == null)
                        continue;
                    // fixup for servicestack not writing out null entries
                    if (Presets[i].Name == null) {
                        Presets[i] = null; continue;
                    }
                    count++;
                }
                Logger.Log(LogType.SystemActivity, "EnvPresets.Load: Loaded " + count + " presets");
            } catch (Exception ex) {
                Presets = null;
                Logger.Log(LogType.Error, "EnvPresets.Load: " + ex);
            }
        }
    }
}