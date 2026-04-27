using HarmonyLib;
using ItemSCPs.SCP;
using System.Linq;
using UnityEngine;
using static ItemSCPs.Plugin;
using SnowyLib;

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
        public static bool immunity { get; private set; }
        public static string currentAnim = "";

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.isBeta) { return; }
            if (!Utils.testing) { return; }



            logger.LogDebug("PingScanTestPerformed");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                if (!Utils.isBeta) { return; }
                if (!IsServerOrHost) { return; }
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");
                Plugin.logger.LogDebug(msg);

                switch (args[0])
                {
                    case "/doors":
                        TerminalAccessibleObject[] doors = GameObject.FindObjectsOfType<TerminalAccessibleObject>().Where(x => x.isBigDoor).ToArray();
                        foreach (TerminalAccessibleObject door in doors)
                        {
                            door.SetDoorOpenServerRpc(false);
                        }
                        break;
                    case "/disease":
                        SCP1025Behavior.diseases[int.Parse(args[1])].Invoke();
                        break;
                    case "/overlay":
                        StatusEffectController.Instance.vignetteOverlay.SetIntensity(float.Parse(args[1]));
                        HUDManager.Instance.DisplayTip("ItemSCPs", "VignetteOverlay: " + args[1]);
                        break;
                    case "/immunity":
                        immunity = !immunity;
                        HUDManager.Instance.DisplayTip("ItemSCPs", "Immunity: " + immunity);
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
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}