using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShiftRecorder
{
    public class Settings
    {
        public bool enabled;
        public bool debugLog;
        public bool autoRecordShifts;

        public string savePath;
        public string saveFileName;

        public int recordingWidth;
        public int recordingHeight;
        public int recordingTargetFPS;
        public int recordingBitrate;
        public int recordingAntiAliasing;

        public bool showInGamePreview;
        public int previewHeight;
        public int previewVerticalPosition;
        public int previewHorizontalPosition;

        public string selectedCameraPosition;

        public List<CameraPosition> cameraPositions;

        public static Settings settings;

        public static void Load()
        {
            var settingsText = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "settings.json"));
            settings = JsonConvert.DeserializeObject<Settings>(settingsText);
            RockVR.Video.PathConfig.saveFolder = System.IO.Path.GetFullPath(settings.savePath);
            RockVR.Video.PathConfig.ffmpegPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ffmpeg.exe");

            MoveCamera();
        }

        public static void MoveCamera()
        {
            if (ShiftRecorder.captureCamera != null)
            {
                var pos = settings.cameraPositions.FirstOrDefault(pos => pos.name == settings.selectedCameraPosition) ?? settings.cameraPositions.First();
                ShiftRecorder.captureCamera.transform.position = pos.position;
                ShiftRecorder.captureCamera.transform.rotation = Quaternion.Euler(pos.rotation.x, pos.rotation.y, pos.rotation.z);
            }
        }

        public class CameraPosition
        {
            public string name;
            public Vector3 position;
            public Vector3 rotation;
        }
    }
}
