using BepInEx.Configuration;
using LethalFauna.Util;
using LethalFauna.Util.Attributes;

namespace LethalFauna.Enemies.Handlers
{
    internal class SkunkBearConfigHandler : ConfigHandler<SkunkBearConfigHandler>
    {
        [Config("Enables spawning of Skunk Bears.")]
        public bool EnableSkunkBear = true;

        [Config("The moons Skunk Bears can spawn on and how rare it is to spawn on them (1 being rare and 100 being common). Split each entry with a semicolon or comma.")]
        public string SpawnLocationsAndRarities = "March 85;Vow 85;Adamance 85;Verdance 85;Dine 85;Rend 85;Polarus 85;Siabudabu 85;Boreal 85";

        public SkunkBearConfigHandler(ConfigFile config) : base(config)
        {
            // Run any extra config setup here
        }
    }
}
