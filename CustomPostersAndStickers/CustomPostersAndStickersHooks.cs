using BBI.Unity.Game;
using BBI.Unity.Game.Gameplay;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomPostersAndStickers
{
    class CustomPostersAndStickersHooks
    {

        // Configure the number of ships in the campaign menu
        [HarmonyPatch(typeof(PlayerSettings), "UnlockedPosters", MethodType.Getter)]
        public class PlayerSettings_UnlockedPosters
        {
            public static bool added = false;

            public static bool Prefix(PlayerSettings __instance, ref List<NarrativeContentImageAsset> __result)
            {
                var levelDataFI = typeof(PlayerSettings).GetField("m_UnlockedPosters", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var levelData = (List<NarrativeContentImageAsset>)levelDataFI.GetValue(__instance);

                __result = levelData;

                if (added) return false;

                foreach (var posterImage in System.IO.Directory.EnumerateFiles((System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Posters"))))
                {
                    var fileName = new System.IO.FileInfo(posterImage).Name;

                    var newImg = ScriptableObject.CreateInstance<NarrativeContentImageAsset>();
                    newImg.name = $"Custom Poster - {fileName}";
                    var texture = new Texture2D(353, 471);
                    texture.LoadImage(System.IO.File.ReadAllBytes(posterImage));

                    var newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
                    typeof(NarrativeContentImageAsset).GetField("m_Content", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newImg, newSprite);
                    typeof(NarrativeContentAsset).GetField("m_ContentIcon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newImg, newSprite);
                    typeof(NarrativeContentImageAsset).GetField("m_ContentDescription", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newImg, "Pie's test image");
                    typeof(NarrativeContentAsset).GetField("m_ContentDisplayName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newImg, "Pie's test image");
                    __result.Add(newImg);

                    CustomPostersAndStickers.LoggerInstance.LogInfo($"Loaded Custom Poster: {fileName}");
                }

                CustomPostersAndStickers.LoggerInstance.LogInfo("Custom Posters Loaded");

                added = true;

                return false;
            }
        }

        [HarmonyPatch(typeof(MainSettings), "StickerSettings", MethodType.Getter)]
        public class MainSettings_StickerSettings
        {
            public static bool added = false;

            public static bool Prefix(MainSettings __instance, ref StickerSettings __result)
            {
                if (!Settings.settings.customStickersEnabled) return true;

                var levelDataFI = typeof(MainSettings).GetField("m_StickerSettingsAsset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var levelData = ((StickerSettingsAsset)levelDataFI.GetValue(__instance))?.Data;
                __result = levelData;

                if (added) return false;

                var stickerCollectionAsset = ScriptableObject.CreateInstance<StickerCollectionAsset>();
                typeof(StickerCollectionAsset).GetField("m_CollectionName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(stickerCollectionAsset, "Custom Sticker Collection");
                var newStickerList = new List<StickerAsset>();
                typeof(StickerCollectionAsset).GetField("m_StickerList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(stickerCollectionAsset, newStickerList);
                __result.StickerCollection.Add(stickerCollectionAsset);

                stickerCollectionAsset.StickerList.AddRange(CustomPostersAndStickers.CustomStickers);

                // For each custom sticker, it needs an unlockable entry. Hardcoding to use "Jack Me Up" for now
                foreach (var content in __result.StickerCollection)
                {
                    foreach (var sticker in content.StickerList)
                    {

                        if (sticker.name == "DetailOriented_Velocity_StickerAsset")
                        {
                            foreach(var newSticker in CustomPostersAndStickers.CustomStickers)
                            {
                                typeof(StickerAsset).GetField("m_Objective", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newSticker, sticker.Objective);
                                typeof(StickerAsset).GetField("m_Size", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(newSticker, sticker.Size);
                            }
                        }
                    }
                }

                CustomPostersAndStickers.LoggerInstance.LogInfo("Sticker Override Main Settings");

                added = true;

                return false;
            }
        }

        [HarmonyPatch(typeof(AssetSaveKeyConfigAsset), "AssetSaveKeys", MethodType.Getter)]
        public class AssetSaveKeyConfigAsset_AssetSaveKeys
        {
            public static bool added = false;

            public static bool Prefix(AssetSaveKeyConfigAsset __instance, ref List<AssetSaveKeyConfigAsset.AssetSaveKeyEntry> __result)
            {
                if (!Settings.settings.customStickersEnabled) return true;

                var levelDataFI = typeof(AssetSaveKeyConfigAsset).GetField("m_AssetSaveKeys", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var levelData = (List<AssetSaveKeyConfigAsset.AssetSaveKeyEntry>)levelDataFI.GetValue(__instance);
                __result = levelData;

                if (added) return false;

                foreach(var sticker in CustomPostersAndStickers.CustomStickers)
                {
                    __result.Add(new AssetSaveKeyConfigAsset.AssetSaveKeyEntry()
                    {
                        Asset = sticker,
                        SaveKey = sticker.name,
                        AssetGUID = CustomPostersAndStickers.CustomStickerGuidMap[sticker],
                        IsAddressable = false
                    });
                }

                CustomPostersAndStickers.LoggerInstance.LogInfo("Sticker Override AssetSaveKeyConfigAsset");

                added = true;
                return false;
            }
        }
    }
}
