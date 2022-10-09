using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using BBI.Unity.Game;
using Carbon.Core.Unity;

namespace DebugMenu
{
    class DebugMenuHooks
    {
        [HarmonyPatch(typeof(Main), "InitializeDebugServices")]
        public class Main_InitializeDebugServices
        {
            public static void Postfix(ref Carbon.Core.Services.ServiceContext ___mGlobalServices, Main __instance)
            {
                DebugService debugService = new DebugService();
                ___mGlobalServices.AddService(debugService);
            }
        }

        [HarmonyPatch(typeof(LynxPlayerActionSetBase), "GetPlayerActionFromId")]
        public class LynxPlayerActionSetBase_GetInputControlFromId
        {
            public static bool Prefix(int id)
            {
                // Override, so we don't throw errors because of the unknown debug console control
                return id != 20310;
            }
        }


        [HarmonyPatch(typeof(ButtonTextSelectionOptions), "Awake")]
        public class ButtonTextSelectionOptions_Awake
        {
            [HarmonyPostfix]
            public static void Postfix(ButtonTextSelectionOptions __instance)
            {
                if(__instance.name == "Button - WEEKLY SHIP")
                    __instance.gameObject.SetActive(false);
            }
        }
    }
}
