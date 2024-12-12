using BBI.Unity.Game;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TestProj
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LoggerInstance;

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                LoggerInstance = Logger;
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                try
                {
                    Logger.LogInfo($"Adding handler");
                    BBI.Unity.Game.Main.EventSystem.AddHandler<BBI.Unity.Game.RoomDecompressedEvent>(evt =>
                    {
                        Logger.LogInfo($"evt");
                        Logger.LogInfo($"Decomp force: {evt.DecompressionForce}");
                    });
                    Logger.LogInfo($"Handler done");
                }
                catch (System.Exception ex)
                {
                    Logger.LogInfo($"Handler failed");
                    Logger.LogInfo(ex.Message);
                }

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }
    }
}
