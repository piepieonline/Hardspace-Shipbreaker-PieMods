using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpgradeCostAdjuster
{
    public class Settings
    {
        public bool enabled;
        public float globalLTCostMultiplier;
        public float globalLTToCreditFactor;
        public int globalRequiredLevelDefaultOverride;
        public Dictionary<string, UpgradeCosts> upgradeCosts;
        public List<string> excludedBecauseBroken;
        public bool debugLogChanges;

        public static Settings settings;

        public static void Load()
        {
            var settingsText = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "settings.json"));
            settings = JsonConvert.DeserializeObject<Settings>(settingsText);  
        }
    }

    public class UpgradeCosts
    {
        public Dictionary<string, float> prices = new Dictionary<string, float>();
        public int requiredLevel = -1;
    }
}
