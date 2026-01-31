using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static ItemSCPs.Plugin;
//Start
namespace ItemSCPs.Items.Rat
{
    internal class SCP005Behavior : PhysicsProp
    {
        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 005 behavior");
        }

        public override void Update()
        {
            base.Update();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (!(playerHeldBy == null) && IsOwner && Physics.Raycast(new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward), out var hitInfo, 3f, 2816))
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