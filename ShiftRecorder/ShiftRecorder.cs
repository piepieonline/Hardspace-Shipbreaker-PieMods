using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using RockVR.Video;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftRecorder
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ShiftRecorder : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource LoggerInstance;

        public static List<VideoCaptureSession> sessions = new List<VideoCaptureSession>();

        private static ShiftRecorder instance;

        private void Awake()
        {
            // Plugin startup logic
            Settings.Load();
            if (Settings.settings.enabled)
            {
                LoggerInstance = Logger;
                new Harmony($"{PluginInfo.PLUGIN_GUID}").PatchAll();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is patched");

                instance = this;

                Main.EventSystem.AddHandler<GameStateChangedEvent>(new Carbon.Core.Events.EventHandler<GameStateChangedEvent>(evt =>
                {
                    if (sessions.Count < GameSession.SessionCount)
                    {
                        sessions.Add(null);
                    }

                    if (evt.GameState != evt.PrevGameState)
                    {
                        if (evt.GameState == GameSession.GameState.Gameplay)
                        {
                            if(sessions.Last() == null)
                            {
                                if (Settings.settings.autoRecordShifts)
                                {
                                    CreateRecorder();

                                    VideoCaptureSession.VideoCaptureCtrl()?.StartCapture();
                                }
                            }
                            else if (VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.PAUSED)
                            {
                                VideoCaptureSession.VideoCaptureCtrl()?.ToggleCapture();
                            }
                        }

                        if (ShiftRecorder.sessions.Count > 0 && evt.GameState == GameSession.GameState.Paused)
                        {
                            if (VideoCaptureSession.VideoCaptureCtrl()?.status == VideoCaptureCtrlBase.StatusType.STARTED)
                            {
                                VideoCaptureSession.VideoCaptureCtrl()?.ToggleCapture();
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

        public static void CreateRecorder()
        {
            sessions[GameSession.SessionCount - 1] = new VideoCaptureSession();

            VideoCaptureSession.captureCamera = GameObject.Find("RecordingCamera");

            if(VideoCaptureSession.captureCamera == null)
            {
                VideoCaptureSession.captureCamera = new GameObject("RecordingCamera");
                Settings.MoveCamera(Settings.settings.selectedCameraPosition);
                var cam = VideoCaptureSession.captureCamera.AddComponent<Camera>();
                cam.cullingMask = ~LayerMask.GetMask("UI", "HUD_2D", "HUD_3D");

                VideoCaptureSession.captureCamera.AddComponent<VideoCapture>();
            }

            sessions[GameSession.SessionCount - 1].videoCapture = VideoCaptureSession.captureCamera.GetComponent<VideoCapture>();

            var camCtrl = new GameObject("CamCtrl");
            sessions[GameSession.SessionCount - 1].videoCaptureCtrl = camCtrl.AddComponent<VideoCaptureCtrl>();
            sessions[GameSession.SessionCount - 1].videoCaptureCtrl.videoCaptures = new VideoCapture[] { sessions[GameSession.SessionCount - 1].videoCapture };

            StartChangeHud();
        }

        public static void StartChangeHud()
        {
            instance.StartCoroutine(instance.ChangeHud());
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

            // Hide the HUD from the recorder camera
            // Don't ask me how or why this works... It just does

            if(Settings.settings.hideHUDInRecording)
            {
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
        }

        private void Update()
        {
            bool shouldMove = false;

            if(Input.GetKeyUp(Settings.settings.keybindNextCamera))
            {
                DebugMenu.cameraIndex++;
                shouldMove = true;
            }
            if (Input.GetKeyUp(Settings.settings.keybindPreviousCamera))
            {
                DebugMenu.cameraIndex = DebugMenu.cameraIndex == 0 ? Settings.settings.cameraPositions.Count - 1 : DebugMenu.cameraIndex - 1;
                shouldMove = true;
            }

            if (Input.GetKeyUp(Settings.settings.keybindCamera1)) { DebugMenu.cameraIndex = 0; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera2)) { DebugMenu.cameraIndex = 1; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera3)) { DebugMenu.cameraIndex = 2; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera4)) { DebugMenu.cameraIndex = 3; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera5)) { DebugMenu.cameraIndex = 4; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera6)) { DebugMenu.cameraIndex = 5; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera7)) { DebugMenu.cameraIndex = 6; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera8)) { DebugMenu.cameraIndex = 7; shouldMove = true; }
            if (Input.GetKeyUp(Settings.settings.keybindCamera9)) { DebugMenu.cameraIndex = 8; shouldMove = true; }

            if (shouldMove)
            {
                DebugMenu.cameraIndex = DebugMenu.cameraIndex % Settings.settings.cameraPositions.Count;
                Settings.MoveCamera(Settings.settings.cameraPositions[DebugMenu.cameraIndex].name);
            }
        }

        public class VideoCaptureSession
        {
            public VideoCapture videoCapture;
            public VideoCaptureCtrl videoCaptureCtrl;

            public static GameObject captureCamera;
            public static GameObject imgObject;
            public static UnityEngine.UI.RawImage rawImage;

            public static VideoCapture VideoCapture()
            {
                return sessions.Count <= GameSession.SessionCount - 1 ? sessions[GameSession.SessionCount - 1].videoCapture : null;
            }

            public static VideoCaptureCtrl VideoCaptureCtrl()
            {
                return (sessions.Count > 0 && sessions.Count == GameSession.SessionCount) ? sessions[GameSession.SessionCount - 1]?.videoCaptureCtrl : null;
            }
        }
    }
}
