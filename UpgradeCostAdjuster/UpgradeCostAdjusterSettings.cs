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
        public float globalCostMultiplier;
        public Dictionary<string, float> upgradeCosts;
        public bool debugLogChanges;

        public static Settings settings;

        public static void Load()
        {
            var settingsText = System.IO.File.ReadAllText($".\\BepInEx\\plugins\\{PluginInfo.PLUGIN_NAME}\\settings.json");
            settings = JsonConvert.DeserializeObject<Settings>(settingsText);  
        }
    }
}
