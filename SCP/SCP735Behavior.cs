using Dawn.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;
using static ItemSCPs.SCP.SCP735Behavior;

// TODO: Add config to unlock scp documents after you scan the scps

namespace ItemSCPs.SCP
{
    internal class SCP735Behavior : PhysicsProp, ISCP, ISingletonItem // TODO: Player cant hear it when theyre dead
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public AudioSource audioSource = null!;

        public AudioClip[] monsterDamagePhrases = null!;
        public AudioClip[] nearOtherPlayersPhrases = null!;
        public AudioClip[] playerDiesPhrases = null!;
        public AudioClip[] playerDamagePhrases = null!;
        public AudioClip[] playerFallDamagePhrases = null!;
        public AudioClip[] randomPhrases = null!;

        public Dictionary<Phrase, AudioClip[]> phrases = new Dictionary<Phrase, AudioClip[]>();

        float phraseCooldown;

        PlayerControllerB? previousPlayerHeldBy;

        public enum Phrase
        {
            MonsterDamagePhrases,
            NearOtherPlayersPhrases,
            PlayerDiesPhrases,
            PlayerDamagePhrases,
            PlayerFallDamagePhrases,
            RandomPhrases
        }

        // Configs
        BoundedRange phraseCooldownRange = new BoundedRange(5, 15);
        float nearPlayersRadius = 10f;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.07f, 0.2f, -0.25f);
            itemProperties.rotationOffset = new Vector3(80, 0, 90);
            itemProperties.floorYOffset = 90;
        }

        public override void Start()
        {
            base.Start();

            phrases.Add(Phrase.MonsterDamagePhrases, monsterDamagePhrases);
            phrases.Add(Phrase.NearOtherPlayersPhrases, nearOtherPlayersPhrases);
            phrases.Add(Phrase.PlayerDiesPhrases, playerDiesPhrases);
            phrases.Add(Phrase.PlayerDamagePhrases, playerDamagePhrases);
            phrases.Add(Phrase.PlayerFallDamagePhrases, playerFallDamagePhrases);
            phrases.Add(Phrase.RandomPhrases, randomPhrases);
        }

        public override void Update()
        {
            base.Update();

            if (playerHeldBy == null) { return; }
            previousPlayerHeldBy = playerHeldBy;
            if (localPlayer != playerHeldBy) { return; }
            if (TESTING.immunity) { return; }

            if (phraseCooldown > 0)
                phraseCooldown -= Time.deltaTime;

            if (phraseCooldown <= 0)
            {
                if (previousPlayerHeldBy.NearOtherPlayers(nearPlayersRadius) && UnityEngine.Random.Range(0, 3) == 0)
                {
                    SpeakPhrase(Phrase.NearOtherPlayersPhrases);
                }
                else
                {
                    SpeakPhrase(Phrase.RandomPhrases);
                }
            }
        }

        public void SpeakPhrase(Phrase phrase, bool overrideIfPlaying = false)
        {
            if (audioSource.isPlaying && !overrideIfPlaying) { return; }

            phraseCooldown = phraseCooldownRange.GetRandomInRange(Utils.randomLocal);

            int index = UnityEngine.Random.Range(0, phrases[phrase].Length);
            SpeakPhraseRpc(phrase, index);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void SpeakPhraseRpc(Phrase phrase, int index)
        {
            logger.LogDebug("Speaking phrase: " + phrase.ToString());

            AudioClip[] clips = phrases[phrase];
            AudioClip clip = clips[index];
            audioSource.Stop();
            audioSource.spatialBlend = previousPlayerHeldBy != null && previousPlayerHeldBy == localPlayer ? 0 : 1;
            audioSource.clip = clip;
            audioSource.Play();
            RoundManager.Instance.PlayAudibleNoise(transform.position, audioSource.maxDistance, audioSource.volume);
            WalkieTalkie.TransmitOneShotAudio(audioSource, clip, 0.85f);
        }
    }

    [HarmonyPatch]
    internal class SCP735Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        public static void KillPlayerPostfix(PlayerControllerB __instance)
        {
            try
            {
                if (__instance != localPlayer || __instance.currentlyHeldObjectServer == null || __instance.currentlyHeldObjectServer is not SCP735Behavior) { return; }
                __instance.currentlyHeldObjectServer?.GetComponent<SCP735Behavior>()?.SpeakPhrase(SCP735Behavior.Phrase.PlayerDiesPhrases, overrideIfPlaying: true);
            }
            catch (Exception e)
            {
                logger.LogError(e);
                return;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
        public static void DamagePlayerPostfix(PlayerControllerB __instance, CauseOfDeath causeOfDeath)
        {
            if (__instance != localPlayer || __instance.isPlayerDead || __instance.currentlyHeldObjectServer == null || __instance.currentlyHeldObjectServer is not SCP735Behavior) { return; }

            switch (causeOfDeath)
            {
                case CauseOfDeath.Gravity:
                    __instance.currentlyHeldObjectServer?.GetComponent<SCP735Behavior>()?.SpeakPhrase(Phrase.PlayerFallDamagePhrases);
                    break;
                case CauseOfDeath.Mauling:
                    __instance.currentlyHeldObjectServer?.GetComponent<SCP735Behavior>()?.SpeakPhrase(Phrase.MonsterDamagePhrases);
                    break;
                case CauseOfDeath.Stabbing:
                    __instance.currentlyHeldObjectServer?.GetComponent<SCP735Behavior>()?.SpeakPhrase(Phrase.MonsterDamagePhrases);
                    break;
                case CauseOfDeath.Scratching:
                    __instance.currentlyHeldObjectServer?.GetComponent<SCP735Behavior>()?.SpeakPhrase(Phrase.MonsterDamagePhrases);
                    break;
                default:
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientClientRpc))]
        public static void DamagePlayerFromOtherClientClientRpcPostfix(PlayerControllerB __instance)
        {
            if (__instance != localPlayer || __instance.isPlayerDead || __instance.currentlyHeldObjectServer == null || __instance.currentlyHeldObjectServer is not SCP735Behavior) { return; }
            __instance.currentlyHeldObjectServer?.GetComponent<SCP735Behavior>()?.SpeakPhrase(Phrase.PlayerDamagePhrases);
        }
    }
}