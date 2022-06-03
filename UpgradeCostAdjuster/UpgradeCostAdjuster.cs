using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;

namespace UpgradeCostAdjuster
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class UpgradeCostAdjuster : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource LoggerInstance;

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if(Settings.settings.enabled)
            {
                LoggerInstance = Logger;
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }
    }
}
