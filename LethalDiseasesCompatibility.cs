using GameNetcodeStuff;
using System.Runtime.CompilerServices;

namespace ItemSCPs
{
    internal class LethalDiseasesCompatibility
    {
        private static bool? _enabled;

        internal static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Snowlance.LethalDiseases");
                }
                return (bool)_enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void InfectPlayer(PlayerControllerB player)
        {
            LethalDiseases.DiseaseAPI.Infect(player);
        }
    }
}