using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace PieChallengeMode
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shipbreaker.exe")]
    public class PieChallengeMode : BaseUnityPlugin
    {
        public static bool IsNextSessionChallengeMode = false;
        public static bool WasLastSessionChallengeMode = false;

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                if(Settings.settings.alwaysOn)
                {
                    IsNextSessionChallengeMode = true;
                }

                Main.EventSystem.AddHandler((ShipSpawningEvent ev) =>
                {
                    if (ev.State == ShipSpawningEvent.SpawnState.Complete && GameSession.CurrentSessionType == GameSession.SessionType.Challenge)
                    {
                        if (Settings.settings.debugLog)
                            Logger.LogInfo("Creating objectives");
                        CreateObjectives(ev.ShipPreview);
                    }
                });

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }

        private void Update()
        {
            // Only swap on the menu screen
            if (!Settings.settings.alwaysOn && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.C) && UnityEngine.SceneManagement.SceneManager.GetSceneByName("master_frontend").isLoaded)
            {
                IsNextSessionChallengeMode = !IsNextSessionChallengeMode;
                GameObject.Find("Mode Text").GetComponent<TMPro.TextMeshProUGUI>().text = IsNextSessionChallengeMode ? "Challenge Mode" : "Free Play";
                
                if(Settings.settings.debugLog)
                    Logger.LogInfo($"Changed Mode: {(IsNextSessionChallengeMode ? "Challenge" : "Free Play")}");
            }
        }

        private void CreateObjectives(ShipPreview shipPreview)
        {
            var orCreateSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<UpdateActiveObjectiveGroupSystem>();
            orCreateSystem.ClearList();

            // TODO: Need to create new one of these
            ObjectiveGroupEntry objective = Resources.FindObjectsOfTypeAll<ObjectiveGroupEntry>().Where(objectiveGroupEntry => objectiveGroupEntry.name == "Default_WEEKLYSHIP_ObjectiveGroupEntry(Please do not remove)").First();

            PieChallengeModeHooks.WorkOrderUIController_TrySendObjectiveUpdatedNotification.successfulObjectives.Clear();

            // TODO: Create these
            var diffSettings = Resources.FindObjectsOfTypeAll<CollectionObjectiveObjectEntry>().Where((objEntry, index) => index == 36).Select(objEntry => objEntry.DifficultySettings).First();

            if (objective != null)
            {
                objective.MassObjectives.Clear();
                objective.CollectionObjectives.Clear();

                var objectInfoAssets = new List<ObjectInfoAsset>();
                var structurePartCategories = new List<CategoryAsset>();

                // Get a list of what is on the ship
                foreach (var summary in PieChallengeModeHooks.ShipPreview_GatherModuleGroupData.ModuleGroupSummaries[shipPreview].moduleSummaries)
                {
                    foreach (var group in summary.Groups)
                    {
                        objectInfoAssets.Add(group.StructurePartAsset.Data.ObjectInfoAsset);
                    }
                    foreach (var part in summary.Parts)
                    {
                        objectInfoAssets.Add(part.StructurePartAsset.Data.ObjectInfoAsset);
                        foreach (var cat in part.StructurePartAsset.Data.ObjectInfoAsset.Data.Categories)
                            structurePartCategories.Add(cat);
                    }
                }

                var BargeSalv_CategoryAsset = Resources.FindObjectsOfTypeAll<CategoryAsset>().Where(categoryAsset => categoryAsset.name == "BargeSalv_CategoryAsset").First();
                foreach (var objectInfo in
                    // All possible barge objects (TODO: Missing airfilters?)
                    objectInfoAssets.Where(z => z.Data.Categories.AsQueryable().Contains(BargeSalv_CategoryAsset) && !Settings.settings.invalidCollectionObjects.Contains(z.name))
                    // Remove duplicates
                    .GroupBy(x => x.AssetGUID).Select(y => y.First()))
                {
                    if (!Settings.settings.collectionObjects.ContainsKey(objectInfo.name))
                    {
                        Logger.LogWarning($"No parameters found for {objectInfo.name}, and it wasn't excluded! Maybe there was a game update?");
                        continue;
                    }

                    var newObj = UnityEngine.ScriptableObject.CreateInstance<CollectionObjectiveObjectEntry>();
                    newObj.name = $"CollectionObjective{objectInfo.name}";

                    foreach (var a in new Dictionary<string, (Type, object)>()
                    {
                        { "m_AssetGUID", (typeof(TypeAsset), Guid.NewGuid().ToString()) },
                        { "m_DifficultySettings", (typeof(CollectionObjectiveEntry), diffSettings) },
                        { "m_RequestedBy", (typeof(CollectionObjectiveEntry), new CompanyInfoAsset[0]) },
                        { "m_ObjectInfo", (typeof(CollectionObjectiveObjectEntry), objectInfo) }
                    })
                    {
                        System.Reflection.FieldInfo fi = a.Value.Item1.GetField(a.Key, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        fi.SetValue(newObj, a.Value.Item2);
                    }
                    // Force the GUID to generate properly
                    var testId = newObj.ID.AssetGUID;

                    objective.CollectionObjectives.Add(newObj);
                }

                Shuffle(objective.CollectionObjectives);

                int objectivesToKeep = UnityEngine.Random.Range(Settings.settings.minTotalObjectives, Settings.settings.maxTotalObjectives + 1);
                objective.CollectionObjectives.RemoveRange(objectivesToKeep, objective.CollectionObjectives.Count - objectivesToKeep);

                foreach (var obj in objective.CollectionObjectives)
                {
                    PieChallengeModeHooks.WorkOrderUIController_TrySendObjectiveUpdatedNotification.successfulObjectives[obj.ID] = (false, ((CollectionObjectiveObjectEntry)obj).ObjectInfo.name);
                }

                if (Settings.settings.debugLog)
                    Logger.LogInfo($"{objectivesToKeep} objectives created");

                // TODO: Can't hide objectives created this way
                orCreateSystem.InsertSingleObjectiveGroup(objective);

                CalculateTimeForObjective(shipPreview, objective);
            }
        }

        private void CalculateTimeForObjective(ShipPreview shipPreview, ObjectiveGroupEntry objective)
        {
            float completionTime = 300;

            if (!Settings.settings.baseTimePerShipType.TryGetValue(shipPreview.ConstructionAssetName, out completionTime))
            {
                Logger.LogWarning($"No base time setting for {shipPreview.ConstructionAssetName}, using the default (300)! Maybe there was a game update?");
            }

            foreach (var obj in objective.CollectionObjectives)
            {
                string objectName = ((CollectionObjectiveObjectEntry)obj).ObjectInfo.name;
                completionTime += Settings.settings.collectionObjects[objectName].maxPercentTime;
            }

            var levelData = SceneLoader.Instance.CurrentLevelData;
            levelData.TimerCountsUp = false;
            levelData.CompletionTimeInSeconds = completionTime;

            PropertyInfo property = typeof(SceneLoader).GetProperty("CurrentLevelData");
            property.GetSetMethod(true).Invoke(SceneLoader.Instance, new object[] { levelData });

            if (Settings.settings.debugLog)
                Logger.LogInfo($"Shift timer set to {completionTime}");
        }

        private static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            var rng = new System.Random();
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}

/* Removed: Mass objectives (don't make sense to have...)
// Get possible mass objectives on this ship
var possibleMassObjectives = Settings.settings.validMassKeys.Where((objKey, index) =>
{
    return structurePartCategories.Contains(massObjectiveEntries[objKey].CategoryAsset);
}).ToList();
// Remove duplicates
possibleMassObjectives = possibleMassObjectives.GroupBy(x => massObjectiveEntries[x].CategoryAsset).Select(x => x.First()).ToList();

foreach (var massObjective in possibleMassObjectives)
{
    objective.MassObjectives.Add(massObjectiveEntries[massObjective]);
}
*/