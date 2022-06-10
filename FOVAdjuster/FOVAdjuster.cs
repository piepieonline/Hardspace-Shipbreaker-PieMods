using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace FOVAdjuster
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class FOVAdjuster : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource LoggerInstance;
        public static bool needsInit = false;

        private static Dictionary<string, Vector3> initialScales = new Dictionary<string, Vector3>(); 

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled && (Settings.settings.fov > 0 || !Settings.settings.showHelmet))
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

            Main.EventSystem.AddHandler((GameStateChangedEvent ev) =>
            {
                if (ev.GameState == GameSession.GameState.Gameplay)
                {
                    // Shift started
                    if (Settings.settings.debugLogChanges)
                        Logger.LogInfo("Shift started, setting needsInit");
                    needsInit = true;
                }
            });
        }
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.C))
            {
                Settings.Load();
                UpdateFOV();
            }
        }

        public static void UpdateFOV()
        {
            if (Settings.settings.debugLogChanges)
                LoggerInstance.LogInfo("Starting FOV update");
            var cam = UnityEngine.GameObject.Find("Main Camera").GetComponent<Camera>();
            if (cam != null)
            {
                if (Settings.settings.debugLogChanges)
                    LoggerInstance.LogInfo("Changing values");

                Main.EventSystem.Post(Settings.settings.showHelmet ? ToggleHelmetEvent.On(): ToggleHelmetEvent.Off());

                if (Settings.settings.fov > 0)
                {
                    float smashedScale = Mathf.LerpUnclamped(1, Settings.settings.smashScaleAt90, InverseLerpUnclamped(72, 90, Settings.settings.fov));
                    var helm = cam.transform.Find("Helmet_Rig/Helmet_Container/PRF_Default_Helmet(Clone)/Helmet_visor_shell");
                    cam.fieldOfView = Settings.settings.fov;

                    if(!initialScales.ContainsKey("Helmet_visor_shell"))
                    {
                        initialScales["Helmet_visor_shell"] = helm.localScale;
                    }
                    helm.localScale = initialScales["Helmet_visor_shell"] * smashedScale;

                    foreach (var shatterFXTransform in cam.GetComponentsInChildren<Transform>(true).Where(child => Settings.settings.damageVFXGameObjectNames.Contains(child.name)))
                    {
                        if (!initialScales.ContainsKey(shatterFXTransform.name))
                        {
                            initialScales[shatterFXTransform.name] = shatterFXTransform.localScale;
                        }

                        shatterFXTransform.localScale = initialScales[shatterFXTransform.name] * smashedScale;
                    }
                }
            }
        }

        public static float InverseLerpUnclamped(float min, float max, float value)
        {
            return (value - min) / (max - min);
        }
    }
}
