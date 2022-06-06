using Newtonsoft.Json;

namespace EndGameDebtModifier
{
    public class Settings
    {
        public bool enabled;
        public bool debugLog;

        public static Settings settings;

        public static void Load()
        {
            var settingsText = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "settings.json"));
            settings = JsonConvert.DeserializeObject<Settings>(settingsText);
        }
    }
}
