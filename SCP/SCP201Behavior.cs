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
using static ItemSCPs.Plugin;
using System.Net;
using System;
using LethalLib.Modules;
using System.Threading;
using System.Collections;
using Unity.Services.Authentication.Generated;

namespace ItemSCPs.SCP
{
    public class SCP201Behavior : PhysicsProp
    {
        private PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public AudioSource ItemSFX;

        float activationRange;
        int damagePerSecond;
        AudioSource targetPlayerSFX { get { return localPlayer.GetComponent<AudioSource>(); } }

        public static Dictionary<PlayerControllerB, bool> Playerbools = new Dictionary<PlayerControllerB, bool>();
        public static Dictionary<PlayerControllerB, bool> Player2ndBools = new Dictionary<PlayerControllerB, bool>();

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 201 behavior");

            activationRange = ConfigManager.config201ActivationRange.Value;
            damagePerSecond = ConfigManager.config201DamagePerSecond.Value;
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player != null && player.isPlayerControlled)
                {
                    int tem = UnityEngine.Random.Range(0, 1);
                    if (tem == 0)
                    {
                        Playerbools[player] = true;
                    }
                    else
                    {
                        Playerbools[player] = false;
                    }
                }

            }
        }
        public override void Update()
        {
            base.Update();
            foreach (var player in StartOfRound.Instance.allPlayerScripts)
            {
                if (Vector3.Distance(player.transform.position, transform.position) < activationRange)
                {
                    if (Playerbools[player] == true)
                    {
                        if (player != null)
                        {
                            Playerbools[player] = false;

                            float temNum = UnityEngine.Random.Range(10, 60);
                            killingPlayer(temNum, player);
                        }
                    }
                }

            }
        }
        public void killingPlayer(float numTemm, PlayerControllerB playerr)
        {
            logger.LogDebug("201 will kill in " + numTemm);
            StartCoroutine(KillingPlayerCoroutine(numTemm, playerr));
        }
        private IEnumerator KillingPlayerCoroutine(float temNum, PlayerControllerB playerr)
        {

            //testing to see if player from playerbools will get the player and if not then playerr
            //atm is not executing this command
            yield return new WaitForSecondsRealtime(temNum);
            playerr.MakeCriticallyInjured(true);
            playerr.DamagePlayer(damagePerSecond, true, true, CauseOfDeath.Stabbing, 0);
            Destroy(playerr.deadBody.gameObject);
            logger.LogDebug("201 has killed");
        }


    }
}