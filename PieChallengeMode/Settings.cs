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
        public int maxTotalObjectives;
        public List<string> validCollectionKeys;
        public List<string> validMassKeys;

        public static Settings settings;

        public static void Load()
        {
            var settingsText = System.IO.File.ReadAllText($".\\BepInEx\\plugins\\{PluginInfo.PLUGIN_NAME}\\settings.json");
            settings = JsonConvert.DeserializeObject<Settings>(settingsText);
        }
    }
}
