using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;

namespace ItemSCPs.SCP
{
    internal class SCP735Behavior : PhysicsProp // TODO: Make this work with SCP-714
    {
        public AudioSource ItemSFX;

        public AudioClip[] MonsterDamagePhrases;
        public AudioClip[] NearOtherPlayersPhrases;
        public AudioClip[] PlayerDiesPhrases;
        public AudioClip[] PlayerDamagePhrases;
        public AudioClip[] PlayerFallDamagePhrases;
        public AudioClip[] RandomPhrases;

        public Dictionary<string, AudioClip[]> Phrases = new Dictionary<string, AudioClip[]>();

        float timeSinceLastPhrase;
        float phraseCooldown;

        PlayerControllerB? previousPlayerHeldBy;

        // Configs
        float minPhraseCooldown = 5f;
        float maxPhraseCooldown = 10f;
        float nearPlayersRadius = 5f;

        public override void Start()
        {
            base.Start();

            Phrases.Add("MonsterDamagePhrases", MonsterDamagePhrases);
            Phrases.Add("NearOtherPlayersPhrases", NearOtherPlayersPhrases);
            Phrases.Add("PlayerDiesPhrases", PlayerDiesPhrases);
            Phrases.Add("PlayerDamagePhrases", PlayerDamagePhrases);
            Phrases.Add("PlayerFallDamagePhrases", PlayerFallDamagePhrases);
            Phrases.Add("RandomPhrases", RandomPhrases);
        }

        public override void Update()
        {
            base.Update();

            if (playerHeldBy == null) { return; }
            previousPlayerHeldBy = playerHeldBy;
            if (localPlayer != playerHeldBy) { return; }

            timeSinceLastPhrase += Time.deltaTime;

            if (previousPlayerHeldBy.takingFallDamage)
            {
                SpeakPhrase("PlayerFallDamagePhrases");
            }

            if (timeSinceLastPhrase > phraseCooldown)
            {
                if (previousPlayerHeldBy.NearOtherPlayers(null, nearPlayersRadius) && UnityEngine.Random.Range(0, 3) == 0)
                {
                    SpeakPhrase("NearOtherPlayersPhrases");
                }
                else
                {
                    SpeakPhrase("RandomPhrases");
                }
            }
        }

        public void SpeakPhrase(string phrase, bool overrideIfPlaying = false)
        {
            if (ItemSFX.isPlaying && !overrideIfPlaying) { return; }

            if (IsServerOrHost)
            {
                SpeakPhraseClientRpc(phrase);
            }
            else
            {
                SpeakPhraseServerRpc(phrase);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SpeakPhraseServerRpc(string phraseName)
        {
            if (IsServerOrHost)
                SpeakPhraseClientRpc(phraseName);
        }

        [ClientRpc]
        public void SpeakPhraseClientRpc(string phraseName)
        {
            logger.LogDebug("Speaking phrase: " + phraseName);

            AudioClip[] phrases = Phrases[phraseName];

            int index = UnityEngine.Random.Range(0, phrases.Length);
            AudioClip phrase = phrases[index];
            ItemSFX.Stop();
            ItemSFX.clip = phrase;
            ItemSFX.Play();
            RoundManager.Instance.PlayAudibleNoise(transform.position);
            WalkieTalkie.TransmitOneShotAudio(ItemSFX, phrase, 0.85f);
            timeSinceLastPhrase = 0f;

            if (previousPlayerHeldBy == null) { return; }
            previousPlayerHeldBy.drunkness = 0.2f;
            if (previousPlayerHeldBy.playersManager.fearLevel < 0.2f) { previousPlayerHeldBy.JumpToFearLevel(0.2f); }
            if (previousPlayerHeldBy == localPlayer)
            {
                timeSinceLastPhrase = 0f;
                phraseCooldown = UnityEngine.Random.Range(minPhraseCooldown, maxPhraseCooldown);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void MonsterDamagedPlayerServerRpc()
        {
            if (IsServerOrHost)
                SpeakPhrase("MonsterDamagePhrases");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerDamagedPlayerServerRpc()
        {
            if (IsServerOrHost)
                SpeakPhrase("PlayerDamagePhrases");
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerKilledServerRpc()
        {
            if (IsServerOrHost)
                SpeakPhrase("PlayerDiesPhrases", true);
        }
    }

    [HarmonyPatch]
    internal class SCP735Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        public static void KillPlayerPrefix(PlayerControllerB __instance)
        {
            try
            {
                if (__instance == localPlayer)
                {
                    if (__instance.currentlyHeldObjectServer != null && __instance.currentlyHeldObjectServer.itemProperties.name == "SCP735Item")
                    {
                        SCP735Behavior? scp = __instance.currentlyHeldObjectServer.GetComponent<SCP735Behavior>();

                        if (scp != null)
                        {
                            scp.PlayerKilledServerRpc();
                        }
                    }
                }
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
            if (__instance == localPlayer && !__instance.isPlayerDead)
            {
                if (__instance.currentlyHeldObjectServer != null && __instance.currentlyHeldObjectServer.itemProperties.name == "SCP735Item")
                {
                    SCP735Behavior? scp = __instance.currentlyHeldObjectServer.GetComponent<SCP735Behavior>();

                    if (scp != null)
                    {
                        if (causeOfDeath == CauseOfDeath.Mauling || causeOfDeath == CauseOfDeath.Stabbing)
                        {
                            scp.MonsterDamagedPlayerServerRpc();
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientClientRpc))]
        public static void DamagePlayerFromOtherClientClientRpcPostfix(PlayerControllerB __instance)
        {
            if (__instance == localPlayer && !__instance.isPlayerDead)
            {
                if (__instance.currentlyHeldObjectServer != null && __instance.currentlyHeldObjectServer.itemProperties.name == "SCP735Item")
                {
                    SCP735Behavior? scp = __instance.currentlyHeldObjectServer.GetComponent<SCP735Behavior>();

                    if (scp != null)
                    {
                        scp.PlayerDamagedPlayerServerRpc();
                    }
                }
            }
        }
    }
}