using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GorillaAvatarCatalog.Behaviours;
using HarmonyLib;
using UnityEngine;

namespace GorillaAvatarCatalog
{
    [BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource MainLogSource;

        public static ConfigFile MainConfig;

        public void Awake()
        {
            MainLogSource = Logger;
            MainConfig = Config;

            Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, Constants.GUID);

            GorillaTagger.OnPlayerSpawned(() => DontDestroyOnLoad(new GameObject($"{Constants.Name} {Constants.Version}", typeof(AvatarPreferences))));
        }
    }
}
