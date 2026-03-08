using HarmonyLib;
using UnityEngine;
using static ItemSCPs.Plugin;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace ItemSCPs
{
    [HarmonyPatch]
    public class TESTING : MonoBehaviour
    {
        public static bool localPlayerImmune = false;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.isBeta) { return; }
            if (!Utils.testing) { return; }
            StatusEffectController.Instance.TestAudio();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            if (!Utils.isBeta) { return; }
            if (!IsServerOrHost) { return; }
            string msg = __instance.chatTextField.text;
            string[] args = msg.Split(" ");
            Plugin.logger.LogDebug(msg);

            switch (args[0])
            {
                case "/overlay": // TODO: Test this
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(float.Parse(args[1]));
                    HUDManager.Instance.DisplayTip("ItemSCPs", "VignetteOverlay: " + args[1]);
                    break;
                case "/immune":
                    localPlayerImmune = !localPlayerImmune;
                    HUDManager.Instance.DisplayTip("ItemSCPs", "localPlayerImmune: " + localPlayerImmune);
                    break;
                default:
                    Utils.ChatCommand(args);
                    break;
            }
        }
    }
}