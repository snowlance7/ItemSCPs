using BepInEx.Logging;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.Items.Snowy.SCP983
{
    internal class SCP983Behavior : PhysicsProp
    {
        public AudioSource ItemSFX;

        public AudioClip MonkeyFlipSFX;
        public AudioClip[] BirthdaySongsSFX;

        public NetworkAnimator networkAnimator;
        public Transform CandyDropPosition;
        public GameObject Eyes;
        public Collider ItemCollider;

        PlayerControllerB targetPlayer;
        bool activated = false;

        float timeSinceDelayedUpdate = 0f;

        // Configs
        float distanceToActivate;

        // SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId] TODO: USE THIS FOR PITCH DETECTION?

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 983 behavior");

            // Choose random player to have a birthday
            PlayerControllerB[] players = StartOfRound.Instance.allPlayerScripts;
            int randomIndex = UnityEngine.Random.Range(0, players.Length - 1);
            targetPlayer = players[randomIndex];
        }

        public override void Update()
        {
            base.Update();

            timeSinceDelayedUpdate += Time.deltaTime;
            if (timeSinceDelayedUpdate > 0.2f)
            {
                timeSinceDelayedUpdate = 0f;
                DelayedUpdate();
            }
        }

        public void DelayedUpdate()
        {
            if (!activated && targetPlayer != null && Vector3.Distance(targetPlayer.transform.position, transform.position) < distanceToActivate)
            {
                activated = true;
                Activate();
            }
        }

        public void Activate()
        {
            
        }

        // RPCs

        [ClientRpc]
        private void DoAnimationClientRpc(string animationName)
        {
            logger.LogDebug("Animation: " + animationName);
            //ItemAnimator.SetTrigger(animationName);
        }
    }
}