/*
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using static ItemSCPs.Plugin;

namespace ItemSCPs.Items.Snowy
{
    internal class SCP268Behavior : PhysicsProp
    {
        public AudioSource ItemSFX;

        public AudioClip ActivateSFX;
        public AudioClip DeactivateSFX;
        public AudioClip UseSFX;

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 268 behavior.");
        }

        public override void Update()
        {
            base.Update();
            if (playerWornBy != null)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player.HasLineOfSightToPosition(playerWornBy.transform.position))
                    {
                        Vector3 directionToItem = playerWornBy.transform.position - player.transform.position;
                        Vector3 directionAwayFromItem = -directionToItem;

                        Quaternion lookAwayRotation = Quaternion.LookRotation(directionAwayFromItem);

                        player.transform.rotation = Quaternion.Lerp(player.transform.rotation, lookAwayRotation, 0.5f * Time.deltaTime);
                    }
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                base.Wear();
            }
        }

        public override void Wear()
        {
            base.Wear();
            StartCoroutine(PlayWearSFXCoroutine());
        }

        public override void UnWear(bool discard = false)
        {
            ItemSFX.PlayOneShot(DeactivateSFX);
            base.UnWear();
        }

        public IEnumerator PlayWearSFXCoroutine()
        {
            ItemSFX.PlayOneShot(UseSFX);

            yield return new WaitForSeconds(3f);

            ItemSFX.PlayOneShot(ActivateSFX);
        }
    }

    [HarmonyPatch]
    internal class SCP268Patches
    {
        private static ManualLogSource logger = LoggerInstance;

        // Player invisibility
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.GetAllPlayersInLineOfSight))]
        private static void GetAllPlayersInLineOfSightPostfixPatch(EnemyAI __instance, ref PlayerControllerB[] __result)
        {
            // SCP268 for making player invisible to enemies
            List<PlayerControllerB> invisiblePlayers = new List<PlayerControllerB>();

            foreach (var scp268 in Object.FindObjectsOfType<SCP268Behavior>())
            {
                if (scp268.playerWornBy != null)
                {
                    invisiblePlayers.Add(scp268.playerWornBy);
                }
            }

            if (__result != null)
            {
                __result = __result.Where(x => !invisiblePlayers.Contains(x)).ToArray();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
        private static void CheckLineOfSightForPlayerPostfixPatch(EnemyAI __instance, ref PlayerControllerB __result)
        {
            // SCP268 for making player invisible to enemies
            foreach (var scp268 in Object.FindObjectsOfType<SCP268Behavior>())
            {
                if (scp268.playerWornBy != null && scp268.playerWornBy == __result)
                {
                    __result = null;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForClosestPlayer))]
        private static void CheckLineOfSightForClosestPlayerPostfixPatch(EnemyAI __instance, ref PlayerControllerB __result)
        {
            // SCP268 for making player invisible to enemies
            foreach (var scp268 in Object.FindObjectsOfType<SCP268Behavior>())
            {
                if (scp268.playerWornBy != null && scp268.playerWornBy == __result)
                {
                    __result = null;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPosition))]
        private static void CheckLineOfSightForPositionPostfixPatch(EnemyAI __instance, Vector3 objectPosition, ref bool __result)
        {
            // SCP268 for making player invisible to enemies
            foreach (var scp268 in Object.FindObjectsOfType<SCP268Behavior>())
            {
                if (scp268.playerWornBy != null && objectPosition == scp268.playerWornBy.gameplayCamera.transform.position)
                {
                    __result = false;
                }
            }
        }
    }
}
*/