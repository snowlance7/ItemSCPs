using System;
using UnityEngine;

namespace ItemSCPs.SCP
{
    internal class SCP005Behavior : PhysicsProp
    {
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

            RaycastHit[] hits = Physics.RaycastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), doorDistance);
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