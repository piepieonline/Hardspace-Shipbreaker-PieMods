using BBI.Unity.Game;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PieChallengeMode
{
    class PieChallengeModeHooks
    {
        // If we load the free play mode, turn it into challenge mode
        [HarmonyPatch(typeof(SceneLoader), "TearDownAndLoadLevelAsync")]
        public class SceneLoader_TearDownAndLoadLevelAsync
        {
            [HarmonyPrefix]
            public static void Prefix(ref LevelAsset.LevelData levelData)
            {
                if(levelData.SessionType == GameSession.SessionType.FreeMode && PieChallengeMode.IsNextSessionChallengeMode)
                {
                    levelData.SessionType = GameSession.SessionType.Challenge;
                }
            }
        }

        // If we load the free play mode, turn it into challenge mode
        [HarmonyPatch(typeof(WorkOrderUIController), "TrySendObjectiveUpdatedNotification")]
        public class WorkOrderUIController_TrySendObjectiveUpdatedNotification
        {
            public static Dictionary<AssetTypeID<ObjectiveEntry>, (bool complete, string name)> successfulObjectives = new Dictionary<AssetTypeID<ObjectiveEntry>, (bool complete, string name)>();

            [HarmonyPrefix]
            public static void Prefix(ref BaseObjectiveData objective, string name)
            {
                if (GameSession.CurrentSessionType == GameSession.SessionType.Challenge)
                {
                    if(successfulObjectives.ContainsKey(objective.EntryCreatedFrom))
                    {
                        if(name == "")
                        {
                            Console.WriteLine($"Warning, unknown objective name for: {successfulObjectives[objective.EntryCreatedFrom].name}");
                            successfulObjectives[objective.EntryCreatedFrom] = (objective.IsComplete, successfulObjectives[objective.EntryCreatedFrom].name);
                        }
                        else
                        {
                            successfulObjectives[objective.EntryCreatedFrom] = (objective.IsComplete, name);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(LevelSelectController), "LevelAssetLoadComplete")]
        public class LevelSelectController_LevelAssetLoadComplete
        {
            [HarmonyPrefix]
            public static void Prefix(ref AsyncOperationHandle<IList<LevelAsset>> levelHandle)
            {
                GameObject.Find("Mode Text").GetComponent<TMPro.TextMeshProUGUI>().text = PieChallengeMode.IsNextSessionChallengeMode ? "Challenge Mode" : "Free Play";
                if (levelHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    // TODO
                    var list = new List<LevelAsset>(levelHandle.Result).Select(levelAsset => levelAsset.Data.StartingShipRef.AssetGUID);

                    var ships = Resources.FindObjectsOfTypeAll<BBI.Unity.Game.ModuleConstructionAsset>().Where(mca => list.Contains(mca.AssetGUID)).ToList();
                }
            }
        }

        // If we end the game by timing out, change the session type back
        [HarmonyPatch(typeof(LevelCompleteEvent), "GetEvent")]
        public class LevelCompleteEvent_GetEvent
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                var levelData = SceneLoader.Instance.CurrentLevelData;
                if (levelData.SessionType == GameSession.SessionType.Challenge)
                {
                    levelData.SessionType = GameSession.SessionType.FreeMode;
                    PropertyInfo property = typeof(SceneLoader).GetProperty("CurrentLevelData");
                    property.GetSetMethod(true).Invoke(SceneLoader.Instance, new object[] { levelData });

                    PieChallengeMode.WasLastSessionChallengeMode = true;
                }
            }
        }

        // If we end the game with the Hab or Pause menu, change the session type back
        [HarmonyPatch(typeof(PromptPopup), "Confirm")]
        public class PromptPopup_Confirm
        {
            [HarmonyPrefix]
            public static void Prefix(PromptPopupData ___mPromptData)
            {
                Console.WriteLine(___mPromptData.ConfirmGameState);

                var levelData = SceneLoader.Instance.CurrentLevelData;
                if(levelData.SessionType == GameSession.SessionType.Challenge)
                {
                    levelData.SessionType = GameSession.SessionType.FreeMode;
                    PropertyInfo property = typeof(SceneLoader).GetProperty("CurrentLevelData");
                    property.GetSetMethod(true).Invoke(SceneLoader.Instance, new object[] { levelData });

                    PieChallengeMode.WasLastSessionChallengeMode = true;
                }
            }
        }

        // End game screen
        [HarmonyPatch(typeof(PostMissionScreen), "UpdateCertXPTotals")]
        public class PostMissionScreen_UpdateCertXPTotals
        {
            [HarmonyPrefix]
            public static bool Prefix(ref int ___mCurrentTab, TextMeshProUGUI ___m_LTFromTiers)
            {
                if(PieChallengeMode.WasLastSessionChallengeMode)
                {
                    foreach (var objective in WorkOrderUIController_TrySendObjectiveUpdatedNotification.successfulObjectives)
                    {
                        if (Settings.settings.debugLog)
                            Console.WriteLine($"{objective.Value.name} was {(objective.Value.complete ? "Passed" : "Failed")}");

                        var newLine = GameObject.Instantiate(___m_LTFromTiers.transform.parent.parent.gameObject, ___m_LTFromTiers.transform.parent.parent.parent);
                        foreach (var textComp in newLine.GetComponentsInChildren<TextMeshProUGUI>())
                        {
                            if (textComp.transform.name == "Label")
                            {
                                textComp.text = objective.Value.name.Replace("Salvage ", "");
                                textComp.rectTransform.sizeDelta = new Vector2(textComp.rectTransform.sizeDelta.x * 2, textComp.rectTransform.sizeDelta.y);
                                textComp.rectTransform.anchoredPosition = new Vector2(620, 0);
                            }
                            else if (textComp.transform.name == "LT")
                            {
                                textComp.text = objective.Value.complete ? "Passed" : "Failed";
                                textComp.color = objective.Value.complete ? Color.green : Color.red;
                                textComp.rectTransform.sizeDelta = new Vector2(textComp.rectTransform.sizeDelta.x * 2, textComp.rectTransform.sizeDelta.y);
                            }
                            else
                            {
                                UnityEngine.Object.Destroy(textComp.transform.gameObject);
                            }
                        }

                        newLine.transform.GetChild(1).gameObject.SetActive(false);
                    }

                    ___m_LTFromTiers.transform.parent.parent.parent.GetChild(0).gameObject.SetActive(false);
                    ___m_LTFromTiers.transform.parent.parent.parent.GetChild(2).gameObject.SetActive(false);

                    ___m_LTFromTiers.transform.parent.parent.parent.parent.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = "SALVAGE GOALS";

                    PieChallengeMode.WasLastSessionChallengeMode = false;

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ShipPreview), "GatherModuleGroupData")]
        public class ShipPreview_GatherModuleGroupData
        {
            public static Dictionary<ShipPreview, ModuleGroupSummary> ModuleGroupSummaries = new Dictionary<ShipPreview, ModuleGroupSummary>();

            [HarmonyPrefix]
            public static void Prefix(ShipPreview __instance, ModuleGroupSummary moduleGroupSummary, ShipPreview.OptionalCreateParams optionalParams)
            {
                // TODO: Memory leak!
                ModuleGroupSummaries[__instance] = moduleGroupSummary;
            }
        }
    }
}
