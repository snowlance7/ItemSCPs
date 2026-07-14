using GameNetcodeStuff;
using HarmonyLib;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP3482Behavior : PhysicsProp, ISCP, ISingletonItem
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public static GameObject? overlay;
        public static bool localPlayerAffected;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.21f, 0.08f, -0.25f);
            itemProperties.rotationOffset = new Vector3(0, -80, 0);
            itemProperties.floorYOffset = 0;
        }

        public static void StaticUpdate()
        {
            if (localPlayerAffected && !SCP714Behavior.localPlayerAffected && !TESTING.immunity)
            {
                if (overlay == null)
                    overlay = Instantiate(ItemSCPsContentHandler.Instance.SCP3482!.Overlay, localPlayer.transform);
            }
            else
            {
                if (overlay != null)
                {
                    Destroy(overlay);
                    overlay = null;
                }
            }
        }

        public override void EquipItem()
        {
            base.EquipItem();
            if (!base.IsOwner) { return; }
            if (TESTING.immunity || SCP714Behavior.localPlayerAffected) { return; }

            localPlayerAffected = true;

            localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
            {
                localPlayerAffected = false;
            }, "scp3482", "antileft_effect", curable: false));
        }
    }

    [HarmonyPatch]
    static class SCP3482Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CalculateNormalLookingInput))]
        private static void PlayerControllerB_CalculateNormalLookingInput_Prefix(ref Vector2 inputVector, PlayerControllerB __instance)
        {
            try
            {
                if (SCP3482Behavior.localPlayerAffected && !SCP714Behavior.localPlayerAffected && !TESTING.immunity)
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
        private static void PlayerControllerB_CalculateSmoothLookingInput_Prefix(ref Vector2 inputVector, PlayerControllerB __instance)
        {
            try
            {
                if (SCP3482Behavior.localPlayerAffected && !SCP714Behavior.localPlayerAffected && !TESTING.immunity)
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

        //[HarmonyPrefix] // TODO: Use transpiler for this
        //[HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItemOnClient))]
        //private static void GrabbableObject_GrabItemOnClient_Postfix(GrabbableObject __instance)
        //{
        //    try
        //    {
        //        if (__instance.IsOwner && SCP3482Behavior.localPlayerAffected && __instance.itemProperties.twoHanded)
        //        {
        //            localPlayer.DropHeldItem(__instance, true, false);
        //        }
        //    }
        //    catch (System.Exception e)
        //    {
        //        logger.LogError(e);
        //        return;
        //    }
        //}

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            try
            {
                if (!SCP3482Behavior.localPlayerAffected || SCP714Behavior.localPlayerAffected || TESTING.immunity) { return; }
                string msg = __instance.chatTextField.text.Replace("left", "", System.StringComparison.OrdinalIgnoreCase);
                __instance.chatTextField.text = msg;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            FieldInfo moveInputField =
                AccessTools.Field(typeof(PlayerControllerB),
                    nameof(PlayerControllerB.moveInputVector));

            MethodInfo clampMethod =
                AccessTools.Method(typeof(SCP3482Patches),
                    nameof(ClampMoveInput));

            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];

                if (codes[i].StoresField(moveInputField))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);

                    yield return new CodeInstruction(
                        OpCodes.Call,
                        clampMethod);
                }
            }
        }

        public static void ClampMoveInput(PlayerControllerB player)
        {
            if (player != localPlayer || !SCP3482Behavior.localPlayerAffected || SCP714Behavior.localPlayerAffected || TESTING.immunity)
                return;

            player.moveInputVector.x = Mathf.Max(player.moveInputVector.x, 0f);
        }
    }
}
