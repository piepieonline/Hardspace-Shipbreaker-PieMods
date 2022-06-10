using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FOVAdjuster
{
    public class Settings
    {
        public bool enabled;
        public bool debugLogChanges;
        public float fov;
        public bool showHelmet;
        public float toolOffsetAt90;
        public float smashScaleAt90;
        public List<string> damageVFXGameObjectNames;

        public static Settings settings;

        public static void Load()
        {
            var settingsText = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "settings.json"));
            settings = JsonConvert.DeserializeObject<Settings>(settingsText);  
        }
    }
}
