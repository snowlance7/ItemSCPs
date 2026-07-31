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

// UPDATE: Add config to unlock scp documents after you scan the scps
// UPDATE: Make it so the player can hit SCP-735 against walls and other things or even throw it and it responds to every action

namespace ItemSCPs.SCP
{
    internal class SCP735Behavior : PhysicsProp, ISCP, ISingletonItem
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public static SCP735Behavior? Instance { get; private set; }

        public AudioSource audioSource = null!;

        public AudioClip[] monsterDamagePhrases = null!;
        public AudioClip[] nearOtherPlayersPhrases = null!;
        public AudioClip[] playerDiesPhrases = null!;
        public AudioClip[] playerDamagePhrases = null!;
        public AudioClip[] playerFallDamagePhrases = null!;
        public AudioClip[] randomPhrases = null!;

        public Dictionary<Phrase, AudioClip[]> phrases = new Dictionary<Phrase, AudioClip[]>();

        float phraseCooldown;

        public PlayerControllerB? previousPlayerHeldBy;

        public enum Phrase
        {
            MonsterDamagePhrases,
            NearOtherPlayersPhrases,
            PlayerDiesPhrases,
            PlayerDamagePhrases,
            PlayerFallDamagePhrases,
            RandomPhrases
        }

        const float nearPlayersRadius = 10f;

        static BoundedRange phraseCooldownRange = new BoundedRange(5, 15);

        [InitConfig]
        public static void InitConfigs()
        {
            phraseCooldownRange = PluginInstance.Config.Bind("SCP-735 Options", "SCP-735 | Phrase Cooldown", new BoundedRange(5, 15), "The cooldown in which SCP-735 will insult the player while holding it.").Value;
        }

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.07f, 0.2f, -0.25f);
            itemProperties.rotationOffset = new Vector3(80, 0, 90);
            itemProperties.floorYOffset = 90;
            itemProperties.twoHanded = false;
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

        public override void OnNetworkPostSpawn()
        {
            if (Instance != null && Instance != this) { return; }

            Instance = this;
            base.OnNetworkPostSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == null || Instance != this) { return; }
            Instance = null;
            base.OnNetworkDespawn();
        }

        public override void Update()
        {
            base.Update();

            if (playerHeldBy != null)
                previousPlayerHeldBy = playerHeldBy;
            else if (previousPlayerHeldBy != null && Vector3.Distance(previousPlayerHeldBy.transform.position, transform.position) > audioSource.maxDistance)
                previousPlayerHeldBy = null;

            if (localPlayer != previousPlayerHeldBy) { return; }
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
            audioSource.spatialBlend = playerHeldBy != null && playerHeldBy == localPlayer ? 0 : 1;
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
                if (SCP735Behavior.Instance == null || __instance != localPlayer || SCP735Behavior.Instance.previousPlayerHeldBy != __instance) { return; }
                SCP735Behavior.Instance.SpeakPhrase(SCP735Behavior.Phrase.PlayerDiesPhrases, overrideIfPlaying: true);
                SCP735Behavior.Instance.previousPlayerHeldBy = null;
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
            if (SCP735Behavior.Instance == null || __instance != localPlayer || SCP735Behavior.Instance.previousPlayerHeldBy != __instance) { return; }

            switch (causeOfDeath)
            {
                case CauseOfDeath.Gravity:
                    SCP735Behavior.Instance.SpeakPhrase(Phrase.PlayerFallDamagePhrases);
                    break;
                case CauseOfDeath.Mauling:
                    SCP735Behavior.Instance.SpeakPhrase(Phrase.MonsterDamagePhrases);
                    break;
                case CauseOfDeath.Stabbing:
                    SCP735Behavior.Instance.SpeakPhrase(Phrase.MonsterDamagePhrases);
                    break;
                case CauseOfDeath.Scratching:
                    SCP735Behavior.Instance.SpeakPhrase(Phrase.MonsterDamagePhrases);
                    break;
                default:
                    break;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientClientRpc))]
        public static void DamagePlayerFromOtherClientClientRpcPostfix(PlayerControllerB __instance)
        {
            if (SCP735Behavior.Instance == null || __instance != localPlayer || SCP735Behavior.Instance.previousPlayerHeldBy != __instance) { return; }
            SCP735Behavior.Instance.SpeakPhrase(Phrase.PlayerDamagePhrases);
        }
    }
}