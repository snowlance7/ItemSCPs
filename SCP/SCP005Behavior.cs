using PSCPLibrary;
using System;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP005Behavior : PhysicsProp, ISCP // TODO: Use a spherecast instead of raycasting
    {
        public override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            int maxCount = Configs.MaxSpawnCounts[itemProperties.itemName];
            int spawnCount = FindObjectsOfType<SCP005Behavior>().Length;
            if (spawnCount <= maxCount) { return; }
            logger.LogDebug($"Only {maxCount} {itemProperties.name} instance{(maxCount > 1 ? "s" : "")} can be spawned, despawning duplicate");
            NetworkObject.Despawn(destroy: true);
        }

        SCPInfo ISCP.SCPInfo => scpInfo;

        public SCPInfo scpInfo = null!;

        const float doorDistance = 1f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0.1f, 0);
            itemProperties.rotationOffset = new Vector3(-90, 0, 0);
            itemProperties.floorYOffset = 90;

            itemProperties.toolTips = ["Use Key [LMB]"];
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }
            if (Physics.Raycast(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), out var hitInfo, doorDistance, 2816))
            {
                DoorLock component = hitInfo.transform.GetComponent<DoorLock>();
                if (component != null && component.isLocked && !component.isPickingLock)
                {
                    component.UnlockDoorSyncWithServer();
                }
                return;
            }

            //RaycastHit[] hits = Physics.RaycastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), doorDistance);
            RaycastHit[] hits = Physics.SphereCastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), doorDistance); // TODO
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("PoweredObject"))
                {
                    hit.collider.gameObject.GetComponent<TerminalAccessibleObject>().SetDoorOpenServerRpc(true);
                }
            }
        }

        void ISCP.OnSCP500TakenByLocalPlayer()
        {
            throw new NotImplementedException();
        }

        void ISCP.OnSCP714UnWearByLocalPlayer()
        {
            throw new NotImplementedException();
        }

        void ISCP.OnSCP714WearByLocalPlayer()
        {
            throw new NotImplementedException();
        }
    }
}