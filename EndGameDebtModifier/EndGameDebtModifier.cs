using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using System.Collections.Generic;

namespace EndGameDebtModifier
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class EndGameDebtModifier : BaseUnityPlugin
    {
        public static Dictionary<PlayerProfile, bool> HasAddedDebt = new Dictionary<PlayerProfile, bool>();

        private void Awake()
        {
            Settings.Load();
            if (Settings.settings.enabled)
            {
                new Harmony("com.piepieonline.challengemode").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is patched!");
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }
    }
}
