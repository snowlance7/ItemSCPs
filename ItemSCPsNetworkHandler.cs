/*
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    public class ItemSCPsNetworkHandler : NetworkBehaviour
    {
#pragma warning disable CS8618
        public static ItemSCPsNetworkHandler Instance { get; private set; }
#pragma warning restore CS8618

        public NetworkList<ulong> PlayersEffectedBy201 = new NetworkList<ulong>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            }

            Instance = this;
            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ShakePlayerCamerasServerRpc(ScreenShakeType type, float distance, Vector3 position)
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                ShakePlayerCamerasClientRpc(type, distance, position);
            }
        }

        [ClientRpc]
        private void ShakePlayerCamerasClientRpc(ScreenShakeType type, float distance, Vector3 position)
        {
            float num = Vector3.Distance(localPlayer.transform.position, position);
            if (num < distance)
            {
                HUDManager.Instance.ShakeCamera(type);
            }
            else if (num < distance * 2f)
            {
                if ((int)type - 1 >= 0) { HUDManager.Instance.ShakeCamera((ScreenShakeType)((int)type - 1)); }
            }
        }

        [ClientRpc]
        private void GrabObjectClientRpc(ulong id, ulong clientId) // TODO: Figure out how to turn off grab animation
        {
            if (clientId == localPlayer.actualClientId)
            {
                if (localPlayer.ItemSlots.Where(x => x == null).Any())
                {
                    GrabbableObject grabbableItem = NetworkManager.Singleton.SpawnManager.SpawnedObjects[id].gameObject.GetComponent<GrabbableObject>();
                    logger.LogDebug($"Grabbing item with weight: {grabbableItem.itemProperties.weight}");

                    localPlayer.GrabObjectServerRpc(grabbableItem.NetworkObject);
                    grabbableItem.parentObject = localPlayer.localItemHolder;
                    grabbableItem.GrabItemOnClient();
                }
            }
        }

        [ClientRpc]
        private void ChangePlayerSizeClientRpc(ulong clientId, float size)
        {
            PlayerControllerB? playerHeldBy = PlayerFromId(clientId);
            if (playerHeldBy == null) { return; }
            playerHeldBy.thisPlayerBody.localScale = new Vector3(size, size, size);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ChangePlayerSizeServerRpc(ulong clientId, float size)
        {
            if (!IsServer) { return; }
            ChangePlayerSizeClientRpc(clientId, size);
        }
    }

    [HarmonyPatch]
    public class NetworkObjectManager
    {
#pragma warning disable CS8618
        static GameObject networkPrefab;
#pragma warning restore CS8618

        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            logger.LogDebug("Initializing network prefab...");
            if (networkPrefab != null)
                return;

            networkPrefab = (GameObject)Plugin.SnowyModAssets.LoadAsset("Assets/ModAssets/SharedAssets/NetworkHandlerSCPItems.prefab");
            //networkPrefab.AddComponent<NetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (IsServerOrHost)
            {
                var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
*/