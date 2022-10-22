using BBI.Unity.Game;
using HarmonyLib;
using RockVR.Video;
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
        
        [HarmonyPatch(typeof(SceneLoader), "TearDownAndLoadFrontEndAsync")]
        public class SceneLoader_TearDownAndLoadFrontEndAsync
        {
            public static void Prefix()
            {
                if (ShiftRecorder.sessions.Count > 0 && ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl != null)
                {
                    if (ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.STARTED || ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.PAUSED)
                    {
                        ShiftRecorder.sessions[GameSession.SessionCount - 1].videoCaptureCtrl.StopCapture();
                    }
                }
            }
        }
    }
}
