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
            LoggerInstance = Logger;
            new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is patched!");
            Settings.Load();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }
}
