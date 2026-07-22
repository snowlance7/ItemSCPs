using Dawn.Utils;
using GameNetcodeStuff;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Authentication.Generated;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP689Behavior : PhysicsProp, ISCP, ISingletonItem
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public static SCP689Behavior? Instance {  get; private set; }

        public SkinnedMeshRenderer renderer = null!;
        public Collider collider = null!;

        public static HashSet<PlayerControllerB> targetPlayers = new HashSet<PlayerControllerB>();

        static Vector3 lastPosition;

        bool inLOS;
        bool isVisible = true;

        static float timeSinceDisappearing;
        static float timeSinceAppearing;

        static float nextAppearTime;

        static float killCooldown = 5f;

        public static void InitConfigs()
        {
            killCooldown = PluginInstance.Config.Bind("SCP-689 Options", "SCP-689 | Kill Cooldown", 5f, "The amount of time in seconds for SCP-689 to disappear again after killing someone and not being looked at.").Value;
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0f, 0.1f, -0.06f);
            itemProperties.rotationOffset = new Vector3(90, 90, 0);
            itemProperties.floorYOffset = 90;
            itemProperties.verticalOffset = -0.05f;
            itemProperties.twoHanded = false;
        }

        public static void StaticUpdate() // Called by network handler update
        {
            if (Instance != null || targetPlayers.Count == 0) { return; }

            targetPlayers.RemoveWhere(p => p == null || !p.isPlayerControlled);

            if (targetPlayers.Count == 0) { return; }

            if (StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }

            timeSinceDisappearing += Time.deltaTime;

            if (timeSinceDisappearing < nextAppearTime) { return; }
            PlayerControllerB? targetPlayer = GetRandomPlayer();
            if (targetPlayer == null) { return; }
            Utils.SpawnItem(ItemSCPsKeys.SCP689, targetPlayer.transform.position);
            SnowyLib.NetworkHandler.Instance.KillPlayerRpc(targetPlayer.actualClientId);
            timeSinceDisappearing = 0f;
        }

        public override void OnNetworkPostSpawn()
        {
            if (Instance != null && Instance != this) { return; }

            Instance = this;
            base.OnNetworkPostSpawn();
        }

        public override void OnNetworkDespawn()
        {
            nextAppearTime = UnityEngine.Random.Range(15, 20);
            if (Instance == null || Instance != this) { return; }
            Instance = null;
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

            inLOS = localPlayer.HasLineOfSightToPosition(collider.bounds.center, width: 50, range: 2000); // TODO: Test this

            if (!IsServer) { return; }

            timeSinceAppearing = isVisible ? timeSinceAppearing + Time.deltaTime : 0f;
            timeSinceDisappearing = !isVisible ? timeSinceDisappearing + Time.deltaTime : 0f;

            // Visibility check
            inLOS = isHeld || isHeldByEnemy || playerHeldBy != null || StartOfRound.Instance.shipIsLeaving || StartOfRound.Instance.inShipPhase;

            if (isVisible)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (player == null || !player.isPlayerControlled) { continue; }
                    if (!player.HasLineOfSightToPosition(collider.bounds.center, width: 50, range: 2000)) { continue; }
                    if (!TESTING.immunity)
                        targetPlayers.Add(player);
                    inLOS = true;
                }
            }

            targetPlayers.RemoveWhere(p => p == null || !p.isPlayerControlled);

            if (isVisible)
            {
                if (inLOS || targetPlayers.Count == 0 || timeSinceAppearing < killCooldown) { return; }
                isVisible = false;
                nextAppearTime = UnityEngine.Random.Range(15, 20);
                lastPosition = transform.position;
                TeleportRpc(Vector3.zero, false);
            }
            else
            {
                if (timeSinceDisappearing < nextAppearTime) { return; }

                if (targetPlayers.Count == 0)
                {
                    isVisible = true;
                    TeleportRpc(lastPosition, true);
                    return;
                }

                PlayerControllerB? targetPlayer = GetRandomPlayer();
                if (targetPlayer == null || !targetPlayer.isPlayerControlled) { return; }
                targetPlayers.Remove(targetPlayer);
                isVisible = true;
                TeleportRpc(targetPlayer.transform.position, true, (int)targetPlayer.actualClientId);
            }
        }

        public static PlayerControllerB? GetRandomPlayer()
        {
            if (!targetPlayers.Any(x => x != null && !x.isPlayerAlone && x.isPlayerControlled))
                return targetPlayers.GetRandom();

            return targetPlayers.Where(x => x != null && !x.isPlayerAlone && x.isPlayerControlled).GetRandom();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void TeleportRpc(Vector3 position, bool visible, int killPlayerId = -1)
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
    }
}
