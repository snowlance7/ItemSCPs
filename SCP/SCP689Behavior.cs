using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using SnowyLib;
using static ItemSCPs.Plugin;
using System.Linq;
using PSCPLibrary;

namespace ItemSCPs.SCP
{
    internal class SCP689Behavior : PhysicsProp, ISCP
    {
        public static SCP689Behavior? Instance { get; private set; }

        SCPInfo ISCP.SCPInfo => scpInfo;

        public SCPInfo scpInfo = null!;
        public SkinnedMeshRenderer renderer = null!;
        public Collider collider = null!;

        public static HashSet<PlayerControllerB> targetPlayers = new HashSet<PlayerControllerB>();

        static Vector3 lastPosition;

        bool inLOS;
        bool isVisible = true;

        static float timeSinceDisappearing;
        static float timeSinceAppearing;

        static float nextAppearTime;

        const float killCooldown = 5f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0f, 0.1f, -0.06f);
            itemProperties.rotationOffset = new Vector3(90, 90, 0);
            itemProperties.floorYOffset = 90;
            itemProperties.verticalOffset = -0.05f;
        }

        public static void StaticUpdate() // Called by network handler update
        {
            if (Instance != null || targetPlayers.Count == 0) { return; }

            foreach (var player in targetPlayers)
            {
                if (player == null) { continue; }

                if (!player.isPlayerControlled)
                    targetPlayers.Remove(player);
            }

            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }

            timeSinceDisappearing += Time.deltaTime;

            if (timeSinceDisappearing < nextAppearTime) { return; }
            PlayerControllerB? targetPlayer = GetRandomPlayer();
            if (targetPlayer == null) { return; }
            Utils.SpawnItem(ItemSCPsKeys.SCP689, targetPlayer.transform.position);
            ItemSCPsNetworkHandler.Instance.KillPlayerClientRpc(targetPlayer.actualClientId);
            timeSinceDisappearing = 0f;
        }

        public override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            if (Instance != null && Instance != this)
            {
                logger.LogDebug($"Only one {itemProperties.name} instance can be spawned, despawning duplicate");
                NetworkObject.Despawn(destroy: true);
                return;
            }

            Instance = this;
        }

        public override void OnNetworkDespawn()
        {
            if (Instance != null && Instance == this)
            {
                nextAppearTime = UnityEngine.Random.Range(15, 20);
                Instance = null;
            }

            base.OnNetworkDespawn();
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

            timeSinceAppearing += isVisible ? Time.deltaTime : 0f;
            timeSinceDisappearing += !isVisible ? Time.deltaTime : 0f;

            // Visibility check
            inLOS = isHeld || isHeldByEnemy || playerHeldBy != null || StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.inShipPhase;

            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player == null || !player.isPlayerControlled) { continue; }
                if (!player.HasLineOfSightToPosition(transform.position)) { continue; }
                if (!TESTING.immunity)
                    targetPlayers.Add(player);
                inLOS = true;
            }

            foreach (var player in targetPlayers)
            {
                if (player == null) { continue; }

                if (!player.isPlayerControlled)
                    targetPlayers.Remove(player);
            }

            if (isVisible)
            {
                if (inLOS || targetPlayers.Count == 0 || timeSinceAppearing < killCooldown) { return; }
                isVisible = false;
                nextAppearTime = UnityEngine.Random.Range(15, 20);
                lastPosition = transform.position;
                TeleportClientRpc(Vector3.zero, false);
            }
            else
            {
                if (timeSinceDisappearing < nextAppearTime) { return; }

                if (targetPlayers.Count == 0)
                {
                    isVisible = true;
                    TeleportClientRpc(lastPosition, true);
                    return;
                }

                PlayerControllerB? targetPlayer = GetRandomPlayer();
                if (targetPlayer == null || !targetPlayer.isPlayerControlled) { return; }
                isVisible = true;
                lastPosition = transform.position;
                TeleportClientRpc(targetPlayer.transform.position, true, (int)targetPlayer.actualClientId);
            }
        }

        public static PlayerControllerB? GetRandomPlayer()
        {
            if (!targetPlayers.Any(x => !x.isPlayerAlone && x.isPlayerControlled))
                return targetPlayers.GetRandom();

            return targetPlayers.Where(x => !x.isPlayerAlone && x.isPlayerControlled).GetRandom();
        }

        [ClientRpc]
        public void TeleportClientRpc(Vector3 position, bool visible, int killPlayerId = -1)
        {
            logger.LogDebug($"Teleporting to {position}, visible: {visible}");
            parentObject = null;
            transform.position = position;

            isVisible = visible;
            renderer.enabled = visible;
            grabbable = visible;
            grabbableToEnemies = visible;
            collider.enabled = visible;

            fallTime = 0f;
            hasHitGround = false;
            reachedFloorTarget = false;
            startFallingPosition = transform.position;
            targetFloorPosition = GetItemFloorPosition(startFallingPosition);
            FallToGround(randomizePosition: false, justSpawned: false, startFallingPosition);

            if (localPlayer.actualClientId != (ulong)killPlayerId || !localPlayer.isPlayerControlled) { return; }
            localPlayer.KillPlayer(Vector3.zero);
        }

        void ISCP.OnSCP500TakenByLocalPlayer()
        {
            return;
        }

        void ISCP.OnSCP714WearByLocalPlayer()
        {
            return;
        }

        void ISCP.OnSCP714UnWearByLocalPlayer()
        {
            return;
        }
    }
}
