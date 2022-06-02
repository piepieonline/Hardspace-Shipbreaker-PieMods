using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using BBI.Unity.Game;
using System.Reflection;
using System.Linq;

namespace UpgradeCostAdjuster
{
    // Price adjustment
    // TODO: Could potentially move into an event on the main plugin, rather than a patch
    [HarmonyPatch(typeof(Main), "StartSession")]
    public class Main_StartSession
    {
        static bool hasPatched = false;

        [HarmonyPrefix]
        public static void Prefix(Main __instance)
        {
            if (Settings.settings.debugLogChanges)
                UpgradeCostAdjuster.LoggerInstance.LogInfo($"Trying to patch costs. Already patched: {hasPatched}");

            if (hasPatched) return;

            var currencyAssets = Resources.FindObjectsOfTypeAll<CurrencyAsset>();
            var ltAsset = currencyAssets.Where(currAsset => currAsset.name == "LT_CurrencyAsset").First();
            var creditsAsset = currencyAssets.Where(currAsset => currAsset.name == "Credits_CurrencyAsset").First();

            // Cache the reflection access
            FieldInfo UpgradeAsset_Price = typeof(UpgradeAsset).GetField("m_Price", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo UpgradePrice_Amount = typeof(UpgradePrice).GetField("m_Amount", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo UpgradePrice_CurrencyAsset = typeof(UpgradePrice).GetField("m_CurrencyAsset", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (UpgradeAsset asset in Resources.FindObjectsOfTypeAll<UpgradeAsset>())
            {
                bool hasSpecificUpgradeCosts = Settings.settings.upgradeCosts.TryGetValue(asset.name, out var specificUpgradeCosts);
                bool hasSpecificRequiredLevelOverride = hasSpecificUpgradeCosts && specificUpgradeCosts.requiredLevel >= 0;
                bool hasSpecificRequiredLTOverride = hasSpecificUpgradeCosts && specificUpgradeCosts.prices["LT_CurrencyAsset"] >= 0;
                bool hasSpecificRequiredCreditOverride = hasSpecificUpgradeCosts && specificUpgradeCosts.prices["Credits_CurrencyAsset"] >= 0;

                if (Settings.settings.globalRequiredLevelDefaultOverride >= 0 || hasSpecificRequiredLevelOverride)
                {
                    int originalTier = asset.RequiredTier;
                    FieldInfo m_RequiredTier = typeof(UpgradeAsset).GetField("m_RequiredTier", BindingFlags.NonPublic | BindingFlags.Instance);
                    m_RequiredTier.SetValue(asset, hasSpecificRequiredLevelOverride ? specificUpgradeCosts.requiredLevel : Settings.settings.globalRequiredLevelDefaultOverride);

                    if (Settings.settings.debugLogChanges)
                        UpgradeCostAdjuster.LoggerInstance.LogInfo($"Changed {asset.name} required level from: {originalTier} to {asset.RequiredTier}");
                }

                if (Settings.settings.globalLTCostMultiplier >= 0 || Settings.settings.globalLTToCreditFactor >= 0 || hasSpecificRequiredLTOverride || hasSpecificRequiredCreditOverride)
                {
                    if (asset.Price.Length != 1 || asset.Price[0].CurrencyAsset != ltAsset || Settings.settings.excludedBecauseBroken.Contains(asset.name))
                    {
                        // We know some unlocks shouldn't be touched at all, listed them in settings
                        if (!Settings.settings.excludedBecauseBroken.Contains(asset.name))
                            UpgradeCostAdjuster.LoggerInstance.LogWarning($"Wrong currency for upgrade {asset.name}! Maybe there was a game update?");
                        continue;
                    }

                    UpgradePrice ltPrice = asset.Price[0];
                    UpgradePrice creditPrice = new UpgradePrice();
                    int originalPrice = ltPrice.Amount;
                    if (hasSpecificRequiredLTOverride)
                    {
                        UpgradePrice_Amount.SetValue(ltPrice, (int)(specificUpgradeCosts.prices["LT_CurrencyAsset"]));
                    }
                    else if (Settings.settings.globalLTCostMultiplier >= 0)
                    {
                        UpgradePrice_Amount.SetValue(ltPrice, (int)(ltPrice.Amount * Settings.settings.globalLTCostMultiplier));
                    }

                    var newPrices = new UpgradePrice[] { ltPrice };

                    if (hasSpecificRequiredCreditOverride || Settings.settings.globalLTToCreditFactor >= 0)
                    {
                        if (hasSpecificRequiredCreditOverride)
                        {
                            UpgradePrice_Amount.SetValue(creditPrice, (int)(specificUpgradeCosts.prices["Credits_CurrencyAsset"]));
                        }
                        else if (Settings.settings.globalLTToCreditFactor >= 0)
                        {
                            UpgradePrice_Amount.SetValue(creditPrice, (int)(ltPrice.Amount * Settings.settings.globalLTToCreditFactor));
                        }

                        UpgradePrice_CurrencyAsset.SetValue(creditPrice, creditsAsset);
                        newPrices = new UpgradePrice[] { ltPrice, creditPrice };
                    }

                    UpgradeAsset_Price.SetValue(asset, newPrices);

                    if (Settings.settings.debugLogChanges)
                        UpgradeCostAdjuster.LoggerInstance.LogInfo($"Changed {asset.name} from: LT {originalPrice} to {string.Join(" ", newPrices.Select(price => $"{price.CurrencyAsset.name.Split('_')[0]} {price.Amount}"))}");
                }
            }
            hasPatched = true;
            UpgradeCostAdjuster.LoggerInstance.LogInfo($"Costs changed");
        }
    }

    // Adjust the purchasing screen orange rectangle to match the number of lines
    [HarmonyPatch(typeof(PriceListController), "BuildPriceList")]
    public class PriceListController_BuildPriceList
    {
        static float initialY = -1;

        [HarmonyPostfix]
        public static void Postfix(PriceListController __instance, UpgradePrice[] ___mPrices)
        {
            var priceListRect = __instance.GetComponent<RectTransform>();

            if (initialY < 0) initialY = priceListRect.sizeDelta.y;

            priceListRect.sizeDelta = new Vector2(priceListRect.sizeDelta.x, initialY * ___mPrices.Length);
        }
    }

    // For credits, don't display the current amount, just the price
    [HarmonyPatch(typeof(UpgradePriceLabel), nameof(UpgradePriceLabel.Price), MethodType.Setter)]
    public class UpgradePriceLabel_SetPrice
    {
        static CurrencyAsset creditsAsset;

        [HarmonyPostfix]
        public static void Postfix(UpgradePriceLabel __instance, UpgradePrice ___mPrice, TMPro.TextMeshProUGUI ___m_CurrentCurrencyText, TMPro.TextMeshProUGUI ___m_RequiredCurrencyText)
        {
            if (creditsAsset == null)
            {
                creditsAsset = Resources.FindObjectsOfTypeAll<CurrencyAsset>().Where(currAsset => currAsset.name == "Credits_CurrencyAsset").First();
            }

            if (___mPrice.CurrencyAsset == creditsAsset)
            {
                foreach(Transform childTransform in ___m_CurrentCurrencyText.transform.parent)
                {
                    if(childTransform == ___m_RequiredCurrencyText.transform)
                    {
                        ___m_RequiredCurrencyText.text = "$" + ___m_RequiredCurrencyText.text;
                    }
                    else
                    {
                        childTransform.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    class UpgradePriceOverride : UpgradePrice
    {
        public CurrencyAsset m_CurrencyAsset;
        public int m_Amount;

        public UpgradePriceOverride(CurrencyAsset currencyAsset, int amount)
        {
            m_CurrencyAsset = currencyAsset;
            m_Amount = amount;
        }

        public new CurrencyAsset CurrencyAsset => m_CurrencyAsset;
        public new int Amount => m_Amount;
    }
}
