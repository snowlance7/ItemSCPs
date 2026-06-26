using Dusk;
using PSCPLibrary;
using System;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP005Behavior : PhysicsProp // TODO: Use a spherecast instead of raycasting
    {
        public override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            if (Configs.MultipleInstances[itemProperties.itemName]) { return; }
            if (FindObjectsOfType<SCP005Behavior>().Length <= 1) { return; }
            logger.LogDebug($"Only one {itemProperties.name} instance can be spawned, despawning duplicate");
            NetworkObject.Despawn(destroy: true);
        }

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
    }
}