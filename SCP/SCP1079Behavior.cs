using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using SnowyLib;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ItemSCPs.Plugin;
using static SnowyLib.StatusEffectController;

namespace ItemSCPs.SCP
{
    internal class SCP1079Behavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioClip[] chewingSounds;
        public GameObject pinkBloodSplatterPrefab;
        public GameObject pinkFootPrintPrefab;
        public GameObject decalProjectorPrefab;
        public AudioClip sizzleSFX;
#pragma warning restore CS8618

        static int candiesEatenByLocalPlayer;

        float timeSinceCandyDecrement;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.05f, 0.11f, -0.05f);
            itemProperties.rotationOffset = new Vector3(180f, 90f, -15f);
            itemProperties.floorYOffset = 90;

            itemProperties.canBeGrabbedBeforeGameStart = true;
            itemProperties.canBeInspected = true;
            itemProperties.toolTips = ["Eat Candy [LMB]"];
        }

        public override void Update()
        {
            base.Update();

            if (candiesEatenByLocalPlayer > 0)
            {
                timeSinceCandyDecrement += Time.deltaTime;

                if (timeSinceCandyDecrement > 30f)
                {
                    timeSinceCandyDecrement = 0f;
                    candiesEatenByLocalPlayer--;
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            RoundManager.PlayRandomClip(playerHeldBy.itemAudio, chewingSounds);
            candiesEatenByLocalPlayer++;

            int bloodDropAmount = 2 * candiesEatenByLocalPlayer;
            AudioClip sizzle = sizzleSFX;

            localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
            {
                if (candiesEatenByLocalPlayer > 1)
                    localPlayer.itemAudio.PlayOneShot(sizzle);

                localPlayer.StatusEffectController().ApplyEffect(new DistributedActionEffect(() =>
                {
                    DropBlood();

                }, bloodDropAmount, "SCP-1079", "Pink Blood Secretion", 10f, true, true, (existing, incoming) => ConflictResult.Allow));

            }, "SCP-1079", "Pink Blood Secretion", UnityEngine.Random.Range(10f, 15f), onConflict: (existing, incoming) => ConflictResult.Allow));

            EatCandyServerRpc(playerHeldBy.actualClientId, candiesEatenByLocalPlayer);
        }

        public void DropBlood()
        {
            logger.LogDebug("Dropping blood");
        }

        [ServerRpc(RequireOwnership = false)]
        public void EatCandyServerRpc(ulong clientId, int candiesEaten)
        {
            if (!IsServer) { return; }
            EatCandyClientRpc(clientId, candiesEaten);
        }

        [ClientRpc]
        public void EatCandyClientRpc(ulong clientId, int candiesEaten)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { logger.LogError("Couldnt find player with id: " + clientId); return; }

            int damage = (int)(4f * Mathf.Pow(1.84f, candiesEaten - 1));
            int amount = 3 * candiesEaten * candiesEaten;
            float interval = 1.2f / Mathf.Pow(1.35f, candiesEaten - 1);

            logger.LogDebug("Dropping pink blood");
            //PinkBloodManager.Instance(player)?.DropPinkBlood(amount, interval, candiesEaten <= 1 ? 0 : damage);
        }
    }
}