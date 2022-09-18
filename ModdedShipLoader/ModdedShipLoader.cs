using BepInEx;
using HarmonyLib;
using UnityEngine.AddressableAssets;

namespace ModdedShipLoader
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ModdedShipLoader : BaseUnityPlugin
    {
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

                Logger.LogInfo($"Loading Custom Ships");
                foreach (var dir in System.IO.Directory.EnumerateDirectories((System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Ships"))))
                {
                    var folderName = new System.IO.DirectoryInfo(dir).Name;
                    Logger.LogInfo($"Loading {folderName.Split('.')[0]} by {folderName.Split('.')[1]}");

                    var catalogPath = System.IO.Path.Combine(dir, "catalog.json");
                    var tempCatalogPath = catalogPath + ".temp";

                    // Create the temp catalog
                    System.IO.File.WriteAllText(
                        tempCatalogPath,
                        // Change the path from the author's machine to this machine
                        System.Text.RegularExpressions.Regex.Replace(
                            System.IO.File.ReadAllText(catalogPath),
                            // Look for a full path (starting with a drive, ending with .bundle file type)
                            @"""[A-Za-z]\:.*\\(\w+\.bundle)"",",
                            // Replace it with the current directory, fix up escaped backslashes
                            $"\"{System.IO.Path.Combine(dir, "$+").Replace("\\", "\\\\")}\","
                        )
                    );
                    
                    Addressables.LoadContentCatalogAsync(tempCatalogPath).Completed += _ =>
                    {
                        System.IO.File.Delete(tempCatalogPath);
                    };
                }

                Logger.LogInfo($"Custom Ships Loaded");

                // Addressables.LoadContentCatalogAsync((System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "FirstShip", "catalog.json")));

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }
    }
}
