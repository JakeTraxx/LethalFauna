using BepInEx.Configuration;
using LethalFauna.Util;
using LethalFauna.Util.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalFauna.Enemies.Handlers
{
    internal class SkunkBearConfigHandler : ConfigHandler<SkunkBearConfigHandler>
    {
        [Config("Enables spawning of Skunk Bears.")]
        public bool EnableSkunkBear = true;

        public SkunkBearConfigHandler(ConfigFile config) : base(config)
        {
            // Run any extra config setup here
        }
    }
}
