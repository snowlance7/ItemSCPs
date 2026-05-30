using GameNetcodeStuff;
using HarmonyLib;
using ItemSCPs.SCP;
using SnowyLib;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication.Generated;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    internal class ItemSCPsNetworkHandler : NetworkBehaviour
    {
        public static ItemSCPsNetworkHandler Instance { get; private set; } = null!;
        public AudioClip[] sneezeSFX = null!; // TODO: Assign in unity
        public AudioClip[] coughSFX = null!;
        public AudioClip[] coughHeavySFX = null!;
        public AudioClip[] heartbeatSlowSFX = null!;
        public AudioClip[] heartbeatFastSFX = null!;

        //public NetworkList<ulong> PlayersEffectedBy201 = new NetworkList<ulong>();

        public enum SoundEffect
        {
            Sneeze,
            Cough,
            CoughHeavy
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            Instance = this;
            logger.LogDebug("NetworkHandler spawned");
            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ShakePlayerCamerasServerRpc(ScreenShakeType type, Vector3 position, float range)
        {
            if (!IsServer) { return; }
            ShakePlayerCamerasClientRpc(type, position, range);
        }

        [ClientRpc]
        void ShakePlayerCamerasClientRpc(ScreenShakeType type, Vector3 position, float range)
        {
            float num = Vector3.Distance(localPlayer.transform.position, position);
            if (num < range)
            {
                HUDManager.Instance.ShakeCamera(type);
            }
            else if (num < range * 2f)
            {
                if ((int)type - 1 >= 0) { HUDManager.Instance.ShakeCamera((ScreenShakeType)((int)type - 1)); }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangePlayerSizeServerRpc(ulong clientId, float size)
        {
            if (!IsServer) { return; }
            ChangePlayerSizeClientRpc(clientId, size);
        }

        [ClientRpc]
        private void ChangePlayerSizeClientRpc(ulong clientId, float size)
        {
            PlayerControllerB? playerHeldBy = PlayerFromId(clientId);
            if (playerHeldBy == null) { return; }
            playerHeldBy.thisPlayerBody.localScale = new Vector3(size, size, size);
        }

        [ServerRpc(RequireOwnership = false)]
        public void MufflePlayerServerRpc(ulong clientId, bool value)
        {
            if (!IsServer) { return; }
            MufflePlayerClientRpc(clientId, value);
        }

        [ClientRpc]
        private void MufflePlayerClientRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            player.MufflePlayer(value);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayPlayerSoundEffectServerRpc(ulong clientId, SoundEffect soundEffect, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
        {
            if (!IsServer) { return; }
            PlayPlayerSoundEffectClientRpc(clientId, soundEffect, bodyPartIndex, volume, min3DDistance, max3DDistance, cutoffFrequency, audibleNoiseID);
        }

        [ClientRpc]
        private void PlayPlayerSoundEffectClientRpc(ulong clientId, SoundEffect soundEffect, int bodyPartIndex = 5, float volume = 1f, float min3DDistance = 1f, float max3DDistance = 10f, float cutoffFrequency = 22000, int audibleNoiseID = 0)
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

        [ServerRpc(RequireOwnership = false)]
        public void DropPinkBloodServerRpc(Vector3 pos)
        {
            if (!IsServer) { return; }
            DropPinkBloodClientRpc(pos);
        }

        [ClientRpc]
        private void DropPinkBloodClientRpc(Vector3 pos)
        {
            SCP1079Behavior.DropPinkBloodOnLocalClient(pos);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddPinkBloodToBodyServerRpc(ulong clientId)
        {
            if (!IsServer) { return; }
            AddPinkBloodToBodyClientRpc(clientId);
        }

        [ClientRpc]
        private void AddPinkBloodToBodyClientRpc(ulong clientId)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            SCP1079Behavior.AddPinkBloodToBodyOnLocalClient(player);
        }
    }

    [HarmonyPatch]
    public class NetworkHandlerPatches
    {
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void AwakePostFix()
        {
            if (!IsServerOrHost) { return; }
            var networkHandlerHost = UnityEngine.Object.Instantiate(ItemSCPsContentHandler.Instance.NetworkHandler?.NetworkHandlerPrefab, Vector3.zero, Quaternion.identity);
            networkHandlerHost?.GetComponent<NetworkObject>().Spawn();
        }
    }
}