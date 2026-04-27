using BepInEx;
using BepInEx.Logging;
using Dawn;
using Dusk;
using GameNetcodeStuff;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace ItemSCPs
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(DawnLib.PLUGIN_GUID)]
    [BepInDependency("SCP500", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS8618
        public static Plugin Instance { get; private set; }
        public static ManualLogSource logger { get; private set; }
        public static DuskMod Mod { get; private set; }
#pragma warning restore CS8618

        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }
        public static PlayerControllerB? PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts.Where(x => x.actualClientId == id).FirstOrDefault(); }
        public static bool IsServerOrHost { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }

        public const ulong RodrigoSteamID = 76561198164429786;
        public const ulong LizzieSteamID = 76561199094139351;
        public const ulong GlitchSteamID = 76561198984467725;
        public const ulong RatSteamID = 76561199182474292;
        public const ulong XuSteamID = 76561198399127090;
        public const ulong SlayerSteamID = 76561198077184650;
        public const ulong SnowySteamID = 76561198253760639;
        public const ulong FunoSteamID = 76561198993437314;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            logger = Instance.Logger;

            harmony.PatchAll();

            AssetBundle? mainBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "itemscps_mainassets"));
            Mod = DuskMod.RegisterMod(this, mainBundle);
            Mod.RegisterContentHandlers();

            InitializeNetworkBehaviours();

            SnowyLib.Utils.isBeta = true; // TODO

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        private static void InitializeNetworkBehaviours()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            logger.LogDebug("Finished initializing network behaviours");
        }
    }
}

/*
Voice.ListenForPhrase("my cool phrase", (message) => {
    Plugin.logger.LogInfo("my cool voice phrase was said!");
});
https://github.com/LoafOrc/VoiceRecognitionAPI/wiki/For-Developers
*/