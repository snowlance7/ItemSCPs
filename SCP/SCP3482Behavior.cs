using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP3482Behavior : PhysicsProp // ANTI LEFT POSTER
    {
        public static bool localPlayerAffected;

        public override void EquipItem()
        {
            base.EquipItem();
            if (TESTING.localPlayerImmune || SCP714Behavior.localPlayerAffected) { return; }
            localPlayerAffected = true;
            StatusEffectController.Instance.ApplyEffect(new OnRemoveActionEffect(() => localPlayerAffected = false, "scp3482", "antileft_effect", curableBySCP500: false));
        }
    }

    [HarmonyPatch]
    class SCP3482Patches // TODO: Test this
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CalculateNormalLookingInput))]
        private static void CalculateNormalLookingInputPrefix(ref Vector2 inputVector, PlayerControllerB __instance)
        {
            try
            {
                if (SCP3482Behavior.localPlayerAffected)
                {
                    inputVector.x = Mathf.Max(inputVector.x, 0f);
                }
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CalculateSmoothLookingInput))]
        private static void CalculateSmoothLookingInputPrefix(ref Vector2 inputVector, PlayerControllerB __instance)
        {
            try
            {
                if (SCP3482Behavior.localPlayerAffected)
                {
                    inputVector.x = Mathf.Max(inputVector.x, 0f);
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
