using HarmonyLib;
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
        public static string currentAnim = "";

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {
            if (!Utils.testing) { return; }
            //HUDManager.Instance.HUDAnimator.SetTrigger("SpawnUI"); // TODO: VIGNETTE FOUND!

            var entrances = GameObject.FindObjectsOfType<EntranceTeleport>(includeInactive: true).ToList();

            foreach (var entrance in entrances)
            {
                if (!entrance.gotExitPoint)
                {
                    if (entrance.FindExitPoint())
                        entrance.gotExitPoint = true;
                    else
                    {
                        logger.LogDebug($"Skipping {entrance.name}, no exit point found");
                        continue;
                    }
                }

                /*Vector3 local = entrance.transform.InverseTransformPoint(entrance.entrancePoint.position);

                local.x = -local.x;
                local.z = -local.z;

                local.y += 0.5f;

                Vector3 oppositePoint = entrance.transform.TransformPoint(local);
                Utils.Ping(oppositePoint, "Inverse Point");*/
            }

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