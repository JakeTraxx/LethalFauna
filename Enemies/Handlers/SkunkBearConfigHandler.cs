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
        public string SpawnLocationsAndRarities = "Adamance 25;Vow 25;March 25;Polarus 25;Extort 25;Acidir 25;Gloom 25;Cosmocos 25;Desolation 25;Solace 25;Auralis 25;Celest 25;Zenit 25;Icebound 25;Hydro 25;Landslide 25;Rend 25;Dine 25;Burrow 25;Synthesis 25;Wither 25;Crest 25;PsychSanctum 25;Junic 25;Roart 25;Spectralis 25;Sanguine 25;Siabudabu 25;Azure 25;Tauralis 25;Argent 25;Zeimalt 25;Zeranos 25;Pinnacle 25;Boreal 25;Verdance 25;Precipice 25;Budapest 25;Mazon 25;Rockwell 25;Seichi 25;Schalttafel 25;Flicker 25;Bomenoren 25;Kanie 25;Motra 25;Reign 25;Monarch 25;Valiance 25;Timbrance 25;Aquar 25;Tundaria 25;Utril 25;Filitrios 25;Veld 25;Faith 25;Vitality 25;Volition 25;Dejection 25;Haul 25;Defectus 25;Crowd 25;Consternation 25;Consolidation 25;Vigilance 25;Cerasi 25;Estate 25;Artifice 25;Solvang 25;Vermoss 25;Release 25;Narcotic 25";

        public SkunkBearConfigHandler(ConfigFile config) : base(config)
        {
            // Run any extra config setup here
        }
    }
}
