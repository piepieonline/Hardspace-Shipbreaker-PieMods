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
                if (ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.STARTED || ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.PAUSED)
                {
                    ShiftRecorder.VideoCaptureSession.VideoCaptureCtrl().StopCapture();
                }
            }
        }
    }
}
