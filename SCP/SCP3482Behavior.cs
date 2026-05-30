using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using static ItemSCPs.Plugin;
using SnowyLib;

namespace ItemSCPs.SCP
{
    internal class SCP3482Behavior : PhysicsProp // ANTI LEFT POSTER
    {
        public static bool localPlayerAffected;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.21f, 0.08f, -0.35f);
            itemProperties.rotationOffset = new Vector3(0, -90, 180);
            itemProperties.floorYOffset = 0;
            itemProperties.restingRotation = new Vector3(0, 0, 180);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            if (TESTING.immunity || SCP714Behavior.localPlayerAffected) { return; }
            localPlayerAffected = true;
            localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() => localPlayerAffected = false, "scp3482", "antileft_effect", curable: false));
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
