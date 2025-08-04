using BepInEx.Configuration;
using LethalFauna.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalFauna.Enemies.Handlers
{
    internal class SkunkBearConfigHandler : ConfigHandler<SkunkBearConfigHandler>
    {
        public ConfigEntry<bool> enableSkunkBear;

        public SkunkBearConfigHandler(ConfigFile config) : base(config)
        {
            enableSkunkBear = config.Bind("Skunk Bear Settings", "enableSkunkBear", true, "Enables spawning of Skunk Bears.");
        }
    }
}
