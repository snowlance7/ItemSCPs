using Dawn.Utils;
using GameNetcodeStuff;
using PSCPLibrary;
using PSCPLibrary.Interfaces;
using SnowyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static ItemSCPs.Plugin;
using static ItemSCPs.SCP.SCP9831Behavior;

// SoundManager.Instance.playerVoicePitches[localPlayer.actualClientId] UPDATE: USE THIS FOR PITCH DETECTION?
// happy birthday to you, happy birthday to you, happy birthday dear player, bad luck go with you! A ding ding ding its your birthday!

namespace ItemSCPs.SCP
{
    internal class SCP983Behavior : PhysicsProp, ISCP, ISingletonItem
    {
        [SerializeField] SCPInfo info = null!;
        public SCPInfo SCPInfo => info;

        public AudioSource audioSource = null!;
        public AudioClip monkeyFlipSFX = null!;
        public AudioClip[] birthdaySongsSFX = null!;

        public Animator animator = null!;
        public Transform candyDropPosition = null!;
        public MeshRenderer eyesRenderer = null!;

        PlayerControllerB targetPlayer = null!;

        Material eyesMaterial = null!;

        string defaultNoteTimes = ".150, .453, .604, 1.059, 1.363, 1.817-2.272, 2.576, 2.727, 2.879, 3.334, 3.788, 4.092-4.547, 4.850, 5.002, 5.153, 5.608, 5.911, 6.215-6.518, 6.669-6.973, 7.276, 7.428, 7.731, 8.186, 8.489-9.095, 9.399, 9.702, 10.005, 10.460, 10.612, 10.915-11.218, 11.370-12.280";

        bool isTargetPlayer => targetPlayer == localPlayer;

        bool activated;
        bool songPlaying;

        bool isSinging;
        bool inWindow;

        int timesPlayed;
        float score;

        public static float scpVignetteIntensity;

        Note[] notes = [];
        private float amplitude;
        private float amplitudeRelative;

        // Configs
        const float distanceToActivate = 2f;
        readonly BoundedRange pitchRange = new BoundedRange(0.9f, 1.1f);

        const bool tipEnabled = true;
        const float minAccuracyRequired = 0.5f;
        const int maxPlays = 5;
        const float calculateTime = 2.5f;
        const float grace = 0.1f;
        const string cfgNoteHoldTimes = ".150, .453, .604, 1.059, 1.363, 1.817-2.272, 2.576, 2.727, 2.879, 3.334, 3.788, 4.092-4.547, 4.850, 5.002, 5.153, 5.608, 5.911, 6.215-6.518, 6.669-6.973, 7.276, 7.428, 7.731, 8.186, 8.489-9.095, 9.399, 9.702, 10.005, 10.460, 10.612, 10.915-11.218, 11.370-12.280";

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(-0.13f, 0.01f, -0.15f);
            itemProperties.rotationOffset = new Vector3(120f, 0f, -90f);
            itemProperties.floorYOffset = 90;
        }

        public override void Start()
        {
            base.Start();

            eyesMaterial = eyesRenderer.material;
            eyesMaterial.SetFloat("_EmissiveIntensity", 1f);

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

            if (IsServer && !TESTING.immunity && !activated && targetPlayer != null && targetPlayer.isPlayerControlled && Vector3.Distance(targetPlayer.transform.position, transform.position) < distanceToActivate)
            {
                activated = true;
                ActivateRpc();
            }

            if (!songPlaying || !isTargetPlayer) { return; }

            if (!Utils.IsPlayerMuted())
                isSinging = Utils.IsPlayerSpeaking(amplitudeThreshold: 0.3f, useRelativeAmplitude: true);

            float songTime = audioSource.time;
            inWindow = false;

            for (int i = 0; i < notes.Length; i++)
            {
                inWindow = songTime >= notes[i].startTime - grace && songTime <= notes[i].endTime;

                if (inWindow)
                {
                    if (isSinging)
                        notes[i].heldTime += Time.deltaTime;
                    break;
                }
            }

            SetEyes();
        }

        void SetEyes()
        {
            Color emissiveColor = Color.black;

            if (inWindow)
                emissiveColor = isSinging ? Color.green : Color.white;
            else if (isSinging)
                emissiveColor = Color.red;

            eyesMaterial.SetColor("_EmissiveColor", emissiveColor);
        }

        public override int GetItemDataToSave() => activated ? 1 : 0;
        public override void LoadItemSaveData(int saveData) => activated = saveData == 1;

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!Utils.IsPlayerMuted()) { return; }
            isSinging = buttonDown;
        }

        public void PlaySongOnLocalClient()
        {
            if (timesPlayed >= maxPlays)
            {
                targetPlayer.KillPlayer(Vector3.zero);
                DispenseCandy(CandyType.Bad);
                eyesMaterial.SetColor("_EmissiveColor", Color.black);
                return;
            }

            logger.LogDebug("Animation: song");
            animator.SetTrigger("song");
        }

        void DoStatusEffects(int songIndex) // TODO: Test this
        {
            if (!isTargetPlayer) { return; }
            logger.LogDebug("Doing player effects " + songIndex);

            if (songIndex > 0)
                Utils.DisplayStatusEffect("WARNING: You are aging rapidly");

            switch (songIndex)
            {
                case 0:
                    break;
                case 1:
                    localPlayer.StatusEffectController().ApplyEffect(new TickActionEffect(() =>
                    {
                        scpVignetteIntensity = Mathf.Min(0.2f, scpVignetteIntensity + Time.deltaTime * 0.1f);
                        VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, scpVignetteIntensity));

                        localPlayer.sprintMeter = Mathf.Min(localPlayer.sprintMeter, 0.8f);
                    }, "SCP-983", "SCP-983_Aging", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Replace, curable: false, onRemove: (effect) =>
                    {
                        scpVignetteIntensity = 0f;
                    }));
                    break;
                case 2:
                    localPlayer.StatusEffectController().ApplyEffect(new TickActionEffect(() =>
                    {
                        scpVignetteIntensity = Mathf.Min(0.35f, scpVignetteIntensity + Time.deltaTime * 0.1f);
                        VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, scpVignetteIntensity));

                        localPlayer.sprintMeter = Mathf.Min(localPlayer.sprintMeter, 0.4f);
                        AudioListener.volume = Mathf.Min(AudioListener.volume, 0.9f);
                    }, "SCP-983", "SCP-983_Aging", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Replace, curable: false, onRemove: (effect) =>
                    {
                        AudioListener.volume = IngamePlayerSettings.Instance.settings.masterVolume;
                        scpVignetteIntensity = 0f;
                    }));
                    break;
                case 3:
                    localPlayer.StatusEffectController().ApplyEffect(new TickActionEffect(() =>
                    {
                        scpVignetteIntensity = Mathf.Min(0.4f, scpVignetteIntensity + Time.deltaTime * 0.1f);
                        VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, scpVignetteIntensity));

                        localPlayer.sprintMeter = 0;
                        localPlayer.isExhausted = true;
                        AudioListener.volume = Mathf.Min(AudioListener.volume, 0.7f);
                    }, "SCP-983", "SCP-983_Aging", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Replace, curable: false, onRemove: (effect) =>
                    {
                        AudioListener.volume = IngamePlayerSettings.Instance.settings.masterVolume;
                        scpVignetteIntensity = 0f;
                    }));
                    break;
                case 4:
                    localPlayer.StatusEffectController().ApplyEffect(new TickActionEffect(() =>
                    {
                        scpVignetteIntensity = Mathf.Min(0.5f, scpVignetteIntensity + Time.deltaTime * 0.1f);
                        VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, scpVignetteIntensity));

                        localPlayer.sprintMeter = 0;
                        localPlayer.isExhausted = true;
                        SoundManager.Instance.earsRingingTimer = 1f;
                        AudioListener.volume = Mathf.Min(AudioListener.volume, 0.5f);
                    }, "SCP-983", "SCP-983_Aging", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Replace, curable: false, onRemove: (effect) =>
                    {
                        AudioListener.volume = IngamePlayerSettings.Instance.settings.masterVolume;
                        scpVignetteIntensity = 0f;
                    }));
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
            (candy as SCP9831Behavior)?.ChangeCandyTypeRpc(candyType);
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

                score = CalculateResult();
                logger.LogDebug("Score: " + score);

                if (score >= 1f)
                {
                    DispenseCandyRpc(CandyType.Perfect);
                    eyesMaterial.SetColor("_EmissiveColor", Color.black);
                }
                else if (score >= minAccuracyRequired)
                {
                    DispenseCandyRpc(CandyType.Good);
                    eyesMaterial.SetColor("_EmissiveColor", Color.black);
                }
                else
                {
                    PlaySongRpc();
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

            if (tipEnabled && timesPlayed > 2 && isTargetPlayer && score < 0.2f)
            {
                if (Utils.IsPlayerMuted())
                    HUDManager.Instance.DisplayTip("Tip", "Pick up and use [LMB] the monkey toy to sing along", useSave: true, prefsKey: "SCP983Tip1");
                else
                    HUDManager.Instance.DisplayTip("Tip", "Sing along with your microphone", useSave: true, prefsKey: "SCP983Tip2");
            }
        }

        // RPCs

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void ActivateRpc()
        {
            activated = true;
            grabbable = false;
            logger.LogDebug("Animation: flip");
            animator.SetTrigger("flip");
            audioSource.PlayOneShot(monkeyFlipSFX, 1f);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void PlaySongRpc()
        {
            PlaySongOnLocalClient();
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void DispenseCandyRpc(CandyType candyType)
        {
            if (!IsServer) { return; }
            DispenseCandy(candyType);
        }
    }

    [Serializable]
    internal struct Note(float startTime, float endTime, float grace)
    {
        public float startTime = startTime - grace;
        public float endTime = endTime + grace;
        public float duration => endTime - startTime;
        public float heldTime;
    }

    internal class SCP9831Behavior : PhysicsProp
    {
        public AudioClip eatCandySFX = null!;
        public MeshRenderer[] renderers = null!;
        public Material[] materials = null!;

        public enum CandyType
        {
            Perfect,
            Good,
            Bad
        }

        CandyType candyType;

        public void Awake()
        {
            itemProperties.positionOffset = new Vector3(0.08f, 0.1f, 0f);
            itemProperties.rotationOffset = new Vector3(0, 0, 0);
            itemProperties.floorYOffset = 90;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (!buttonDown) { return; }

            switch (candyType) // TODO: Test this
            {
                case CandyType.Perfect:

                    localPlayer.StatusEffectController().RemoveEffect((e) => e.id == "SCP-983_Aging");
                    localPlayer.health = 200;
                    localPlayer.MakeCriticallyInjured(false);

                    localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        logger.LogDebug("PerfectCandyEffect start");
                        if (!localPlayer.isPlayerControlled || localPlayer.isPlayerDead || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
                        if (UnityEngine.Random.Range(0f, 1f) > 0.2f) { return; }
                        NetworkHandler.Instance.CreateLightFlashRpc(localPlayer.bodyParts[5].transform.position);
                        localPlayer.KillPlayer(Vector3.zero, spawnBody: false);

                        localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                        {
                            logger.LogDebug("PerfectCandyExtraLife start");
                            localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                            {
                                logger.LogDebug("PerfectCandyRevive start");
                                if (localPlayer.isPlayerControlled && !localPlayer.isPlayerDead) { return; }
                                localPlayer.RevivePlayer();
                                localPlayer.health = 200;

                            }, "SCP-983-1", "PerfectCandyRevive", 10f, onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false));

                        }, "SCP-983-1", "PerfectCandyExtraLife", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false));

                    }, "SCP-983-1", "PerfectCandyEffect", UnityEngine.Random.Range(10, 301), onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false));

                    break;
                case CandyType.Good:
                    localPlayer.StatusEffectController().RemoveEffect((e) => e.id == "SCP-983_Aging");
                    break;
                case CandyType.Bad:

                    localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect(() =>
                    {
                        logger.LogDebug("PerfectCandyExtraLife start");
                        if (!localPlayer.isPlayerControlled || localPlayer.isPlayerDead) { return; }

                        Utils.DisplayStatusEffect("WARNING: You are aging extremely fast");

                        localPlayer.StatusEffectController().ApplyEffect(new LerpValueEffect((x) =>
                        {
                            VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, x));

                            localPlayer.sprintMeter = Mathf.Min(localPlayer.sprintMeter, 1 - x);
                            localPlayer.isExhausted = true;

                            if (x > 0.8f)
                                SoundManager.Instance.earsRingingTimer = 1f;

                            AudioListener.volume = Mathf.Min(AudioListener.volume, 1 - x);

                        }, 0f, 1f, 10f, "SCP-983-1", "BadCandyAging", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false, onRemove: (effect) =>
                        {
                            logger.LogDebug("BadCandyAging OnRemove");
                            if (!localPlayer.isPlayerControlled || localPlayer.isPlayerDead) { return; }
                            localPlayer.KillPlayer(Vector3.zero);
                            AudioListener.volume = IngamePlayerSettings.Instance.settings.masterVolume;
                        }));

                    }, "SCP-983-1", "BadCandyEffect", 10f, onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false));

                    break;
                default:
                    break;
            }

            playerHeldBy.statusEffectAudio.PlayOneShot(eatCandySFX, 1f);
            playerHeldBy.DespawnHeldObject();
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void ChangeCandyTypeRpc(CandyType candyType)
        {
            this.candyType = candyType;

            foreach (var renderer in renderers)
            {
                renderer.material = materials[(int)candyType];
            }
        }
    }

    public class LightFlash : MonoBehaviour
    {
        [SerializeField] Light light = null!;
        [SerializeField] AnimationCurve intensityCurve = null!;

        float timeSinceSpawned;
        const float destroyTime = 60;

        void Update()
        {
            timeSinceSpawned += Time.deltaTime;

            if (timeSinceSpawned >= destroyTime)
            {
                Destroy(this.gameObject);
                return;
            }

            float timeNormalized = timeSinceSpawned / destroyTime;
            float intensityNormalized = intensityCurve.Evaluate(timeNormalized);

            float lightIntensity = Mathf.Lerp(0, 40000, intensityNormalized);
            light.intensity = lightIntensity;

        }
    }
}