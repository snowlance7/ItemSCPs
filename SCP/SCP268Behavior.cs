using GameNetcodeStuff;
using HarmonyLib;
using ItemSCPs.SCP;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using WearableItemsAPI;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    public class SCP268Behavior : WearableObject, ISCP, ISingletonItem // TODO: MAKE IT SO TURRET DOESNT SEE YOU
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public static SCP268Behavior? Instance { get; private set; }

#pragma warning disable CS8618
        public AudioSource audioSource;
        public AudioClip activateSFX;
        public AudioClip deactivateSFX;
        public GameObject mesh;
#pragma warning restore CS8618

        bool useAltInvisibility = false; // TODO: Setup config

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0f, 0.15f, -0.17f);
            itemProperties.rotationOffset = new Vector3(90, 0, -105);
            itemProperties.floorYOffset = 90;

            wearableItemProperties.wornPositionOffset = new Vector3(0, 0.27f, 0.07f);
            wearableItemProperties.wornRotationOffset = new Vector3(-30, 0, 0);
        }

        public override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            Instance ??= this;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) { Instance = null; }
            base.OnNetworkDespawn();
        }

        public override void Update() // TODO: Make it so the effects pause when the player is speaking or interacting with the player
        {
            base.Update();
            if (playerWornBy != null)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player != localPlayer) { continue;}
                    if (player == playerWornBy) { continue; }
                    if (TESTING.immunity) { continue; }
                    if (SCP714Behavior.localPlayerAffected) { continue; }
                    bool setInvisible = !TESTING.immunity && !SCP714Behavior.localPlayerAffected && !(PlayerSpeaking() && playerWornBy.HasLineOfSightToPosition(localPlayer.bodyParts[0].position, width: 35));
                    if (player.HasLineOfSightToPosition(playerWornBy.transform.position, width: 50))
                    {
                        Vector3 directionToItem = playerWornBy.transform.position - player.transform.position;
                        Vector3 directionAwayFromItem = -directionToItem;

                        Quaternion lookAwayRotation = Quaternion.LookRotation(directionAwayFromItem);

                        player.transform.rotation = Quaternion.Lerp(player.transform.rotation, lookAwayRotation, 0.5f * Time.deltaTime);
                    }
                }
            }
        }

        bool PlayerSpeaking()
        {
            throw new System.NotImplementedException();
        }

        void SetPlayerInvisible(bool value)
        {
            
        }

        public override void OnWear()
        {
            base.OnWear();
            if (localPlayer == playerWornBy)
                audioSource.PlayOneShot(activateSFX);
            if (useAltInvisibility && playerWornBy != null)
                playerWornBy.MakePlayerInvisible(true);
        }

        public override void OnUnWear()
        {
            if (localPlayer == playerWornBy)
                audioSource.PlayOneShot(deactivateSFX);
            if (useAltInvisibility && playerWornBy != null)
                playerWornBy.MakePlayerInvisible(false);
            base.OnUnWear();
        }
    }

    [HarmonyPatch]
    internal static class SCP268Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.GetAllPlayersInLineOfSight))]
        private static void EnemyAI_GetAllPlayersInLineOfSight_Postfix(EnemyAI __instance, ref PlayerControllerB[] __result)
        {
            try
            {
                if (SCP268Behavior.Instance == null || SCP268Behavior.Instance.playerWornBy == null) { return; }

                if (__result != null)
                    __result = __result.Where(x => x != SCP268Behavior.Instance.playerWornBy).ToArray();
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPlayer))]
        private static void EnemyAI_CheckLineOfSightForPlayer_Postfix(EnemyAI __instance, ref PlayerControllerB __result)
        {
            try
            {
                if (SCP268Behavior.Instance == null || SCP268Behavior.Instance.playerWornBy == null) { return; }

                if (SCP268Behavior.Instance.playerWornBy == __result)
                    __result = null;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForClosestPlayer))]
        private static void EnemyAI_CheckLineOfSightForClosestPlayer_Postfix(EnemyAI __instance, ref PlayerControllerB __result)
        {
            try
            {
                if (SCP268Behavior.Instance == null || SCP268Behavior.Instance.playerWornBy == null) { return; }

                if (SCP268Behavior.Instance.playerWornBy == __result)
                    __result = null;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.CheckLineOfSightForPosition))]
        private static void EnemyAI_CheckLineOfSightForPosition_Postfix(EnemyAI __instance, Vector3 objectPosition, ref bool __result)
        {
            try
            {
                if (SCP268Behavior.Instance == null || SCP268Behavior.Instance.playerWornBy == null) { return; }

                if (objectPosition == SCP268Behavior.Instance.playerWornBy.gameplayCamera.transform.position)
                    __result = false;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.PlayerIsTargetable))]
        private static void EnemyAI_PlayerIsTargetable_Postfix(EnemyAI __instance, PlayerControllerB playerScript, ref bool __result)
        {
            try
            {
                if (SCP268Behavior.Instance == null || SCP268Behavior.Instance.playerWornBy == null) { return; }

                if (SCP268Behavior.Instance.playerWornBy == playerScript)
                    __result = false;
            }
            catch (System.Exception e)
            {
                logger.LogError(e);
                return;
            }
        }
    }
}