using GameNetcodeStuff;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

// UPDATE: Add ability to kill enemies too, use Utils.GetTopOfObjectRender to get where it should appear when it kills

namespace ItemSCPs.SCP
{
    internal class SCP689Behavior : PhysicsProp, ISCP, ISingletonItem
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public static SCP689Behavior? Instance {  get; private set; }

        public Collider collider = null!;

        public static HashSet<PlayerControllerB> targetPlayers = new HashSet<PlayerControllerB>();

        static Vector3 lastPosition;

        bool inLOS;

        float timeNotInLOS;
        static float timeSinceDisappearing;
        float timeSinceAppearing;

        static float nextAppearTime;

        static float killCooldown = 5f;
        static float grace = 0.1f;

        static bool inShipPhase => (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) && !Utils.inTestRoom;

        [InitConfig]
        public static void InitConfigs()
        {
            killCooldown = PluginInstance.Config.Bind("SCP-689 Options", "SCP-689 | Kill Cooldown", 5f, "The amount of time in seconds for SCP-689 to disappear again after killing someone and not being looked at.").Value;
            grace = PluginInstance.Config.Bind("SCP-689 Options", "SCP-689 | Grace", 0.1f, "The amount of time in seconds SCP-689 needs to be out of sight to disappear").Value;
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0f, 0.1f, -0.06f);
            itemProperties.rotationOffset = new Vector3(90, 90, 0);
            itemProperties.floorYOffset = 90;
            itemProperties.verticalOffset = -0.05f;
            itemProperties.twoHanded = false;
            itemProperties.allowDroppingAheadOfPlayer = true;
        }

        [StaticUpdate]
        public static void StaticUpdate() // Called by network handler update
        {
            if (!NetworkHandler.Instance.IsServer) { return; }

            if (targetPlayers.Count > 0)
                targetPlayers.RemoveWhere(p => p == null || !p.isPlayerControlled);

            if (Instance != null) { return; }

            if (inShipPhase)
            {
                lastPosition = Vector3.zero;
                return;
            }

            timeSinceDisappearing += Time.deltaTime;

            if (timeSinceDisappearing < nextAppearTime) { return; }

            if (targetPlayers.Count == 0 && lastPosition == Vector3.zero) { return; }

            PlayerControllerB? targetPlayer = GetRandomPlayer();
            if (targetPlayer == null || !targetPlayer.isPlayerControlled) { return; }

            timeSinceDisappearing = 0f;
            Utils.SpawnItem(ItemSCPsKeys.SCP689, targetPlayer.transform.position);
            SnowyLib.NetworkHandler.Instance.KillPlayerRpc(targetPlayer.actualClientId);
            targetPlayers.Remove(targetPlayer);
        }

        public override void OnNetworkPostSpawn()
        {
            if (Instance != null && Instance != this) { return; }

            Instance = this;
            base.OnNetworkPostSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == null || Instance != this) { return; }
            Instance = null;
            base.OnNetworkDespawn();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            if (!base.IsOwner || !isPocketed) { return; }
            SetNextAppearTimeRpc();
            int slot = Array.IndexOf(playerHeldBy.ItemSlots, this);
            playerHeldBy.DestroyItemInSlotAndSync(slot);
        }

        public override void Start()
        {
            base.Start();
            lastPosition = transform.position;
        }

        public override void Update()
        {
            base.Update();

            if (!IsServer) { return; }

            timeSinceAppearing += Time.deltaTime;

            // Visibility check
            inLOS = isHeld || isHeldByEnemy || playerHeldBy != null || inShipPhase;

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !player.isPlayerControlled || TESTING.immunity) { continue; }
                if (!player.HasLineOfSightToPosition(collider.bounds.center, width: 50, range: 2000)) { continue; }
                if (!TESTING.immunity)
                    targetPlayers.Add(player);
                inLOS = true;
            }

            if (inLOS || targetPlayers.Count == 0 || timeSinceAppearing < killCooldown)
            {
                timeNotInLOS = 0f;
                return;
            }

            timeNotInLOS += Time.deltaTime;
            if (timeNotInLOS < grace) { return; }

            nextAppearTime = UnityEngine.Random.Range(15, 20);
            lastPosition = transform.position;
            NetworkObject.Despawn(destroy: true);
        }

        public static PlayerControllerB? GetRandomPlayer()
        {
            if (!targetPlayers.Any(x => x != null && !x.isPlayerAlone && x.isPlayerControlled))
                return targetPlayers.GetRandom();

            return targetPlayers.Where(x => x != null && !x.isPlayerAlone && x.isPlayerControlled).GetRandom();
        }

        [Rpc(SendTo.Server)]
        public void SetNextAppearTimeRpc()
        {
            nextAppearTime = UnityEngine.Random.Range(15, 20);
            lastPosition = transform.position;
        }
    }
}
