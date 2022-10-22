using BBI.Unity.Game;
using System;
using System.Linq;
using Unity.Entities;

namespace RockVR.Video
{
    public class StringUtils
    {
        public static string GetTimeString()
        {
            return DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        }

        public static string GetRandomString(int length)
        {
            System.Random random = new System.Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GetFileName(string _)
        {
            var name = ShiftRecorder.Settings.settings.saveFileName;

            if (World.DefaultGameObjectInjectionWorld.EntityManager.TryGetComponent(ModuleService.Instance.CurrentShipPreview.SpawnedManifest, out ShipIdentification shipIdentification))
            {
                name = name.Replace("{ShipName}", shipIdentification.ShipName.ToString());

                if (Main.Instance.LocalizationService.TryLocalize(shipIdentification.ShipDifficulty.ToString(), out var localisedText))
                {
                    name = name.Replace("{ShipDifficulty}", localisedText);
                }
                if (Main.Instance.LocalizationService.TryLocalize(shipIdentification.ShipArchetype.ToString(), out localisedText))
                {
                    name = name.Replace("{ShipArchetype}", localisedText);
                }
                if (Main.Instance.LocalizationService.TryLocalize(shipIdentification.ShipRole.ToString(), out localisedText))
                {
                    name = name.Replace("{ShipRole}", localisedText);
                }
            }

            name = name.Replace("{SessionType}", Enum.GetName(typeof(GameSession.SessionType), GameSession.CurrentSessionType));

            name = name.Replace("{Time}", GetTimeString());

            name = name.Replace("{Hash}", GetRandomString(5));

            return name;
        }

        public static string GetMp4FileName(string name)
        {
            return GetFileName(name) + ".mp4";
        }

        public static string GetH264FileName(string name)
        {
            return GetFileName(name) + ".h264";
        }

        public static string GetWavFileName(string name)
        {
            return GetFileName(name) + ".wav";
        }

        public static string GetPngFileName(string name)
        {
            return GetFileName(name) + ".png";
        }

        public static string GetJpgFileName(string name)
        {
            return GetFileName(name) + ".jpg";
        }

        public static bool IsRtmpAddress(string str)
        {
            return (str != null && str.StartsWith("rtmp"));
        }
    }

    public class MathUtils
    {
        public static bool CheckPowerOfTwo(int input)
        {
            return (input & (input - 1)) == 0;
        }
    }
}