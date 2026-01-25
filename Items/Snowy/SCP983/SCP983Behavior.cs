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
using static ItemSCPs.Plugin;

// SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId] TODO: USE THIS FOR PITCH DETECTION?

namespace ItemSCPs.Items.Snowy.SCP983
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

        float timeSinceSongStarted;
        float lastWordTime;

        int timesPlayed;

        int maxPlays => birthdaySongsSFX.Length;
        float songLength => birthdaySongsSFX[timesPlayed].length;

        static string[] acceptedWords = { "happy", "birthday", "to", "you", "dear", "player", "bad", "luck", "go", "with", "ding", "its", "your" };

        // Configs
        float distanceToActivate = 5f;
        BoundedRange pitchRange = new BoundedRange(0.9f, 1.1f);
        float minAccuracyRequired = 0.6f;
        float graceWordTiming = 0.3f;

        public static void RegisterPhrases()
        {
            if (ItemSCPsContentHandler.Instance.SCP983 == null) { return; }
            // happy birthday to you, happy birthday to you, happy birthday dear player, bad luck go with you! A ding ding ding its your birthday!
            Voice.CustomListenForPhrases(acceptedWords, (obj, recognized) => { onPhraseSpoken.Invoke(recognized.Message, recognized.Confidence); });
        }

        public override void Start()
        {
            base.Start();
            if (activated) { return; }
            targetPlayer = StartOfRound.Instance.allPlayerScripts[Utils.randomLocal.Next(0, StartOfRound.Instance.allPlayerScripts.Length)];
            if (targetPlayer == localPlayer)
                onPhraseSpoken.AddListener(OnPhraseSpoken);
        }

        public override void Update()
        {
            base.Update();

            timeSinceSongStarted += Time.time;

            if (IsServer && !activated && targetPlayer != null && targetPlayer.isPlayerControlled && Vector3.Distance(targetPlayer.transform.position, transform.position) < distanceToActivate)
            {
                activated = true;
                PlaySongClientRpc();
            }

            if (songPlaying && timeSinceSongStarted > songLength)
            {
                songPlaying = false;
                CalculateResult();
            }
        }

        void OnPhraseSpoken(string phrase, float confidence)
        {
            if (!songPlaying || !acceptedWords.Contains(phrase))
                return;

            float delta = Time.time - lastWordTime;
            spoken.Add(new(phrase, delta, confidence));
            lastWordTime = Time.time;

            logger.LogDebug($"{phrase}, {delta}, {confidence}");
        }

        void CalculateResult()
        {
            // TODO
        }

        void ResetVariables()
        {
            lastWordTime = 0f;
            spoken.Clear();
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

            audioSource.pitch = pitchRange.GetRandomInRange(Utils.randomLocal);
            audioSource.clip = birthdaySongsSFX[timesPlayed];
            audioSource.Play();
            timeSinceSongStarted = 0f;
            songPlaying = true;
            timesPlayed++;
            ResetVariables();
        }
    }

    class Step(string phrase, float delay = 0.4f, float confidence = 0.6f)
    {
        public string phrase = phrase;
        public float delay = delay;
        public float confidence = confidence;
    }

    static class Steps
    {
        public static Step[] steps =
        {
            new("happy"),
            new("birthday"),
            new("to"),
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