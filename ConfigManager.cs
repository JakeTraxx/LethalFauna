using BepInEx.Configuration;

namespace LethalFauna
{
    internal class ConfigManager
    {
        public static ConfigEntry<bool> enableSkunkBear;
        public static ConfigEntry<bool> enableWatcherHarpy;
        public static ConfigEntry<bool> enableScribe;

        public static void Init()
        {
            enableSkunkBear = LethalFaunaMod.instance.Config.Bind("Skunk Bear Settings", "enableSkunkBear", true, "Enables spawning of Skunk Bears.");
            enableWatcherHarpy = LethalFaunaMod.instance.Config.Bind("Watcher Harpy Settings", "enableWatcherHarpy", true, "Enables spawning of Watcher Harpys.");
            enableScribe = LethalFaunaMod.instance.Config.Bind("Scribe Settings", "enableScribe", true, "Enables spawning of Scribes.");
        }
    }
}
