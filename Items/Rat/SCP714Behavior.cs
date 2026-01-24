/*
using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static SCPItems.Plugin;
//using System.Speech.Synthesis;
using System.Net;
using System;
using LethalLib.Modules;
using WearableItemsAPI;
using System.Collections;
using SCPItems.Items.Rat.SCP207;

// TODO: Add Text To Speech
// TODO: Make insanity increase and at longer times add drunkness and fear

namespace SCPItems.Items.Rat
{
    internal class SCP714Behavior : WearableItem
    {
        private static ManualLogSource logger = LoggerInstance;
        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }
        public static bool ignoreSpeed;
        public bool worn = false;
        public override void Start()
        {
            base.Start();
            ignoreSpeed = false;

            WearSlot = WearableSlot.LeftArm; // TEMP
            showWearable = true;
            showWearableOnClient = true;

            logger.LogDebug("Starting 714 behavior.");
        }

        public override void Update()
        {
            logger.LogDebug("Before base update");
            base.Update();
            if (playerHeldBy == null)
            {
                return;
            }
            PlayerControllerB player = playerHeldBy;
            if (SCP207Manager.Instance != null)
            {
                ignoreSpeed = SCP207Manager.Instance.effect;
                if (worn && ignoreSpeed)
                {
                    player.movementSpeed = 5f;
                }
            }

        }
        private IEnumerator headacheTime(PlayerControllerB player)
        {
            while (worn == true)
            {
                player.drunkness = 0.5f;
                yield return new WaitForSeconds(10);
            }

        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (buttonDown)
            {
                //Wear(WearableSlot.LeftArm, showWearable: true, showWearableOnClient: true, new Vector3(0f, 0.5f, 0f));
                Wear();
            }
        }

        public override void Wear()
        {
            //base.Wear(slot, showWearable, showWearableOnClient, wearablePositionOffset); // REDO THIS, ASK SNOWY HOW
            PlayerControllerB player = playerHeldBy;
            if (playerHeldBy == null)
            {
                return;
            }
            player.movementSpeed = 2.5f;
            player.drunkness = 0.3f;
            headacheTime(player);
            if (SCP207Manager.Instance != null)
            {
                SCP207Manager.Instance.scp714on = true;
            }
            worn = true;
            base.Wear();

            //ItemSFX.PlayOneShot(WearSFX);

        }

        public override void UnWear(bool discard = false)
        {
            //ItemSFX.PlayOneShot(DeactivateSFX);
            worn = false;
            base.UnWear();
            PlayerControllerB player = playerHeldBy;
            if (playerHeldBy == null)
            {
                return;
            }
            if (SCP207Manager.Instance != null)
            {
                SCP207Manager.Instance.scp714on = false;
            }
            player.movementSpeed = 5f;
            player.drunkness = 0f;
        }
    }
}
*/