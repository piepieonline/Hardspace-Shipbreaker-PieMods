using BBI.Unity.Game;
using BBI.Unity.Game.Gameplay;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomPostersAndStickers
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class CustomPostersAndStickers : BaseUnityPlugin
    {
        public static ManualLogSource LoggerInstance;

        public static List<StickerAsset> CustomStickers;
        public static Dictionary<StickerAsset, string> CustomStickerGuidMap;

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                LoggerInstance = Logger;
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                CreateStickers();

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }

        private void CreateStickers()
        {
            CustomStickers = new List<StickerAsset>();
            CustomStickerGuidMap = new Dictionary<StickerAsset, string>();

            var newStickerTemplate = ScriptableObject.CreateInstance<StickerAsset>();
            typeof(StickerAsset).GetField("m_StickerImage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, null);
            typeof(StickerAsset).GetField("m_StickerObjectiveType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, StickerAsset.StickerObjectiveType.PAT);
            typeof(StickerAsset).GetField("m_Objective", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, null); // TODO: Load asset
            typeof(StickerAsset).GetField("m_ObtainOnlyOnce", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, false);
            typeof(StickerAsset).GetField("m_AmountRequiredForSticker", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, 1.0);
            typeof(StickerAsset).GetField("m_SalvageType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, SalvageableChangedEvent.SalvageableState.Processed);
            typeof(StickerAsset).GetField("m_MustCompleteWithinSingleShip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, false);
            typeof(StickerAsset).GetField("m_MustCollectAllInstancesOfObject", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, false);
            typeof(StickerAsset).GetField("m_CurrencyAmountInSingleShip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, 0.0f);
            typeof(StickerAsset).GetField("m_ModuleRequiredList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, new List<ModuleConstructionAsset>());
            typeof(StickerAsset).GetField("m_Size", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, null); // TODO: Retrieve this
            typeof(StickerAsset).GetField("m_CanUpgrade", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, false);
            typeof(StickerAsset).GetField("m_IsShiny", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, false);

            foreach (var stickerConfig in System.IO.Directory.EnumerateDirectories((System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Stickers"))))
            {
                var manifest = JsonConvert.DeserializeObject<StickerManifest>(File.ReadAllText(Path.Combine(stickerConfig, "manifest.json")));
                var newSticker = GameObject.Instantiate(newStickerTemplate);

                // StickerImage
                var texture = new Texture2D(256, 128);
                texture.name = $"{manifest.stickerName} texture";
                texture.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(stickerConfig, manifest.imagePath)));
                var newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));

                var material = new Material(Shader.Find("HDRP/Lit"));
                material.mainTexture = texture;

                newSprite.name = $"{manifest.stickerName} sprite";
                newSticker.name = manifest.stickerName;

                typeof(StickerAsset).GetField("m_NameLocID", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, $"CS m_NameLocID {manifest.stickerName}");
                typeof(StickerAsset).GetField("m_DescriptionLocID", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newStickerTemplate, $"CS m_DescriptionLocID {manifest.stickerName}");
                typeof(StickerAsset).GetField("m_StickerImage", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newSticker, newSprite);
                typeof(StickerAsset).GetField("m_MaterialOverride", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newSticker, material);

                CustomStickers.Add(newSticker);
                CustomStickerGuidMap.Add(newSticker, manifest.guid);
                LoggerInstance.LogInfo($"Loaded Custom Sticker: {manifest.stickerName}");
            }

        }
    }
}
