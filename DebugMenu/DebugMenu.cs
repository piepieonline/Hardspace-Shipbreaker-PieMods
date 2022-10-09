using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using System.Linq;
using UnityEngine;

namespace DebugMenu
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class DebugMenu : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
                this.enabled = false;
            }
        }

        private void Update()
        {
            // Only swap on the menu screen
            if (GameSession.CurrentGameState == GameSession.GameState.Gameplay && Input.GetKeyDown(Settings.settings.activateMouseKey))
            {
                if(Cursor.visible == false || Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.Confined;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }
}
