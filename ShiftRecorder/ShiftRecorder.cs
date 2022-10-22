using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using RockVR.Video;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftRecorder
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ShiftRecorder : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource LoggerInstance;

        public static List<VideoCaptureSession> sessions = new List<VideoCaptureSession>();

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                LoggerInstance = Logger;
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                Main.EventSystem.AddHandler<GameStateChangedEvent>(new Carbon.Core.Events.EventHandler<GameStateChangedEvent>(evt =>
                {
                    if (evt.GameState != evt.PrevGameState)
                    {
                        if (evt.GameState == GameSession.GameState.Gameplay)
                        {
                            if(sessions.Count < GameSession.SessionCount)
                            // if (videoCaptureCtrl == null || newScene)
                            {
                                CreateRecorder();
                                if (Settings.settings.autoRecordShifts)
                                    sessions[GameSession.SessionCount - 1].videoCaptureCtrl.StartCapture();
                            }
                            else if (sessions[GameSession.SessionCount - 1].videoCaptureCtrl != null && sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.PAUSED)
                            {
                                sessions[GameSession.SessionCount - 1].videoCaptureCtrl.ToggleCapture();
                            }
                        }

                        if (ShiftRecorder.sessions.Count > 0 && evt.GameState == GameSession.GameState.Paused && sessions[GameSession.SessionCount - 1].videoCaptureCtrl != null)
                        {
                            if (sessions[GameSession.SessionCount - 1].videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.STARTED)
                            {
                                sessions[GameSession.SessionCount - 1].videoCaptureCtrl.ToggleCapture();
                            }
                        }
                    }
                }));

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            }
            else
            {
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is disabled");
            }
        }

        private void CreateRecorder()
        {

            sessions.Add(new VideoCaptureSession());

            VideoCaptureSession.captureCamera = GameObject.Find("RecordingCamera");

            if(VideoCaptureSession.captureCamera == null)
            {
                VideoCaptureSession.captureCamera = new GameObject("RecordingCamera");
                Settings.MoveCamera();
                var cam = VideoCaptureSession.captureCamera.AddComponent<Camera>();
                cam.cullingMask = ~LayerMask.GetMask("UI", "HUD_2D", "HUD_3D");

                VideoCaptureSession.captureCamera.AddComponent<VideoCapture>();
            }

            sessions[GameSession.SessionCount - 1].videoCapture = VideoCaptureSession.captureCamera.GetComponent<VideoCapture>();

            var camCtrl = new GameObject("CamCtrl");
            sessions[GameSession.SessionCount - 1].videoCaptureCtrl = camCtrl.AddComponent<VideoCaptureCtrl>();
            sessions[GameSession.SessionCount - 1].videoCaptureCtrl.videoCaptures = new VideoCapture[] { sessions[GameSession.SessionCount - 1].videoCapture };

            StartCoroutine(ChangeHud());
        }

        System.Collections.IEnumerator ChangeHud()
        {
            yield return new WaitForUpdate();

            if (Settings.settings.showInGamePreview)
            {
                VideoCaptureSession.imgObject = GameObject.Find("Preview Image");

                if(VideoCaptureSession.imgObject == null)
                {
                    VideoCaptureSession.imgObject = new GameObject("Preview Image");

                    RectTransform trans = VideoCaptureSession.imgObject.AddComponent<RectTransform>();
                    trans.transform.SetParent(GameObject.Find("Center Dot").transform.parent); // setting parent
                    trans.localScale = Vector3.one;

                    Settings.MovePreview();

                    VideoCaptureSession.rawImage = VideoCaptureSession.imgObject.AddComponent<UnityEngine.UI.RawImage>();
                    VideoCaptureSession.imgObject.transform.SetParent(GameObject.Find("Center Dot").transform.parent);

                    var aspectRatio = sessions[GameSession.SessionCount - 1].videoCapture.frameWidth / (float)sessions[GameSession.SessionCount - 1].videoCapture.frameHeight;
                    trans.sizeDelta = new Vector2(Settings.settings.previewHeight * aspectRatio, Settings.settings.previewHeight); // custom size
                }
            }

            // TODO: Hide the HUD

            yield return new WaitForSeconds(2f);
            var UICamera = GameObject.Find("UI Camera").GetComponent<Camera>();
            UICamera.enabled = true;
            var hudCanvas = GameObject.Find("HUD Canvas - Other").GetComponent<Canvas>();
            hudCanvas.worldCamera = UICamera;
            // hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var helmCanvas = GameObject.Find("HUD Canvas - Helmet").GetComponent<Canvas>();
            helmCanvas.worldCamera = UICamera;
            // helmCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            yield return new WaitForUpdate();
            helmCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            
            yield return new WaitForUpdate();
            helmCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            UICamera.enabled = false;
        }

        public class VideoCaptureSession
        {
            public VideoCapture videoCapture;
            public VideoCaptureCtrl videoCaptureCtrl;

            public static GameObject captureCamera;
            public static GameObject imgObject;
            public static UnityEngine.UI.RawImage rawImage;
        }
    }
}
