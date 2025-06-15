using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using UnityEngine;

namespace LethalFauna
{
    [BepInPlugin(mGUID, mName, mVersion)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class LethalFaunaMod : BaseUnityPlugin
    {
        const string mGUID = "eXish.LethalFauna";
        const string mName = "LethalFauna";
        const string mVersion = "1.0.0";

        readonly Harmony harmony = new Harmony(mGUID);

        internal static LethalFaunaMod instance;
        internal static AssetBundle bundle;

        void Awake()
        {
            if (instance == null)
                instance = this;

            ConfigManager.Init();

            string modLocation = instance.Info.Location.TrimEnd("LethalFauna.dll".ToCharArray());
            bundle = AssetBundle.LoadFromFile(modLocation + "lethalfauna");
            if (bundle != null)
            {
                if (ConfigManager.enableSkunkBear.Value)
                {
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
                if (ConfigManager.enableWatcherHarpy.Value)
                {
                    var wh = bundle.LoadAsset<EnemyType>("Assets/LethalFauna/WatcherHarpy/WatcherHarpy.asset");
                    var watcherHarpyTN = bundle.LoadAsset<TerminalNode>("Assets/LethalFauna/WatcherHarpy/Bestiary/WatcherHarpyTN.asset");
                    var watcherHarpyTK = bundle.LoadAsset<TerminalKeyword>("Assets/LethalFauna/WatcherHarpy/Bestiary/WatcherHarpyTK.asset");
                    NetworkPrefabs.RegisterNetworkPrefab(wh.enemyPrefab);
                    LethalLib.Modules.Enemies.RegisterEnemy(wh, 100, Levels.LevelTypes.All, watcherHarpyTN, watcherHarpyTK);
                }
            }
            else
                instance.Logger.LogError("Unable to locate the asset file! Enemies will not spawn.");

            harmony.PatchAll();

            instance.Logger.LogInfo($"{mName}-{mVersion} loaded!");
        }
    }
}
