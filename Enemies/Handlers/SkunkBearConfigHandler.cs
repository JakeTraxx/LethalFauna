using BepInEx.Configuration;
using LethalFauna.Util;
using LethalFauna.Util.Attributes;

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
