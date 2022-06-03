using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PieChallengeMode
{
    public class Settings
    {
        public bool enabled;
        public bool alwaysOn;
        public bool debugLog;
        public int minTotalObjectives;
        public int maxTotalObjectives;
        public Dictionary<string, float> baseTimePerShipType;
        public Dictionary<string, ObjectCollectionParameters> collectionObjects;
        public List<string> invalidCollectionObjects;

        public static Settings settings;

        public static void Load()
        {
            var settingsText = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "settings.json"));
            settings = JsonConvert.DeserializeObject<Settings>(settingsText);
        }

        public class ObjectCollectionParameters
        {
            public float minPercent;
            public float maxPercent;
            public float minPercentTime;
            public float maxPercentTime;
        }
    }
}
