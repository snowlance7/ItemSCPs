using BepInEx.Logging;
using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static ItemSCPs.Plugin;
using System.Net;
using System;
using LethalLib.Modules;
using Unity.Services.Authentication.Generated;
using HarmonyLib;



//Start
namespace ItemSCPs.Items.Rat.SCP207
{
    internal class SCP207Behavior : PhysicsProp
    {
        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public static bool slStyle;
        public static float speed;

        public static float timeSinceDamaged = 0f;
        public static float timeUntilDeath;
        public static float timeUntilDebuff;
        public AudioClip gulpGulpgulp;
        
        private string grabTooltip = "Drink [E]";

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 207 behavior");
            slStyle = ConfigManager.config207SCPSLstyle.Value;
            speed = ConfigManager.config207Speed.Value;
            

        }

        public override void Update()
        {
            base.Update();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            AudioSource playerAudio = playerHeldBy.itemAudio;
            playerAudio.PlayOneShot(gulpGulpgulp, 50f);
            logger.LogDebug(playerHeldBy.movementSpeed);
            //playerHeldBy.health += 10;

            playerHeldBy.movementSpeed = speed - 0.4f;
            if (SCP207Manager.Instance == null)
            {
                logger.LogDebug("SCP207Manager instance is null. Creating a new instance.");
                SCP207Manager.Init();
            }
            if (playerHeldBy != null)
            {
                if (!slStyle)
                {
                    timeUntilDebuff = UnityEngine.Random.Range(30, 150);
                    timeUntilDeath = UnityEngine.Random.Range(30, 150);
                    SCP207Manager.Instance.instantKillingPlayer(timeUntilDebuff, playerHeldBy, timeUntilDeath, speed);
                }
                else
                {
                    SCP207Manager.Instance.slowKillingPlayer(playerHeldBy, speed);
                }


                playerHeldBy.DespawnHeldObject();
            }
        }
    }

}