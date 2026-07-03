using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System.Linq;
using UnityEngine;

namespace ItemSCPs.SCP
{
    internal class SCP005Behavior : PhysicsProp, ISCP, ISingletonItem
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        const float unlockableDistance = 1f;

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

            RaycastHit[] hits = Physics.SphereCastAll(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), unlockableDistance);
            foreach (var hit in hits)
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

            foreach (MonoBehaviour unlockable in FindObjectsOfType<MonoBehaviour>().OfType<ISCP005Unlockable>())
            {
                if ((unlockable.gameObject.transform.position - playerHeldBy.gameplayCamera.transform.position).sqrMagnitude < unlockableDistance * unlockableDistance)
                    ((ISCP005Unlockable)unlockable).Unlock();
            }
        }
    }
}