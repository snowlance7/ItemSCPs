using Dawn;
using HarmonyLib;
using ItemSCPs.SCP;
using SnowyLib;
using System.Linq;
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
        public static bool immunity { get; private set; }

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.testing) { return; }
        }

        public static void Update()
        {
            if (!Utils.testing) { return; }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                string msg = __instance.chatTextField.text;
                string[] args = msg.Split(" ");

                switch (args[0])
                {
                    case "/immunity":
                        immunity = !immunity;
                        HUDManager.Instance.DisplayTip("ItemSCPs", "Immunity: " + immunity);
                        break;
                    case "/immune":
                        immunity = !immunity;
                        HUDManager.Instance.DisplayTip("ItemSCPs", "Immunity: " + immunity);
                        break;
                    case "/testlight":
                        Vector3 spawnPosition = localPlayer.bodyParts[5].transform.position;
                        NetworkHandler.Instance.CreateLightFlashRpc(spawnPosition);
                        break;
                    case "/disease":
                        if (args.Length == 1 || !int.TryParse(args[1], out int index)) { return; }
                        if (index > 5 || index < 0) { return; }
                        SCP1025Behavior.diseases[index].Invoke();
                        break;
                    case "/3482":
                        SCP3482Behavior.localPlayerAffected = false;
                        break;
                    default:
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