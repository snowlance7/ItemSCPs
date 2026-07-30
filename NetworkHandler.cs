using GameNetcodeStuff;
using HarmonyLib;
using ItemSCPs.SCP;
using SnowyLib;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    internal class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; } = null!;

        public AudioClip[] sneezeSFX = null!;
        public AudioClip[] coughSFX = null!;
        public AudioClip[] coughHeavySFX = null!;
        public AudioClip[] heartbeatSlowSFX = null!;
        public AudioClip[] heartbeatFastSFX = null!;

        public enum SoundEffect
        {
            Sneeze,
            Cough,
            CoughHeavy
        }

        public void Update()
        {
            SCP689Behavior.StaticUpdate();
            SCP3482Behavior.StaticUpdate();
            SCP207Behavior.StaticUpdate();

            TESTING.Update();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && Instance != null)
                Instance.gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            Instance = this;
            logger.LogDebug("NetworkHandler spawned");
            base.OnNetworkSpawn();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void PlayPlayerSoundEffectRpc(ulong clientId, SoundEffect soundEffect, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            Transform position = player.bodyParts[bodyPartIndex];
            AudioClip[] clips;

            switch (soundEffect)
            {
                case SoundEffect.Sneeze:
                    clips = sneezeSFX;
                    break;
                case SoundEffect.Cough:
                    clips = coughSFX;
                    break;
                case SoundEffect.CoughHeavy:
                    clips = coughHeavySFX;
                    break;
                default:
                    return;
            }

            Utils.PlaySoundAtPosition(position, clips, volume, true, true, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void DropPinkBloodRpc(Vector3 pos)
        {
            SCP1079Behavior.DropPinkBloodOnLocalClient(pos);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void AddPinkBloodToBodyRpc(ulong clientId)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            SCP1079Behavior.AddPinkBloodToBodyOnLocalClient(player);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void CreateLightFlashRpc(Vector3 position)
        {
            var prefab = ItemSCPsContentHandler.Instance.SCP983?.LightFlashPrefab;
            if (prefab == null) { logger.LogError("Unable to get LightFlashPrefab in CreateLightFlash"); return; }
            Instantiate(prefab, position, Quaternion.identity);
            StunGrenadeItem.StunExplosion(base.transform.position, affectAudio: true, 1f, 10f, 1f, false, null, null);
        }
    }

    [HarmonyPatch]
    public class NetworkHandlerPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void AwakePostFix()
        {
            if (!IsServerOrHost) { return; }
            var networkHandlerHost = UnityEngine.Object.Instantiate(ItemSCPsContentHandler.Instance.ItemSCPsAssets?.NetworkHandlerPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost?.GetComponent<NetworkObject>().Spawn();
        }
    }
}