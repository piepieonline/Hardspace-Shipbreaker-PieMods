using BepInEx;
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
        private void Awake()
        {
            try
            {
                BBI.Unity.Game.Main.EventSystem.AddHandler<BBI.Unity.Game.RoomDecompressedEvent>(evt =>
                {
                    Logger.LogInfo($"evt");
                    Logger.LogInfo($"Decomp force: {evt.DecompressionForce}");
                });
            } catch (System.Exception ex)
            {
                Logger.LogInfo(ex.Message);
            }

            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                Addressables.LoadContentCatalogAsync((System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "FirstShip", "catalog.json")));

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }
    }
}
