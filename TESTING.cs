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
        public static string currentAnim = "";

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.isBeta) { return; }
            if (!Utils.testing) { return; }
            //StatusEffectController.Instance.TestAudio();
            localPlayer.PlayQuickSpecialAnimation(1);
            localPlayer.playerBodyAnimator.SetTrigger(currentAnim);
            logger.LogDebug("PingScanTestPerformed");
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
                case "/overlay":
                    StatusEffectController.Instance.vignetteOverlay.SetIntensity(float.Parse(args[1]));
                    HUDManager.Instance.DisplayTip("ItemSCPs", "VignetteOverlay: " + args[1]);
                    break;
                case "/immune":
                    localPlayerImmune = !localPlayerImmune;
                    HUDManager.Instance.DisplayTip("ItemSCPs", "localPlayerImmune: " + localPlayerImmune);
                    break;
                case "/anim":
                    localPlayer.playerBodyAnimator.SetTrigger(args[1]);
                    currentAnim = args[1];
                    break;
                case "/qanim":
                    localPlayer.PlayQuickSpecialAnimation(1);
                    localPlayer.playerBodyAnimator.SetTrigger(args[1]);
                    currentAnim = args[1];
                    break;
                default:
                    Utils.ChatCommand(args);
                    break;
            }
        }
    }
}