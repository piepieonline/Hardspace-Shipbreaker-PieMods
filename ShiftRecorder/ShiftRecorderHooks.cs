using BBI.Unity.Game;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftRecorder
{
    internal class ShiftRecorderHooks
    {
        // Debug Menu
        [HarmonyPatch(typeof(GameSession), "InitializeServices")]
        public class GameSession_InitializeServices
        {
            public static void Prefix(ref Carbon.Core.Services.ServiceContext ___mSessionServices)
            {
                ___mSessionServices.AddService<DebugMenu>(new DebugMenu(), true);
            }
        }
    }
}
