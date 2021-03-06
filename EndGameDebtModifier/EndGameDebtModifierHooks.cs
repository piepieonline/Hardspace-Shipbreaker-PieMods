using BBI.Unity.Game;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EndGameDebtModifier
{
    class EndGameDebtModifierHooks
    {
        [HarmonyPatch(typeof(PlayerProfileSaveLoadManager), "SerializePlayerProfile")]
        public class PlayerProfileSaveLoadManager_SerializePlayerProfile
        {
            [HarmonyPrefix]
            public static void Prefix(ref PlayerProfile playerProfile)
            {
                if (Settings.settings.debugLog)
                    Console.WriteLine($"Checking debt before saving - {playerProfile.ProfileName}");
                if (playerProfile.DebtPaidOff && EndGameDebtModifier.HasAddedDebt[playerProfile])
                {
                    Console.WriteLine($"Removing debt before saving - {playerProfile.ProfileName}");
                    playerProfile.CurrencyController.ChangeCurrency(playerProfile.DifficultyMode.DebtInterestAsset.Data.Currency.ID, (float)playerProfile.DifficultyMode.StartingDebtAmount, true);
                    EndGameDebtModifier.HasAddedDebt[playerProfile] = false;
                }
			}

            [HarmonyPostfix]
            public static void Postfix(ref PlayerProfile playerProfile)
            {
                if (Settings.settings.debugLog)
                    Console.WriteLine($"Checking debt after saving - {playerProfile.ProfileName}");
                if (playerProfile.DebtPaidOff && (!EndGameDebtModifier.HasAddedDebt.ContainsKey(playerProfile) || !EndGameDebtModifier.HasAddedDebt[playerProfile]))
                {
                    Console.WriteLine($"Adding debt after saving - {playerProfile.ProfileName}");
                    playerProfile.CurrencyController.ChangeCurrency(playerProfile.DifficultyMode.DebtInterestAsset.Data.Currency.ID, (float)playerProfile.DifficultyMode.StartingDebtAmount, false);
                    EndGameDebtModifier.HasAddedDebt[playerProfile] = true;
                }
            }
        }
        
        [HarmonyPatch(typeof(PlayerProfileSaveLoadManager), "DeserializePlayerProfile")]
        public class PlayerProfileSaveLoadManager_DeserializePlayerProfile
        {
            [HarmonyPostfix]
            public static void Postfix(ref PlayerProfile playerProfile)
            {
                if (Settings.settings.debugLog)
                    Console.WriteLine($"Checking debt after loading - {playerProfile.ProfileName}");
                if (playerProfile.DebtPaidOff && (!EndGameDebtModifier.HasAddedDebt.ContainsKey(playerProfile) || !EndGameDebtModifier.HasAddedDebt[playerProfile]))
                {
                    Console.WriteLine($"Adding debt after loading - {playerProfile.ProfileName}");
                    playerProfile.CurrencyController.ChangeCurrency(playerProfile.DifficultyMode.DebtInterestAsset.Data.Currency.ID, (float)playerProfile.DifficultyMode.StartingDebtAmount, false);
                    EndGameDebtModifier.HasAddedDebt[playerProfile] = true;
                }
                
            }
        }
    }
}
