using LethalFauna.Util;
using LethalLib.Modules;
using System;
using System.Linq;
using UnityEngine;

namespace LethalFauna.Enemies.Handlers
{
    internal class SkunkBearHandler : EnemyHandler<SkunkBearHandler>
    {
        public static SkunkBearConfigHandler Config;
        readonly string[] vanillaLevelStrings = { "experimentation", "assurance", "vow", "offense", "march", "adamance", "rend", "dine", "titan", "artifice", "embrion" };
        readonly Levels.LevelTypes[] vanillaLevelTypes = { Levels.LevelTypes.ExperimentationLevel, Levels.LevelTypes.AssuranceLevel, Levels.LevelTypes.VowLevel, Levels.LevelTypes.OffenseLevel, Levels.LevelTypes.MarchLevel, Levels.LevelTypes.AdamanceLevel, Levels.LevelTypes.RendLevel, Levels.LevelTypes.DineLevel, Levels.LevelTypes.TitanLevel, Levels.LevelTypes.ArtificeLevel, Levels.LevelTypes.EmbrionLevel };

        public SkunkBearHandler()
        {
            Config = new SkunkBearConfigHandler(LethalFaunaMod.instance.CreateConfig("skunkbear"));

            if (!Config.EnableSkunkBear)
                return;

            // Verify SpawnLocationsAndRarities is valid
            string[] locAndRarity = Config.SpawnLocationsAndRarities.Split(';', ',');
            for (int i = 0; i < locAndRarity.Length; i++)
            {
                if (!int.TryParse(locAndRarity[i].Trim().Split(' ').Last(), out int temp))
                {
                    LethalFaunaMod.log.LogWarning("The config value SpawnLocationsAndRarities for the Skunk Bear is invalid!");
                    return;
                }
                if (temp < 1 || temp > 100)
                {
                    LethalFaunaMod.log.LogWarning("The config value SpawnLocationsAndRarities for the Skunk Bear is invalid!");
                    return;
                }
            }

            AssetBundle bundle = LethalFaunaMod.bundle;

            var sb = bundle.LoadAsset<EnemyType>("Assets/LethalFauna/SkunkBear/SkunkBear.asset");
            var skunkBearTN = bundle.LoadAsset<TerminalNode>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkBearTN.asset");
            var skunkBearTK = bundle.LoadAsset<TerminalKeyword>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkBearTK.asset");
            NetworkPrefabs.RegisterNetworkPrefab(sb.enemyPrefab);
            for (int i = 0; i < locAndRarity.Length; i++)
            {
                string[] parts = locAndRarity[i].Trim().Split(' ');
                string planetName = "";
                for (int j = 0; j < parts.Length - 1; j++)
                    planetName += parts[j] + " ";
                planetName = planetName.Trim();
                if (vanillaLevelStrings.Contains(planetName.ToLowerInvariant()))
                    LethalLib.Modules.Enemies.RegisterEnemy(sb, int.Parse(parts.Last()), vanillaLevelTypes[Array.IndexOf(vanillaLevelStrings, planetName.ToLowerInvariant())], skunkBearTN, skunkBearTK);
                else
                    LethalLib.Modules.Enemies.RegisterEnemy(sb, int.Parse(parts.Last()), Levels.LevelTypes.None, new string[] { planetName }, skunkBearTN, skunkBearTK);
            }

            var sb_cub = bundle.LoadAsset<EnemyType>("Assets/LethalFauna/SkunkBear/SkunkCub.asset");
            var skunkCubTN = bundle.LoadAsset<TerminalNode>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkCubTN.asset");
            var skunkCubTK = bundle.LoadAsset<TerminalKeyword>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkCubTK.asset");
            NetworkPrefabs.RegisterNetworkPrefab(sb_cub.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(sb_cub, 100, Levels.LevelTypes.None, skunkCubTN, skunkCubTK);
        }
    }
}
