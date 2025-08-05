using LethalFauna.Util;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalFauna.Enemies.Handlers
{
    internal class SkunkBearHandler : EnemyHandler<SkunkBearHandler>
    {
        public static SkunkBearConfigHandler Config;

        public SkunkBearHandler()
        {
            Config = new SkunkBearConfigHandler(LethalFaunaMod.instance.CreateConfig("skunkbear"));

            if (!Config.EnableSkunkBear)
                return;

            AssetBundle bundle = LethalFaunaMod.bundle;

            var sb = bundle.LoadAsset<EnemyType>("Assets/LethalFauna/SkunkBear/SkunkBear.asset");
            var skunkBearTN = bundle.LoadAsset<TerminalNode>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkBearTN.asset");
            var skunkBearTK = bundle.LoadAsset<TerminalKeyword>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkBearTK.asset");
            NetworkPrefabs.RegisterNetworkPrefab(sb.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(sb, 100, Levels.LevelTypes.All, skunkBearTN, skunkBearTK);

            var sb_cub = bundle.LoadAsset<EnemyType>("Assets/LethalFauna/SkunkBear/SkunkCub.asset");
            var skunkCubTN = bundle.LoadAsset<TerminalNode>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkCubTN.asset");
            var skunkCubTK = bundle.LoadAsset<TerminalKeyword>("Assets/LethalFauna/SkunkBear/Bestiary/SkunkCubTK.asset");
            NetworkPrefabs.RegisterNetworkPrefab(sb_cub.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(sb_cub, 100, Levels.LevelTypes.All, skunkCubTN, skunkCubTK);
        }
    }
}
