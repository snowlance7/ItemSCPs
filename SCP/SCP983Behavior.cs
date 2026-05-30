using Dawn.Utils;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static ItemSCPs.SCP.SCP9831Behavior;
using static ItemSCPs.Plugin;
using SnowyLib;

// SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId] TODO: USE THIS FOR PITCH DETECTION?
// TODO: Use a holding hand out animation for holding the monkey, with it sitting on your hand
// happy birthday to you, happy birthday to you, happy birthday dear player, bad luck go with you! A ding ding ding its your birthday!

namespace ItemSCPs.SCP
{
    public class SCP983Behavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AudioSource audioSource;
        public AudioClip monkeyFlipSFX;
        public AudioClip[] birthdaySongsSFX;

        public Animator animator;
        public Transform candyDropPosition;
        public MeshRenderer eyesRenderer;

        PlayerControllerB targetPlayer;
#pragma warning restore CS8618

        string defaultNoteTimes = ".150, .453, .604, 1.059, 1.363, 1.817-2.272, 2.576, 2.727, 2.879, 3.334, 3.788, 4.092-4.547, 4.850, 5.002, 5.153, 5.608, 5.911, 6.215-6.518, 6.669-6.973, 7.276, 7.428, 7.731, 8.186, 8.489-9.095, 9.399, 9.702, 10.005, 10.460, 10.612, 10.915-11.218, 11.370-12.280";

        bool isTargetPlayer => targetPlayer == localPlayer;

        bool activated;
        bool songPlaying;

        bool isHolding;
        bool inWindow;

        int timesPlayed;

        Note[] notes = [];

        // Configs
        const float distanceToActivate = 2f;
        readonly BoundedRange pitchRange = new BoundedRange(0.9f, 1.1f);

        const float minAccuracyRequired = 0.5f;
        const int maxPlays = 5;
        const float calculateTime = 2.5f;
        const float grace = 0.1f;
        const string cfgNoteHoldTimes = ".150, .453, .604, 1.059, 1.363, 1.817-2.272, 2.576, 2.727, 2.879, 3.334, 3.788, 4.092-4.547, 4.850, 5.002, 5.153, 5.608, 5.911, 6.215-6.518, 6.669-6.973, 7.276, 7.428, 7.731, 8.186, 8.489-9.095, 9.399, 9.702, 10.005, 10.460, 10.612, 10.915-11.218, 11.370-12.280";

        public void Awake() // TODO: Set these
        {
            itemProperties.positionOffset = new Vector3(0f, 0f, 0f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;
        }

        public override void Start()
        {
            base.Start();

            itemProperties.positionOffset = new Vector3(-0.13f, 0.01f, -0.15f);
            itemProperties.rotationOffset = new Vector3(120f, 0f, -90f);
            itemProperties.floorYOffset = 90;
            
            Material mat = eyesRenderer.material;
            mat.SetFloat("_EmissiveIntensity", 1f);

            targetPlayer = Utils.GetRandomPlayer(Utils.randomGlobal);
            notes = ParseNoteTimesConfig(cfgNoteHoldTimes).ToArray();
        }

        List<Note> ParseNoteTimesConfig(string cfg)
        {
            var result = new List<Note>();

            foreach (var note in cfg.Replace(" ", "").Split(','))
            {
                var parts = note.Split('-');
                if (parts.Length > 2)
                {
                    logger.LogWarning($"Error parsing config string for SCP-983 NoteHoldTimes. `{note}` should have at most one dash, skipping...");
                    continue;
                }

                if (float.TryParse(parts[0], out float start))
                {
                    float end = start;
                    if (parts.Length == 2 && !float.TryParse(parts[1], out end))
                    {
                        logger.LogWarning($"Invalid end time in `{note}`, skipping...");
                        continue;
                    }
                    result.Add(new Note(start, end, grace));
                }
                else
                {
                    logger.LogWarning($"Invalid start time in `{note}`, skipping...");
                }
            }

            return result;
        }
        public override void Update()
        {
            base.Update();

            if (IsServer && !activated && targetPlayer != null && targetPlayer.isPlayerControlled && Vector3.Distance(targetPlayer.transform.position, transform.position) < distanceToActivate)
            {
                activated = true;
                ActivateClientRpc();
            }

            if (!songPlaying || !isTargetPlayer) { return; }

            float songTime = audioSource.time;
            inWindow = false;

            for (int i = 0; i < notes.Length; i++)
            {
                inWindow = songTime >= notes[i].startTime - grace && songTime <= notes[i].endTime;

                if (inWindow)
                {
                    if (isHolding)
                        notes[i].heldTime += Time.deltaTime;
                    break;
                }
            }

            SetEyes();
        }

        void SetEyes()
        {
            Material mat = eyesRenderer.material;

            Color emissiveColor = Color.black;

            if (inWindow)
                emissiveColor = isHolding ? Color.green : Color.white;
            else if (isHolding)
                emissiveColor = Color.red;

            mat.SetColor("_EmissiveColor", emissiveColor);
        }

        public override int GetItemDataToSave() => activated ? 1 : 0;
        public override void LoadItemSaveData(int saveData) => activated = saveData == 1;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            isHolding = buttonDown;
        }

        public void PlaySongOnLocalClient()
        {
            if (timesPlayed >= maxPlays)
            {
                targetPlayer.KillPlayer(Vector3.zero);
                DispenseCandy(CandyType.Bad);
                return;
            }

            logger.LogDebug("Animation: song");
            animator.SetTrigger("song");
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

        void DispenseCandy(CandyType candyType)
        {
            if (!IsServer) { return; }
            logger.LogDebug("Dispensing candy " + candyType.ToString());
            var candy = Utils.SpawnItem(ItemSCPsKeys.SCP9831, candyDropPosition.position);
            (candy as SCP9831Behavior)?.ChangeCandyTypeClientRpc(candyType);
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

        float CalculateResult()
        {
            float totalScore = 0f;

            foreach (var note in notes)
            {
                if (note.duration <= 0f) continue;
                float accuracy = note.heldTime / note.duration;
                accuracy = Mathf.Clamp01(accuracy);

                totalScore += accuracy;
            }

            float averageScore = totalScore / notes.Length;

            logger.LogInfo("Score: " + averageScore);
            return averageScore;
        }

        void ResetHoldTimes()
        {
            for (int i = 0; i < notes.Length; i++)
            {
                notes[i].heldTime = 0f;
            }
        }

        public void OnFinishSong() // Animation
        {
            if (!isTargetPlayer) { return; }
            logger.LogDebug("Getting result");

            IEnumerator GetResultRoutine()
            {
                yield return null;
                yield return new WaitForSeconds(calculateTime);

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

            StartCoroutine(GetResultRoutine());
        }

        public void OnStartSong() // Animation
        {
            grabbable = true;
            int songIndex = GetSongIndex(timesPlayed);
            logger.LogDebug("SongIndex: " + songIndex);

            DoStatusEffects(songIndex);
            ResetHoldTimes();
            audioSource.pitch = pitchRange.GetRandomInRange(Utils.randomGlobal);
            audioSource.clip = birthdaySongsSFX[songIndex];
            audioSource.Play();
            songPlaying = true;
            timesPlayed++;
        }

        // RPCs

        [ClientRpc]
        private void ActivateClientRpc()
        {
            activated = true;
            grabbable = false;
            logger.LogDebug("Animation: flip");
            animator.SetTrigger("flip");
            audioSource.PlayOneShot(monkeyFlipSFX, 1f);
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
            PlaySongOnLocalClient();
        }

        [ServerRpc(RequireOwnership = false)]
        private void DispenseCandyServerRpc(CandyType candyType)
        {
            if (!IsServer) { return; }
            DispenseCandy(candyType);
        }
    }

    [Serializable]
    public struct Note(float startTime, float endTime, float grace)
    {
        public float startTime = startTime/* - grace*/;
        public float endTime = endTime + grace;
        public float duration => endTime - startTime;
        public float heldTime;
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

        public void Awake() // TODO: Set these
        {
            itemProperties.positionOffset = new Vector3(0f, 0f, 0f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;
        }

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