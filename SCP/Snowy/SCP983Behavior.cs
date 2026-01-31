using BepInEx.Logging;
using Dawn.Utils;
using GameNetcodeStuff;
using LethalLib.Modules;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using VoiceRecognitionAPI;
using static ItemSCPs.Items.Snowy.SCP9831Behavior;
using static ItemSCPs.Plugin;

// SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId] TODO: USE THIS FOR PITCH DETECTION?
// TODO: Use a holding hand out animation for holding the monkey, with it sitting on your hand

namespace ItemSCPs.Items.Snowy
{
    internal class SCP983Behavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public AudioClip monkeyFlipSFX;
        public AudioClip[] birthdaySongsSFX;

        public Animator animator;
        public Transform candyDropPosition;
        public GameObject eyes;

        public Collider collider;


        PlayerControllerB targetPlayer;
#pragma warning restore CS8618

        static UnityEvent<string, float> onPhraseSpoken = new UnityEvent<string, float>();
        Step[] steps => Steps.steps;

        List<Step> spoken = [];

        bool activated;
        bool songPlaying;
        bool candyDispensed;

        int timesPlayed;

        static readonly string[] acceptedWords = { "happy", "birthday", "to", "you", "dear", "player", "bad", "luck", "go", "with", "ding", "its", "your" };

        // Configs
        const float distanceToActivate = 5f;
        readonly BoundedRange pitchRange = new BoundedRange(0.9f, 1.1f);

        const float minAccuracyRequired = 0.6f;
        const int maxPlays = 10;

        public static void RegisterPhrases()
        {
            if (ItemSCPsContentHandler.Instance.SCP983 == null) { return; }
            // happy birthday to you, happy birthday to you, happy birthday dear player, bad luck go with you! A ding ding ding its your birthday!
            Voice.CustomListenForPhrases(acceptedWords, (obj, recognized) => { onPhraseSpoken.Invoke(recognized.Message, recognized.Confidence); });
        }

        public override void Start()
        {
            base.Start();
            if (candyDispensed) { return; }
            targetPlayer = StartOfRound.Instance.allPlayerScripts[Utils.randomLocal.Next(0, StartOfRound.Instance.allPlayerScripts.Length)];
            if (targetPlayer == localPlayer)
                onPhraseSpoken.AddListener(OnPhraseSpoken);
        }

        public override void Update()
        {
            base.Update();

            if (IsServer && !activated && targetPlayer != null && targetPlayer.isPlayerControlled && Vector3.Distance(targetPlayer.transform.position, transform.position) < distanceToActivate)
            {
                activated = true;
                PlaySongClientRpc();
            }

            if (songPlaying && !audioSource.isPlaying)
            {
                songPlaying = false;
                CalculateResult();
            }
        }

        void OnPhraseSpoken(string phrase, float confidence)
        {
            if (!songPlaying || !acceptedWords.Contains(phrase))
                return;

            spoken.Add(new(phrase, confidence));

            logger.LogDebug($"{phrase}, {confidence}");
        }

        void CalculateResult()
        {
            // TODO
            Dictionary<string, int> expectedCounts = new();
            foreach (var step in steps)
            {
                expectedCounts.TryAdd(step.phrase, 0);
                expectedCounts[step.phrase]++;
            }

            Dictionary<string, List<float>> spokenConfidences = new();
            foreach (var s in spoken)
            {
                if (!spokenConfidences.ContainsKey(s.phrase))
                    spokenConfidences[s.phrase] = new List<float>();

                spokenConfidences[s.phrase].Add(s.confidence);
            }

            float score = 0f;
            float maxScore = steps.Length;

            foreach (var kvp in expectedCounts)
            {
                string word = kvp.Key;
                int expected = kvp.Value;

                if (!spokenConfidences.TryGetValue(word, out var confidences))
                    continue;

                // Sort highest confidence first
                confidences.Sort((a, b) => b.CompareTo(a));

                int matches = Mathf.Min(expected, confidences.Count);

                for (int i = 0; i < matches; i++)
                {
                    float c = confidences[i];

                    // Confidence-weighted credit
                    float confidenceScore = Mathf.Clamp01(
                        (c - minAccuracyRequired) / (1f - minAccuracyRequired)
                    );

                    score += confidenceScore;
                }
            }

            float accuracy = Mathf.Clamp01(score / maxScore);

            if (accuracy >= 1f)
            {
                DispenseCandyServerRpc(CandyType.Perfect);
            }
            else if (accuracy >= minAccuracyRequired)
            {
                DispenseCandyServerRpc(CandyType.Good);
            }
            else
            {
                PlaySongServerRpc();
            }
        }

        void DoStatusEffects(int songIndex)
        {
            // TODO
            switch (songIndex)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                default:
                    break;
            }
        }

        int GetSongIndex(int playCount)
        {
            int numSongs = birthdaySongsSFX.Length;
            int basePlays = maxPlays / numSongs;
            int extraPlays = maxPlays % numSongs;

            int[] playDistribution = new int[numSongs];
            for (int i = 0; i < numSongs; i++)
                playDistribution[i] = basePlays + (i < extraPlays ? 1 : 0);

            // Make last song get the last play
            playDistribution[numSongs - 1] = maxPlays - playDistribution.Take(numSongs - 1).Sum();

            int runningSum = 0;
            for (int i = 0; i < numSongs; i++)
            {
                runningSum += playDistribution[i];
                if (playCount <= runningSum)
                    return i;
            }

            return numSongs - 1; // fallback
        }

        public override int GetItemDataToSave() => candyDispensed ? 1 : 0;
        public override void LoadItemSaveData(int saveData) => candyDispensed = saveData == 1;

        // RPCs

        [ClientRpc]
        private void DoAnimationClientRpc(string animName)
        {
            logger.LogDebug("DoAnimationClientRpc: " + animName);
            animator.SetTrigger(animName);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlaySongServerRpc()
        {
            if (!IsServer) { return; }
            PlaySongClientRpc();
        }

        [ClientRpc]
        private void PlaySongClientRpc()
        {
            activated = true;

            if (timesPlayed >= maxPlays)
            {
                targetPlayer.KillPlayer(Vector3.zero);
                return;
            }

            int songIndex = GetSongIndex(timesPlayed);

            DoStatusEffects(songIndex);

            spoken.Clear();
            audioSource.pitch = pitchRange.GetRandomInRange(Utils.randomLocal);
            audioSource.clip = birthdaySongsSFX[songIndex];
            audioSource.Play();
            songPlaying = true;
            timesPlayed++;
        }

        [ServerRpc(RequireOwnership = false)]
        private void DispenseCandyServerRpc(CandyType candyType)
        {
            if (!IsServer) { return; }
            var candy = Utils.SpawnItem(ItemSCPsKeys.SCP983, candyDropPosition.position);
            (candy as SCP9831Behavior)?.ChangeCandyTypeClientRpc(candyType);
            candyDispensed = true;
            spoken.Clear();
            songPlaying = false;
        }
    }

    internal class SCP9831Behavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioClip eatCandySFX;
        public MeshRenderer[] renderers;
        public Material[] materials;
#pragma warning restore CS8618

        public enum CandyType
        {
            Perfect,
            Good,
            Bad
        }

        CandyType candyType;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            switch (candyType) // TODO
            {
                case CandyType.Perfect:
                    break;
                case CandyType.Good:
                    break;
                case CandyType.Bad:
                    break;
                default:
                    break;
            }

            playerHeldBy.statusEffectAudio.PlayOneShot(eatCandySFX, 1f);
            playerHeldBy.DespawnHeldObject();
        }

        [ClientRpc]
        public void ChangeCandyTypeClientRpc(CandyType candyType)
        {
            this.candyType = candyType;

            foreach (var renderer in renderers)
            {
                renderer.material = materials[(int)candyType];
            }
        }
    }

    class Step(string phrase, float confidence = 0.6f)
    {
        public string phrase = phrase;
        public float confidence = confidence;
    }

    static class Steps
    {
        public static Step[] steps =
        {
            new("happy"),
            new("birthday"),
            new("to", 0.5f),
            new("you"),
            new("happy"),
            new("birthday"),
            new("to"),
            new("you"),
            new("happy"),
            new("birthday"),
            new("dear"),
            new("player"),
            new("bad"),
            new("luck"),
            new("go"),
            new("with"),
            new("you"),
            new("ding"),
            new("ding"),
            new("ding"),
            new("its"),
            new("your"),
            new("birthday")
        };
    }
}