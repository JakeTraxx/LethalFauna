using BepInEx.Configuration;
using LethalFauna.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalFauna.Enemies.Handlers
{
    internal class WatcherHarpyConfigHandler : ConfigHandler
    {
        public ConfigEntry<bool> enableWatcherHarpy;
        public WatcherHarpyConfigHandler(ConfigFile config) : base(config)
        {
            enableWatcherHarpy = config.Bind("Watcher Harpy Settings", "enableWatcherHarpy", true, "Enables spawning of Watcher Harpys.");
        }
    }
}
