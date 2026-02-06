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
using PySpeech;
using static ItemSCPs.Items.Snowy.SCP9831Behavior;
using static ItemSCPs.Plugin;

// SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId] TODO: USE THIS FOR PITCH DETECTION?
// TODO: Use a holding hand out animation for holding the monkey, with it sitting on your hand

namespace ItemSCPs.Items.Snowy
{
    public class SCP983Behavior : PhysicsProp
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

        static UnityEvent<string> onPhraseSpoken = new UnityEvent<string>();

        bool activated;
        bool songPlaying;
        bool candyDispensed;

        int timesPlayed;

        static readonly string[] acceptedPhrases = { "happy birthday", "to you", "happy birthday dear", "go with you", "ding ding ding", "its your birthday" };
        string[] acceptedPhrasesInOrder = { "happy birthday", "to you", "happy birthday", "to you", "happy birthday dear", "bad luck", "go with you", "ding ding ding", "its your birthday" };

        List<string> spoken = [];

        // Configs
        const float distanceToActivate = 5f;
        readonly BoundedRange pitchRange = new BoundedRange(0.9f, 1.1f);

        const float minAccuracyRequired = 0.5f;
        const int maxPlays = 10;

        public static void RegisterPhrases()
        {
            if (ItemSCPsContentHandler.Instance.SCP983 == null) { return; }
            // happy birthday to you, happy birthday to you, happy birthday dear player, bad luck go with you! A ding ding ding its your birthday!
            Speech.RegisterPhrases(acceptedPhrases);
            Speech.RegisterCustomHandler((obj, recognized) => { onPhraseSpoken?.Invoke(recognized.Text); });
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
                DoAnimationClientRpc("flip");
            }

            if (songPlaying && !audioSource.isPlaying)
            {
                songPlaying = false;
                float score = CalculateResult();
                logger.LogDebug("Score: " + score);

                if (score >= 1f)
                {
                    DispenseCandyServerRpc(CandyType.Perfect);
                }
                else if (score >= minAccuracyRequired)
                {
                    DispenseCandyServerRpc(CandyType.Good);
                }
                else
                {
                    PlaySongServerRpc();
                }
            }
        }

        void OnPhraseSpoken(string phrase) // Event // TODO: Continue here
        {
            if (!songPlaying) { return; }
            bool accepted = Speech.IsAboveThreshold(acceptedPhrases, minAccuracyRequired);

            if (accepted)
                spoken.Add(phrase);

            logger.LogDebug($"{(accepted ? "\u2714" : "\u2718")} {phrase}"); // TODO: Test this
        }

        public void OnFinishFlip() // Animation
        {
            PlaySong();
        }

        public float CalculateResult()
        {
            if (spoken.Count == 0)
                return 0f;

            int total = acceptedPhrasesInOrder.Length;
            int correctInOrder = 0;
            int correctAnywhere = 0;

            // Count exact matches in order
            int minLength = Mathf.Min(spoken.Count, total);
            for (int i = 0; i < minLength; i++)
            {
                if (spoken[i].ToLower().Trim() == acceptedPhrasesInOrder[i].ToLower())
                {
                    correctInOrder++;
                }
            }

            // Count matches ignoring order
            List<string> acceptedList = acceptedPhrasesInOrder.Select(x => x.ToLower()).ToList();
            foreach (var phrase in spoken)
            {
                string p = phrase.ToLower().Trim();
                if (acceptedList.Contains(p))
                {
                    correctAnywhere++;
                    acceptedList.Remove(p); // remove to avoid double-counting
                }
            }

            // Combine scores: exact in-order counts more (70%), anywhere matches less (30%)
            float inOrderScore = (float)correctInOrder / total;
            float anywhereScore = (float)correctAnywhere / total;

            float finalScore = inOrderScore * 0.7f + anywhereScore * 0.3f;

            // Clamp to 0-1
            return Mathf.Clamp01(finalScore);
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

        void PlaySong()
        {
            activated = true;

            if (timesPlayed >= maxPlays)
            {
                targetPlayer.KillPlayer(Vector3.zero);
                DispenseCandy(CandyType.Bad);
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

        void DispenseCandy(CandyType candyType)
        {
            if (!IsServer) { return; }
            var candy = Utils.SpawnItem(ItemSCPsKeys.SCP983, candyDropPosition.position);
            (candy as SCP9831Behavior)?.ChangeCandyTypeClientRpc(candyType);
            candyDispensed = true;
            spoken.Clear();
            songPlaying = false;
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
            PlaySong();
        }

        [ServerRpc(RequireOwnership = false)]
        private void DispenseCandyServerRpc(CandyType candyType)
        {
            if (!IsServer) { return; }
            DispenseCandy(candyType);
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
}