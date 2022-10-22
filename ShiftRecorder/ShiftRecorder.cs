using BBI.Unity.Game;
using BepInEx;
using HarmonyLib;
using RockVR.Video;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShiftRecorder
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class ShiftRecorder : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource LoggerInstance;

        public static VideoCapture videoCapture;
        public static VideoCaptureCtrl videoCaptureCtrl;

        public static GameObject captureCamera;
        public static GameObject imgObject;
        public static UnityEngine.UI.RawImage rawImage;

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
                        if (videoCaptureCtrl != null && (evt.GameState == GameSession.GameState.GameComplete || evt.GameState == GameSession.GameState.GameOver))
                        {
                            if (videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.STARTED || videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.PAUSED)
                            {
                                videoCaptureCtrl.StopCapture();
                            }
                        }

                        if (evt.GameState == GameSession.GameState.Gameplay)
                        {
                            if (videoCaptureCtrl != null && videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.PAUSED)
                            {
                                videoCaptureCtrl.ToggleCapture();
                            }
                            else if (videoCaptureCtrl == null)
                            {
                                CreateRecorder();
                                if(Settings.settings.autoRecordShifts)
                                    videoCaptureCtrl.StartCapture();
                            }
                        }

                        if (videoCaptureCtrl != null && evt.GameState == GameSession.GameState.Paused)
                        {
                            if (videoCaptureCtrl.status == VideoCaptureCtrlBase.StatusType.STARTED)
                            {
                                videoCaptureCtrl.ToggleCapture();
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
            if(videoCapture == null || videoCaptureCtrl == null)
            {
                captureCamera = new GameObject("RecordingCamera");
                Settings.MoveCamera();
                var cam = captureCamera.AddComponent<Camera>();
                cam.cullingMask = ~LayerMask.GetMask("UI", "HUD_2D", "HUD_3D");
                videoCapture = captureCamera.AddComponent<VideoCapture>();

                var camCtrl = new GameObject("CamCtrl");
                videoCaptureCtrl = camCtrl.AddComponent<VideoCaptureCtrl>();
                videoCaptureCtrl.videoCaptures = new VideoCapture[] { videoCapture };
            }

            StartCoroutine(ChangeHud());
        }

        System.Collections.IEnumerator ChangeHud()
        {
            yield return new WaitForUpdate();

            if (Settings.settings.showInGamePreview)
            {
                imgObject = new GameObject("Preview Image");

                RectTransform trans = imgObject.AddComponent<RectTransform>();
                trans.transform.SetParent(GameObject.Find("Center Dot").transform.parent); // setting parent
                trans.localScale = Vector3.one;
                trans.anchoredPosition = new Vector2(850f * Settings.settings.previewHorizontalPosition, 450f * Settings.settings.previewVerticalPosition * -1); // setting position, will be on center

                rawImage = imgObject.AddComponent<UnityEngine.UI.RawImage>();
                imgObject.transform.SetParent(GameObject.Find("Center Dot").transform.parent);

                var aspectRatio = videoCapture.frameWidth / (float)videoCapture.frameHeight;
                trans.sizeDelta = new Vector2(Settings.settings.previewHeight * aspectRatio, Settings.settings.previewHeight); // custom size
            }

            // TODO: Hide the HUD

            /*
            var UICamera = GameObject.Find("UI Camera").GetComponent<Camera>();
            var hudCanvas = GameObject.Find("HUD Canvas - Other").GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            hudCanvas.worldCamera = UICamera;
            */
        }
    }
}
