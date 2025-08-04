using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalFauna.Enemies.Handlers
{
    internal class WatcherHarpyHandler
    {
        public static WatcherHarpyConfigHandler Config;

        public WatcherHarpyHandler()
        {
            Config = new WatcherHarpyConfigHandler(LethalFaunaMod.instance.CreateConfig("watcherharpy"));
            if (!Config.enableWatcherHarpy.Value)
                return;

            AssetBundle bundle = LethalFaunaMod.bundle;
            var wh = bundle.LoadAsset<EnemyType>("Assets/LethalFauna/WatcherHarpy/WatcherHarpy.asset");
            var watcherHarpyTN = bundle.LoadAsset<TerminalNode>("Assets/LethalFauna/WatcherHarpy/Bestiary/WatcherHarpyTN.asset");
            var watcherHarpyTK = bundle.LoadAsset<TerminalKeyword>("Assets/LethalFauna/WatcherHarpy/Bestiary/WatcherHarpyTK.asset");
            NetworkPrefabs.RegisterNetworkPrefab(wh.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(wh, 100, Levels.LevelTypes.All, watcherHarpyTN, watcherHarpyTK);
        }
    }
}
