using PSCPLibrary;
using PSCPLibrary.Interfaces;
using System.Linq;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP005Behavior : PhysicsProp, ISCP // TODO: Use a spherecast instead of raycasting
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        const float unlockableDistance = 1f;

        public override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            if (Configs.MultipleInstances[itemProperties.itemName]) { return; }
            if (FindObjectsOfType<SCP005Behavior>().Length <= 1) { return; }
            logger.LogDebug($"Only one {itemProperties.name} instance can be spawned, despawning duplicate");
            NetworkObject.Despawn(destroy: true);
        }

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
            /*if (Physics.Raycast(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), out var hitInfo, doorDistance, 2816))
            {
                DoorLock component = hitInfo.transform.GetComponent<DoorLock>();
                if (component != null && component.isLocked && !component.isPickingLock)
                {
                    component.UnlockDoorSyncWithServer();
                }
                return;
            }*/

            //RaycastHit[] hits = Physics.RaycastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), doorDistance);
            RaycastHit[] hits = Physics.SphereCastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), unlockableDistance); // TODO
            foreach (var hit in hits) // TODO: Test this
            {
                if (hit.collider.CompareTag("PoweredObject"))
                {
                    TerminalAccessibleObject? terminalAccessibleObject = hit.collider.gameObject.GetComponent<TerminalAccessibleObject>();
                    if (terminalAccessibleObject != null && terminalAccessibleObject.isBigDoor && !terminalAccessibleObject.isDoorOpen)
                        terminalAccessibleObject.SetDoorOpenServerRpc(true);
                }

                DoorLock? component = hit.transform.GetComponent<DoorLock>();
                if (component != null && component.isLocked && !component.isPickingLock)
                    component.UnlockDoorSyncWithServer();
            }

            foreach (MonoBehaviour unlockable in FindObjectsOfType<MonoBehaviour>().OfType<ISCP005Unlockable>()) // TODO: Test this
            {
                if ((unlockable.gameObject.transform.position - playerHeldBy.gameplayCamera.transform.position).sqrMagnitude < unlockableDistance * unlockableDistance)
                    ((ISCP005Unlockable)unlockable).Unlock();
            }
        }
    }
}