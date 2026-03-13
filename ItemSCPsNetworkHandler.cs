using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs
{
    internal class ItemSCPsNetworkHandler : NetworkBehaviour // TODO: Test to make sure network handler even works without the gamenetworkmanager start patch???
    {
#pragma warning disable CS8618
        public static ItemSCPsNetworkHandler Instance { get; private set; }
        public GameObject TestingHUDOverlayPrefab;
#pragma warning restore CS8618

        //public NetworkList<ulong> PlayersEffectedBy201 = new NetworkList<ulong>();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            Instance = this;
            logger.LogDebug("NetworkHandler spawned");
            base.OnNetworkSpawn();
        }

        public void Start()
        {
            if (Utils.isBeta)
                TestingHUDOverlay.Init(TestingHUDOverlayPrefab);
        }

        public void Update()
        {
            /*var ui = TestingHUDOverlay.Instance;
            ui.SetLabel1("SprintMeter: " + localPlayer.sprintMeter); // 0-1
            ui.SetLabel2("SprintTime: " + localPlayer.sprintTime); // 11, idk what this does
            ui.SetLabel3("SprintMultiplier: " + localPlayer.sprintMultiplier); // 1-2.5, controls sprint speed */
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
        void MufflePlayerClientRpc(ulong clientId, bool value)
        {
            PlayerControllerB? player = PlayerFromId(clientId);
            if (player == null) { return; }
            Utils.MufflePlayer(player, value);
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