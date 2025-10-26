using BepInEx.Configuration;
using LethalFauna.Util;
using LethalFauna.Util.Attributes;

namespace LethalFauna.Enemies.Handlers
{
    internal class WatcherHarpyConfigHandler : ConfigHandler<WatcherHarpyConfigHandler>
    {
        [Config("Enables spawning of Watcher Harpys.")]
        public bool EnableWatcherHarpy = true;

        public WatcherHarpyConfigHandler(ConfigFile config) : base(config)
        {
            // Run any extra config setup here
        }
    }
}
