using Dawn.Utils;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static ItemSCPs.Items.Snowy.SCP9831Behavior;
using static ItemSCPs.Plugin;

// SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId] TODO: USE THIS FOR PITCH DETECTION?
// TODO: Use a holding hand out animation for holding the monkey, with it sitting on your hand
// happy birthday to you, happy birthday to you, happy birthday dear player, bad luck go with you! A ding ding ding its your birthday!

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

        PlayerControllerB targetPlayer;
#pragma warning restore CS8618

        bool isTargetPlayer => targetPlayer == localPlayer;

        bool activated;
        bool songPlaying;

        int timesPlayed;

        // Configs
        const float distanceToActivate = 2f;
        readonly BoundedRange pitchRange = new BoundedRange(0.9f, 1.1f);

        const float minAccuracyRequired = 0.5f;
        const int maxPlays = 10;
        const float calculateTime = 2.5f;
        const float grace = 0.1f;

        public override void Start()
        {
            base.Start();

            itemProperties.positionOffset = new Vector3(-0.13f, 0.01f, -0.15f);
            itemProperties.rotationOffset = new Vector3(120f, 0f, -90f);
            itemProperties.floorYOffset = 90;

            targetPlayer = Utils.GetRandomPlayer()!;
        }

        public override void Update()
        {
            base.Update();

            if (IsServer && !activated && targetPlayer != null && targetPlayer.isPlayerControlled && Vector3.Distance(targetPlayer.transform.position, transform.position) < distanceToActivate)
            {
                activated = true;
                ActivateClientRpc();
            }


        }

        public override int GetItemDataToSave() => activated ? 1 : 0;
        public override void LoadItemSaveData(int saveData) => activated = saveData == 1;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);


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
            throw new System.NotImplementedException();
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

            DoStatusEffects(songIndex);
            audioSource.pitch = pitchRange.GetRandomInRange(Utils.randomLocal);
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
    public struct Note
    {
        public float startTime;
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