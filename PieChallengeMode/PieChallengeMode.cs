using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PieChallengeMode
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shipbreaker.exe")]
    public class PieChallengeMode : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            new Harmony("com.piepieonline.challengemode").PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is patched!");

            Settings.Load();

            Main.EventSystem.AddHandler((ShipSpawningEvent ev) =>
            {
                Logger.LogInfo($"ShipSpawningEvent.SpawnState {ev.State}");
                Logger.LogInfo($"GameSession.CurrentSessionType {GameSession.CurrentSessionType}");

                if (ev.State == ShipSpawningEvent.SpawnState.Complete && GameSession.CurrentSessionType == GameSession.SessionType.FreeMode)
                {
                    Logger.LogInfo("Creating objectives");
                    CreateObjectives(ev.ShipPreview);
                }
            });
        }

        public static Dictionary<string, CollectionObjectiveObjectEntry> collectionObjectiveEntries = new Dictionary<string, CollectionObjectiveObjectEntry>();
        public static Dictionary<string, MassObjectiveCategoryEntry> massObjectiveEntries = new Dictionary<string, MassObjectiveCategoryEntry>();
        public static ShipPreview shipPreview1;
        private void CreateObjectives(ShipPreview shipPreview)
        {
            var orCreateSystem = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<UpdateActiveObjectiveGroupSystem>();
            orCreateSystem.ClearList();

            ObjectiveGroupEntry objective = null;

            foreach (var objectGroupEntry in ScriptableObject_ScriptableObject.ScriptableObjectsMapping[typeof(ObjectiveGroupEntry)])
            {
                if (objectGroupEntry.name == "Default_WEEKLYSHIP_ObjectiveGroupEntry(Please do not remove)")
                {
                    objective = Instantiate((ObjectiveGroupEntry)objectGroupEntry);
                    break;
                }
            }

            if (collectionObjectiveEntries.Count != ScriptableObject_ScriptableObject.ScriptableObjectsMapping[typeof(CollectionObjectiveObjectEntry)].Count)
            {
                collectionObjectiveEntries.Clear();
                foreach (var collectionObj in ScriptableObject_ScriptableObject.ScriptableObjectsMapping[typeof(CollectionObjectiveObjectEntry)])
                {
                    collectionObjectiveEntries.Add(collectionObj.name, (CollectionObjectiveObjectEntry)collectionObj);
                }
            }

            if (massObjectiveEntries.Count != ScriptableObject_ScriptableObject.ScriptableObjectsMapping[typeof(MassObjectiveCategoryEntry)].Count)
            {
                massObjectiveEntries.Clear();
                foreach (var massObj in ScriptableObject_ScriptableObject.ScriptableObjectsMapping[typeof(MassObjectiveCategoryEntry)])
                {
                    massObjectiveEntries.Add(massObj.name, (MassObjectiveCategoryEntry)massObj);
                }
            }

            if (objective != null)
            {
                objective.MassObjectives.Clear();
                objective.CollectionObjectives.Clear();

                var objectInfoAssets = new List<ObjectInfoAsset>();
                var structurePartCategories = new List<CategoryAsset>();

                // Get a list of what is on the ship
                foreach (var summary in ShipPreview_GatherModuleGroupData.ModuleGroupSummaries[shipPreview].moduleSummaries)
                {
                    foreach (var group in summary.Groups)
                    {
                        objectInfoAssets.Add(group.StructurePartAsset.Data.ObjectInfoAsset);
                    }
                    foreach (var part in summary.Parts)
                    {
                        foreach (var cat in part.StructurePartAsset.Data.ObjectInfoAsset.Data.Categories)
                            structurePartCategories.Add(cat);
                    }
                }

                // Get possible collection objectives on this ship
                var possibleCollectionObjectives = Settings.settings.validCollectionKeys.Where(objKey =>
                {
                    return objectInfoAssets.Contains(collectionObjectiveEntries[objKey].ObjectInfo);
                }).ToList();
                // Remove duplicates
                possibleCollectionObjectives = possibleCollectionObjectives.GroupBy(x => collectionObjectiveEntries[x].ObjectInfo).Select(x => x.First()).ToList();


                // Get possible mass objectives on this ship
                var possibleMassObjectives = Settings.settings.validMassKeys.Where((objKey, index) =>
                {
                    return structurePartCategories.Contains(massObjectiveEntries[objKey].CategoryAsset);
                }).ToList();
                // Remove duplicates
                possibleMassObjectives = possibleMassObjectives.GroupBy(x => massObjectiveEntries[x].CategoryAsset).Select(x => x.First()).ToList();

                foreach (var collectionObjective in possibleCollectionObjectives)
                {
                    objective.CollectionObjectives.Add(collectionObjectiveEntries[collectionObjective]);
                }

                foreach (var massObjective in possibleMassObjectives)
                {
                    objective.MassObjectives.Add(massObjectiveEntries[massObjective]);
                }

                orCreateSystem.InsertSingleObjectiveGroup(objective);
            }
        }
    }
}
