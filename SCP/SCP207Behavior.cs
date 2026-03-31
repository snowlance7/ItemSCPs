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

namespace ItemSCPs.SCP
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

    internal class SCP207Manager : MonoBehaviour
    {
        //need to find out to play audio as the player

        public static SCP207Manager Instance;
        public bool scp500Taken = false;
        public bool effect;
        public int amountTaken;
        public bool scp714on = false;
        public int recentDamage = 0;
        public bool dead = false;

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = new GameObject("SCP207Manager").AddComponent<SCP207Manager>();
            }
        }
        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
            }


            logger.LogDebug("SCP207Manager has spawned....");
        }

        void Update()
        {
            if (SCP500Behavior.localPlayerAffected)
            {
                logger.LogDebug("WUT");
                //scp500Taken = true;
            }
        }
        public void instantKillingPlayer(float debuffTime, PlayerControllerB playerr, float deathTime, float speed)
        {
            amountTaken++;
            playerr.health += 10;
            logger.LogDebug("207 will end in: " + debuffTime + "/n death will happen in:" + deathTime);
            StartCoroutine(FastKillingPlayerCoroutine(debuffTime, playerr, deathTime, speed));
        }
        private IEnumerator FastKillingPlayerCoroutine(float debuffTime, PlayerControllerB playerr, float deathTime, float speed)
        {

            //might remove debuff but then its just slStyle but better
            effect = true;
            if (amountTaken > 1)
            {
                playerr.movementSpeed = (float)(speed * (1 + 0.25 * amountTaken));
            }
            if (amountTaken == 1)
            {
                yield return new WaitForSeconds(debuffTime);
            }
            playerr.movementSpeed = 2.5f;
            playerr.drunkness = 0.3f;
            HUDManager.Instance.DisplayTip("", "Uh-Oh, I don't feel so well");
            effect = false;
            int i = 0;
            while (i != deathTime)
            {
                yield return new WaitForSeconds(1);
                i++;
                if (scp500Taken)
                {
                    playerr.drunkness = 0f;
                    playerr.movementSpeed = 5f;
                    Destroy(Instance.gameObject);
                }
            }
            if (!scp500Taken)
            {
                playerr.DamagePlayer(100, true, true, CauseOfDeath.Unknown, 0);
                playerr.movementSpeed = 5f;
                logger.LogDebug("201 has killed");
                Destroy(Instance.gameObject);
            }
        }
        public void slowKillingPlayer(PlayerControllerB playerr, float speed)
        {

            logger.LogDebug("Secret Lab Style");
            playerr.health += recentDamage + 10;
            scp500Taken = false;
            StartCoroutine(SlowKillingPlayerCoroutine(playerr, speed));

        }
        private IEnumerator SlowKillingPlayerCoroutine(PlayerControllerB playerr, float speed)
        {
            amountTaken++;
            HUDManager.Instance.DisplayTip("", "Uh-Oh, I don't feel so well");
            effect = true;
            while (!scp500Taken)
            {
                int numm = UnityEngine.Random.Range(1, 10);
                if (amountTaken > 1)
                {
                    numm = (int)(numm * (1 + amountTaken * 0.25));
                    playerr.movementSpeed = (float)(speed * (1 + 0.25 * amountTaken));
                }
                if (scp714on == true)
                {
                    playerr.movementSpeed /= 2;
                }
                if (amountTaken >= 4)
                {
                    Landmine.SpawnExplosion(playerr.transform.position + Vector3.up, true, 6f, 7f);
                    playerr.movementSpeed = 5f;
                    killBehavior(playerr);
                    //UnityEngine.Object.Destroy(Instance.gameObject);
                }
                if (playerr.health - numm <= 0)
                {
                    logger.LogDebug("death is approaching");
                    //playerr.movementSpeed = 5f;
                    dead = true;
                    //scp500Taken = true;
                }

                playerr.DamagePlayer(numm, true, true, CauseOfDeath.Unknown, 0);
                recentDamage = numm;

                int num = UnityEngine.Random.Range(2, 8);
                int num2 = 0;
                while (num2 != num)
                {
                    yield return new WaitForSeconds(1);
                    //if (scp500Taken == true || dead == true)
                    //{
                    //  break;
                    //}
                    num2++;
                }

                //yield return new WaitForSeconds(num);


                if (StartOfRound.Instance.inShipPhase == true) //it for some reason hates this line of code
                {
                    logger.LogDebug("WOAH");
                    killBehavior(playerr);
                    //playerr.movementSpeed = 5f;
                    //UnityEngine.Object.Destroy(Instance.gameObject);
                }
                if (playerr.isPlayerDead == true)
                {
                    logger.LogDebug("bro is dead");
                    killBehavior(playerr);
                    //playerr.movementSpeed = 5f; //Need to reset the stuff once the player leaves the moon and make sure speed is reset after death
                    //UnityEngine.Object.Destroy(Instance.gameObject); //need to reset on ship take off and why isnt it deleting?
                    //logger.LogDebug("bro did not delete"); //I wonder if i can edit the startOfRound statReset script

                }
            }
            logger.LogDebug("JOE JOE MA");

            effect = false;
            //scp500Taken = false;
            //playerr.movementSpeed = 5f;
            killBehavior(playerr);

        }
        private IEnumerator lolDrunk(PlayerControllerB playerr)
        {
            yield return new WaitForSeconds(10f);
            playerr.drunkness = 0.5f;
        }
        public void killBehavior(PlayerControllerB playerr)
        {
            playerr.movementSpeed = 5f;
            logger.LogDebug("wa");
            Destroy(Instance.gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }


    }
}