using System;
using System.Collections.Generic;
using System.Linq;
using BBI.Unity.Game;
using HarmonyLib;
using UnityEngine;

namespace PieChallengeMode
{
    [HarmonyPatch(typeof(ShipPreview), "GatherModuleGroupData")]
    public class ShipPreview_GatherModuleGroupData
    {
        public static Dictionary<ShipPreview, ModuleGroupSummary> ModuleGroupSummaries = new Dictionary<ShipPreview, ModuleGroupSummary>();

        [HarmonyPrefix]
        public static void Prefix(ShipPreview __instance, ModuleGroupSummary moduleGroupSummary, ShipPreview.OptionalCreateParams optionalParams)
        {
            Console.WriteLine("ShipPreview_GatherModuleGroupData called");
            ModuleGroupSummaries[__instance] = moduleGroupSummary;
        }
    }

    [HarmonyPatch(typeof(ScriptableObject), MethodType.Constructor)]
    public class ScriptableObject_ScriptableObject
    {
        public static Dictionary<Type, List<UnityEngine.ScriptableObject>> ScriptableObjectsMapping = new Dictionary<Type, List<UnityEngine.ScriptableObject>>();

        [HarmonyPrefix]
        public static void Prefix(ScriptableObject __instance)
        {
            var instanceType = __instance.GetType();
            if (!ScriptableObjectsMapping.ContainsKey(instanceType)) ScriptableObjectsMapping.Add(instanceType, new List<UnityEngine.ScriptableObject>());
            ScriptableObjectsMapping[instanceType].Add(__instance);
        }
    }
}
