using HarmonyLib;
using ItemSCPs.Items.Snowy;
using System;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    public static class VoiceRecognition
    {
        public static void RegisterPhrases()
        {
            SCP983Behavior.RegisterPhrases();
        }
    }

    [HarmonyPatch]
    public static class VoiceRecognitionPatches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void PingScan_performedPostFix()
        {
            try
            {
                VoiceRecognition.RegisterPhrases();
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }
        }
    }
}
