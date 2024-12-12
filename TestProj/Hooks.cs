using System;
using System.Collections.Generic;
using System.Linq;
using BBI.Unity.Game;
using BBI.Unity.Game.Gameplay;
using Carbon.Audio;
using Carbon.Core.Services;
using Carbon.Core.Unity;
using HarmonyLib;
using InControl;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TestProj
{
    [HarmonyPatch(typeof(Carbon.Core.Log), "LogMessage")]
    public class Log_LogMessage
    {
        [HarmonyPrefix]
        public static void Prefix(object sender, Carbon.Core.Log.Severity sev, Carbon.Core.Log.Channel channel, string format, params object[] args)
        {
            string message = $"{sev.ToString()}: {((args != null && args.Length != 0) ? string.Format(format, args) : format)}";
            Console.WriteLine(message);

            if (sev == Carbon.Core.Log.Severity.Error)
            {
                try
                {
                    var stackTrace = new System.Diagnostics.StackTrace(2, true);
                    System.IO.File.WriteAllText("D:\\exception.txt", stackTrace.GetFrame(0).GetType() + "\r\n" + stackTrace.ToString());
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(PlayableArea), "GetPlayableAreaState")]
    public class PlayableAreaState_PlayableAreaState
    {
        public static bool Prefix(ref PlayableArea.PlayableAreaState __result)
        {
            __result = PlayableArea.PlayableAreaState.Safe;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameSession), "InitializeServices")]
    public class GameSession_InitializeServices
    {
        public static void Prefix(ref Carbon.Core.Services.ServiceContext ___mSessionServices)
        {
            // return;

            // ___mSessionServices.AddService<DebugAudioService>(new DebugAudioService(__mSettings), true);
            // ___mSessionServices.AddService<DebugCrashTestService>(new DebugCrashTestService(), true);
            // ___mSessionServices.AddService<DebugDurabilityService>(new DebugDurabilityService(), true);
            ___mSessionServices.AddService<DebugEntityService>(new DebugEntityService(), true);
            // ___mSessionServices.AddService<DebugGlitchService>(new DebugGlitchService(), true);
            ___mSessionServices.AddService<DebugHauntingService>(new DebugHauntingService(), true);
            // ___mSessionServices.AddService<DebugHistogramService>(new DebugHistogramService(), true); // Already exists
            ___mSessionServices.AddService<DebugJointService>(new DebugJointService(), true);
            // ___mSessionServices.AddService<DebugLightAndPostService>(new DebugLightAndPostService(), true);
            // ___mSessionServices.AddService<DebugLocalizationService>(new DebugLocalizationService(), true);
            ___mSessionServices.AddService<DebugLynxAchievementService>(new DebugLynxAchievementService(), true);
            ___mSessionServices.AddService<DebugMemoryService>(new DebugMemoryService(), true);
            ___mSessionServices.AddService<DebugMonoBehaviourService>(new DebugMonoBehaviourService(), true);
            ___mSessionServices.AddService<DebugNarrativeService>(new DebugNarrativeService(), true);
            ___mSessionServices.AddService<DebugObjectStatisticsService>(new DebugObjectStatisticsService(), true);
            ___mSessionServices.AddService<DebugOnlineService>(new DebugOnlineService(), true);
            ___mSessionServices.AddService<DebugPhysicsService>(new DebugPhysicsService(), true);
            // ___mSessionServices.AddService<DebugPlayerActionTrackerService>(new DebugPlayerActionTrackerService(), true);
            // ___mSessionServices.AddService<DebugPlayerHealthService>(new DebugPlayerHealthService(__mSettings), true);
            ___mSessionServices.AddService<DebugPlayerProfileCreateService>(new DebugPlayerProfileCreateService(), true);
            ___mSessionServices.AddService<DebugPlayerService>(new DebugPlayerService(), true);
            ___mSessionServices.AddService<DebugProfilerCaptureService>(new DebugProfilerCaptureService(), true);
            ___mSessionServices.AddService<DebugQualitySettingsService>(new DebugQualitySettingsService(), true);
            ___mSessionServices.AddService<DebugReactorService>(new DebugReactorService(), true);
            // ___mSessionServices.AddService<DebugRenderingFeatureService>(new DebugRenderingFeatureService(), true);
            ___mSessionServices.AddService<DebugSaveLoadService>(new DebugSaveLoadService(), true);
            ___mSessionServices.AddService<DebugShiftService>(new DebugShiftService(), true);
            ___mSessionServices.AddService<DebugShipService>(new DebugShipService(), true);
            ___mSessionServices.AddService<DebugSpawnPoolService>(new DebugSpawnPoolService(), true);
            ___mSessionServices.AddService<DebugStickerService>(new DebugStickerService(), true);
            ___mSessionServices.AddService<DebugTelemetryService>(new DebugTelemetryService(), true);
            ___mSessionServices.AddService<DebugTeleportService>(new DebugTeleportService(), true);
            ___mSessionServices.AddService<DebugTutorialObjectives>(new DebugTutorialObjectives(), true);
            ___mSessionServices.AddService<DebugUIService>(new DebugUIService(), true);
            // ___mSessionServices.AddService<DebugUpgradesService>(new DebugUpgradesService(__mSettings), true);

            // Custom
            ___mSessionServices.AddService<DebugMenu>(new DebugMenu(), true);
        }
    }

    /*
    [HarmonyPatch(typeof(TriggerableSpeechControlBehaviour), "OnBehaviourPlay")]
    public class TriggerableSpeechControlBehaviour_OnBehaviourPlay
    {
        public static void Prefix(TriggerableSpeechAsset ___mTriggerableSpeechAsset)
        {
            // Debug.Log($"Speech triggered: {___mTriggerableSpeechAsset.name}");
            // Debug.Log(Environment.StackTrace);
        }
    
    [HarmonyPatch(typeof(TriggerableSpeechControlBehaviour), "InitializeSpeechControlData")]
    public class TriggerableSpeechControlBehaviour_InitializeSpeechControlData
    {
        public static void Prefix(TriggerableSpeechAsset speechAsset)
        {
            // Debug.Log($"Speech init: {speechAsset.name}");
            // Debug.Log(Environment.StackTrace);
        }
    }

    [HarmonyPatch(typeof(LynxSpeechEventController), "OnTriggeredSpeechEvent")]
    public class TriggerableSpeechControlAsset_duration
    {
        public static void Prefix(TriggerableSpeechEvent ev)
        {
            Debug.Log($"Speech init:");
            Debug.Log(Environment.StackTrace);
        }
    }
    
    [HarmonyPatch(typeof(PATConditionalTriggerComponent), "OnTrigger")]
    public class PATConditionalTriggerComponent_OnTrigger
    {
        public static void Prefix()
        {
            Debug.Log($"OnTrigger");
        }
    }
    */
}