using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using WearableItemsAPI;
using static ItemSCPs.Plugin;
using SnowyLib;

namespace ItemSCPs.Items.Snowy
{
    public class SCP268Behavior : WearableObject
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public AudioClip activateSFX;
        public AudioClip deactivateSFX;
#pragma warning restore CS8618

        bool useAltInvisibility = false;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, -0.1f);
            itemProperties.rotationOffset = new Vector3(90, 0, 90);
            itemProperties.floorYOffset = 90;

            itemProperties.toolTips = ["Wear [LMB]"];
            itemProperties.canBeGrabbedBeforeGameStart = true;
        }

        public override void Update()
        {
            base.Update();
            if (playerWornBy != null)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player == playerWornBy) { continue; }
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

        public override void OnWear(PlayerControllerB playerWearing)
        {
            base.OnWear(playerWearing);
            if (localPlayer == playerWornBy)
                audioSource.PlayOneShot(activateSFX);
            if (useAltInvisibility && playerWornBy != null)
                Utils.MakePlayerInvisible(playerWornBy, true);
        }

        public override void OnUnWear()
        {
            if (localPlayer == playerWornBy)
                audioSource.PlayOneShot(deactivateSFX);
            if (useAltInvisibility && playerWornBy != null)
                Utils.MakePlayerInvisible(playerWornBy, false);
            base.OnUnWear();
        }
    }

    [HarmonyPatch]
    internal class SCP268Patches
    {
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