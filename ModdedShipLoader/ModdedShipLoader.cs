using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ModdedShipLoader
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ModdedShipLoader : BaseUnityPlugin
    {
        public const int MANIFEST_VERSION = 1;

        public static BepInEx.Logging.ManualLogSource LoggerInstance;

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                LoggerInstance = Logger;
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                List<string> overrideKeys = new List<string>();

                Logger.LogInfo($"Loading Custom Ships");
                foreach (var dir in System.IO.Directory.EnumerateDirectories((System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Ships"))))
                {
                    var folderName = new System.IO.DirectoryInfo(dir).Name;
                    var shipName = folderName.Split('.')[0];
                    Logger.LogInfo($"Loading {shipName} by {folderName.Split('.')[1]}");

                    var catalogPath = Path.Combine(dir, "catalog.json");
                    var tempCatalogPath = catalogPath + ".temp";

                    if(!File.Exists(Path.Combine(dir, "manifest.json")))
                    {
                        Logger.LogWarning($"ERROR: {shipName} is missing a manifest. Skipping");
                        continue;
                    }

                    var manifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(Path.Combine(dir, "manifest.json")));

                    if (manifest.version > MANIFEST_VERSION)
                    {
                        Logger.LogWarning($"ERROR: {shipName} is on a newer manifest ({manifest.version} > {MANIFEST_VERSION}) version. Skipping");
                        continue;
                    }
                    else if (manifest.version < MANIFEST_VERSION)
                    {
                        Logger.LogInfo($"WARN: {shipName} is on an older manifest ({manifest.version} < {MANIFEST_VERSION}) version. It may not work correctly.");
                    }

                    // Create the temp catalog
                    File.WriteAllText(
                        tempCatalogPath,
                        // Change the path from the author's machine to this machine
                        System.Text.RegularExpressions.Regex.Replace(
                            System.IO.File.ReadAllText(catalogPath),
                            // Look for a full path (starting with a drive, ending with .bundle file type)
                            @"""[A-Za-z]\:.*\\(\w+\.bundle)"",",
                            // Replace it with the current directory, fix up escaped backslashes
                            $"\"{Path.Combine(dir, "$+").Replace("\\", "\\\\")}\","
                        )
                    );

                    overrideKeys.AddRange(manifest.baseOverrides);
                    
                    Addressables.LoadContentCatalogAsync(tempCatalogPath).Completed += _ =>
                    {
                        System.IO.File.Delete(tempCatalogPath);

                        // Force load modded levels - the game prewarms, so we need to as well
                        Addressables.LoadResourceLocationsAsync(new string[] { "ModdedLevel" }).Completed += list =>
                        {
                            foreach (var location in list.Result)
                            {
                                Addressables.LoadAssetAsync<BBI.Unity.Game.ModuleConstructionAsset>(location);
                            }
                        };
                    };
                }

                ModdedShipLoaderHooks.Addressables_LoadAssetAsync.assetKeys = overrideKeys.ToArray();

                Logger.LogInfo($"Custom Ships Loaded");

                Main.EventSystem.AddHandler(new Carbon.Core.Events.EventHandler<UIAsyncLoadingEvent>(UpdateLevelNames));

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }

        private static LevelSelectButton[] levelSelectButtons;
        private static int levelSelectEventCount = 0;
        private static void UpdateLevelNames(UIAsyncLoadingEvent ev)
        {
            if (ev.UISectionName == UIAsyncLoadingController.UISectionName.FREEPLAY_LEVEL_SELECT_LIST)
            {
                if (levelSelectButtons == null || levelSelectButtons.Length < 2)
                    levelSelectButtons = FindObjectsOfType<LevelSelectButton>();

                if (levelSelectButtons.Length == levelSelectEventCount)
                {
                    foreach (var button in FindObjectsOfType<LevelSelectButton>())
                    {
                        var levelDataFI = typeof(LevelSelectButton).GetField("mLevelToLoad", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var levelData = (LevelAsset.LevelData)levelDataFI.GetValue(button);
                        if (levelData.LevelDescriptionFull.StartsWith("CUSTOM"))
                        {
                            foreach (var text in button.GetComponentsInChildren<LocalizedTextMeshProUGUI>())
                            {
                                if (text.name == "Ship name")
                                    text.ChangeFieldByStringContent(levelData.LevelDisplayName);
                                else if (text.name == "Ship role")
                                    text.ChangeFieldByStringContent(levelData.LevelDescriptionShort);
                            }

                            if (levelData.StartingUpgrades == null)
                            {
                                levelData.StartingUpgrades = Resources.FindObjectsOfTypeAll<UpgradeListAsset>().Where(asset => asset.name == "FreeMode_UpgradeListAsset").First();
                                levelDataFI.SetValue(button, levelData);
                            }
                        }
                    }

                    levelSelectEventCount = 0;
                    levelSelectButtons = null;
                }
                else
                {
                    levelSelectEventCount++;
                }
            }
        }
    }

    public class Manifest
    {
        public int version;
        public string[] baseOverrides;
    }
}
