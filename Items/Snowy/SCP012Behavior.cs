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

namespace ItemSCPs.Items.Snowy
{
    internal class SCP012Behavior : PhysicsProp // TODO: Broken fix this shiz // TODO: Make this work with SCP-714
    {

        public AudioSource ItemSFX;

        public AudioClip[] SpeechSFX;

        float activationRange;
        float forceLookIntensity;
        float forceWalkIntensity;
        float deathZone;
        int damagePerSecond;

        PlayerControllerB targetPlayer;
        AudioSource targetPlayerSFX { get { return localPlayer.currentVoiceChatAudioSource; } }

        float timeSinceLastSpeech = 0f;
        float timeSinceDamagePlayer = 0f;

        public override void Start()
        {
            base.Start();
            logger.LogDebug("Starting 012 behavior");

            activationRange = ConfigManager.config012ActivationRange.Value;
            forceLookIntensity = ConfigManager.config012ForceLookIntensity.Value;
            forceWalkIntensity = ConfigManager.config012ForceWalkIntensity.Value;
            deathZone = ConfigManager.config012DeathZone.Value;
            damagePerSecond = ConfigManager.config012DamagePerSecond.Value;
        }

        public override void Update()
        {
            base.Update();

            if (targetPlayer == null)
            {
                foreach (var player in StartOfRound.Instance.allPlayerScripts)
                {
                    if (Vector3.Distance(player.transform.position, transform.position) < activationRange && !player.isPlayerDead && player.isPlayerControlled)
                    {
                        targetPlayer = player;
                        return;
                    }
                }
            }
            else
            {
                if (Vector3.Distance(targetPlayer.transform.position, transform.position) < activationRange && !targetPlayer.isPlayerDead && targetPlayer.isPlayerControlled)
                {
                    if (!ItemSFX.isPlaying) { ItemSFX.Play(); }

                    targetPlayer.turnCompass.LookAt(transform);
                    targetPlayer.transform.rotation = Quaternion.Lerp(targetPlayer.transform.rotation, targetPlayer.turnCompass.rotation, forceLookIntensity * Time.deltaTime);

                    // TODO: Move player in direction of item slowly

                    if (Vector3.Distance(targetPlayer.transform.position, transform.position) < deathZone)
                    {
                        timeSinceDamagePlayer += Time.deltaTime;
                        timeSinceLastSpeech += Time.deltaTime;

                        FreezeTargetPlayer(true);
                        // TODO: Make sure player cant move while moving them closer

                        if (timeSinceLastSpeech >= 20f)
                        {
                            PlayRandomSpeechSFXClientRpc();
                            timeSinceLastSpeech = 0f;
                        }

                        if (timeSinceDamagePlayer >= 1f)
                        {
                            targetPlayer.MakeCriticallyInjured(true);
                            targetPlayer.DamagePlayer(damagePerSecond, true, true, CauseOfDeath.Stabbing, 0);
                        }
                    }
                }
                else
                {
                    FreezeTargetPlayer(false);
                    ItemSFX.Stop();
                    targetPlayer = null;
                }
            }
        }

        public void FreezeTargetPlayer(bool on)
        {
            targetPlayer.disableLookInput = on;
            targetPlayer.disableMoveInput = on;
            targetPlayer.disableInteract = on;
        }

        // RPCs
        [ClientRpc]
        private void PlayRandomSpeechSFXClientRpc()
        {
            if (localPlayer == targetPlayer)
            {
                targetPlayerSFX.PlayOneShot(SpeechSFX[UnityEngine.Random.Range(0, SpeechSFX.Length - 1)]);
            }
        }
    }
}