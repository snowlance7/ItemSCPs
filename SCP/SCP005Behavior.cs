using UnityEngine;

namespace ItemSCPs.SCP
{
    internal class SCP005Behavior : PhysicsProp
    {
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }
            if (Physics.Raycast(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), out var hitInfo, 3f, 2816))
            {
                DoorLock component = hitInfo.transform.GetComponent<DoorLock>();
                //isBigDoor door = hitInfo.transform.GetComponent<isBigDoor>();
                //figure out how to get the door hitbox and call it
                if (component != null && component.isLocked && !component.isPickingLock)
                {
                    component.UnlockDoorSyncWithServer();
                }
            }
        }
    }
}