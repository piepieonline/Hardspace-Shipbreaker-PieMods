using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using BBI.Unity.Game;
using System.Reflection;
using System.Linq;

namespace UpgradeCostAdjuster
{
    [HarmonyPatch(typeof(ScriptableObject), MethodType.Constructor)]
    public class ScriptableObject_ScriptableObject
    {
        public static List<UpgradeAsset> UpgradeAssets = new List<UpgradeAsset>();

        [HarmonyPrefix]
        public static void Postfix(ScriptableObject __instance)
        {
            if (__instance.GetType() == typeof(UpgradeAsset))
            {
                UpgradeAssets.Add((UpgradeAsset)__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Main), "StartSession")]
    public class Main_StartSession
    {
        static bool hasPatched = false;

        [HarmonyPrefix]
        public static void Prefix(Main __instance)
        {
            if (hasPatched) return;
            foreach (UpgradeAsset asset in ScriptableObject_ScriptableObject.UpgradeAssets)
            {
                foreach (var price in asset.Price)
                {
                    FieldInfo amountField = typeof(UpgradePrice).GetField("m_Amount", BindingFlags.NonPublic | BindingFlags.Instance);
                    var prevPrice = price.Amount;

                    if (Settings.settings.upgradeCosts.ContainsKey(asset.name) && Settings.settings.upgradeCosts[asset.name] >= 0)
                    {
                        amountField.SetValue(price, (int)(Settings.settings.upgradeCosts[asset.name]));
                    }
                    else if (Settings.settings.globalCostMultiplier >= 0)
                    {
                        amountField.SetValue(price, (int)(price.Amount * Settings.settings.globalCostMultiplier));
                    }

                    if (Settings.settings.debugLogChanges)
                        Console.WriteLine($"Price for {asset.name}: Before: {prevPrice} After: {price.Amount}");
                }
            }
            hasPatched = true;
            Console.WriteLine($"Costs changed");
        }
    }
}
