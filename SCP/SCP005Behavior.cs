using System;
using UnityEngine;

namespace ItemSCPs.SCP
{
    internal class SCP005Behavior : PhysicsProp
    {
        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0, 0, 0);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;
        }

        public override void Start()
        {
            base.Start();
            Utils.OnFinishGeneratingLevel.AddListener(OnFinishGeneratingLevel);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }
            if (Physics.Raycast(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), out var hitInfo, 3f, 2816))
            {
                DoorLock component = hitInfo.transform.GetComponent<DoorLock>();
                //isBigDoor door = hitInfo.transform.GetComponent<isBigDoor>();
                //figure out how to get the door hitbox and call it

                //[Debug: ItemSCPs] BigDoor(Clone)
                //[Debug: ItemSCPs] PoweredObject <- tag
                //[Debug: ItemSCPs] 0
                //[Debug: ItemSCPs] Default
                if (component != null && component.isLocked && !component.isPickingLock)
                {
                    component.UnlockDoorSyncWithServer();
                }
            }
        }

        private void OnFinishGeneratingLevel()
        {
            throw new NotImplementedException();
        }
    }
}