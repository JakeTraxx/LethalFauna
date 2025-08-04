using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using LethalFauna.Util;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            string modLocation = instance.Info.Location.TrimEnd("LethalFauna.dll".ToCharArray());
            bundle = AssetBundle.LoadFromFile(modLocation + "lethalfauna");
            if (bundle == null)
            {
                instance.Logger.LogError("Unable to locate the asset file! Enemies will not spawn.");
                return;
            }

            // Initialize enemy handlers
            List<Type> creatureHandlers = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.BaseType.GetGenericTypeDefinition() == typeof(EnemyHandler<>)).ToList<Type>();

            foreach (Type type in creatureHandlers)
            {
                type.GetConstructor(new Type[] { })?.Invoke(new object[] { });
            }

            harmony.PatchAll();

            instance.Logger.LogInfo($"{mName}-{mVersion} loaded!");
        }

        internal ConfigFile CreateConfig(string configName)
        {
            return new ConfigFile(Utility.CombinePaths(Paths.ConfigPath, "eXish.LethalFauna." + configName + ".cfg"),
                saveOnInit: false, MetadataHelper.GetMetadata(this));
        }
    }
}
