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

        static float timesPlayedNormalized;
        int timesPlayed;
        float score;

        public static float vignetteIntensity;

        public static float sprintMeter;

        Note[] notes = [];

        static bool hinderingLocalPlayer;

        const float distanceToActivate = 2f;

        static float minAccuracyRequiredForPerfect = 0.95f;
        static float minAccuracyRequiredForGood = 0.5f;
        static int maxPlays = 5;
        static float calculateTime = 2.5f;
        static float grace = 0.1f;
        static string noteHoldTimes = ".150, .453, .604, 1.059, 1.363, 1.817-2.272, 2.576, 2.727, 2.879, 3.334, 3.788, 4.092-4.547, 4.850, 5.002, 5.153, 5.608, 5.911, 6.215-6.518, 6.669-6.973, 7.276, 7.428, 7.731, 8.186, 8.489-9.095, 9.399, 9.702, 10.005, 10.460, 10.612, 10.915-11.218, 11.370-12.280";

        [InitConfig]
        public static void InitConfigs()
        {
            minAccuracyRequiredForPerfect = PluginInstance.Config.Bind("SCP-983 Options", "SCP-983 | Min Accuracy Required For Perfect", 0.95f, "The min accuracy required to win the singing minigame and dispense a perfect candy.").Value;
            minAccuracyRequiredForGood = PluginInstance.Config.Bind("SCP-983 Options", "SCP-983 | Min Accuracy Required For Good", 0.5f, "The min accuracy required to win the singing minigame and dispense a good candy.").Value;
            maxPlays = PluginInstance.Config.Bind("SCP-983 Options", "SCP-983 | Max Plays", 5, "The max amount of times SCP-983 will sing the birthday song before the player dies.").Value;
            calculateTime = PluginInstance.Config.Bind("SCP-983 Options", "SCP-983 | Calculate Time", 2.5f, "The amount of time it takes between songs to calculate score. Timing has no effect on actual score, just gives a buffer before singing again.").Value;
            grace = PluginInstance.Config.Bind("SCP-983 Options", "SCP-983 | Grace", 0.1f, "The grace time before and after each note to be counted as holding/singing the note").Value;
            noteHoldTimes = PluginInstance.Config.Bind("SCP-983 Options", "SCP-983 | Note Hold Times", ".150, .453, .604, 1.059, 1.363, 1.817-2.272, 2.576, 2.727, 2.879, 3.334, 3.788, 4.092-4.547, 4.850, 5.002, 5.153, 5.608, 5.911, 6.215-6.518, 6.669-6.973, 7.276, 7.428, 7.731, 8.186, 8.489-9.095, 9.399, 9.702, 10.005, 10.460, 10.612, 10.915-11.218, 11.370-12.280", "The singing/holding times for each note in the song time that the player should sing for").Value;
        }

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
            notes = ParseNoteTimesConfig(noteHoldTimes).ToArray();
        }

        List<Note> ParseNoteTimesConfig(string cfg)
        {
            var result = new List<Note>();

            foreach (var note in cfg.Replace(" ", "").Split(','))
            {
                var parts = note.Split('-');
                if (parts.Length > 2)
                {
                    logger.LogWarning($"Error parsing config string for SCP-983 NoteHoldTimes. `{note}` should have at most one dash, using default NoteHoldTimes...");
                    return ParseNoteTimesConfig(defaultNoteTimes);
                }

                if (float.TryParse(parts[0], out float start))
                {
                    float end = start;
                    if (parts.Length == 2 && !float.TryParse(parts[1], out end))
                    {
                        logger.LogWarning($"Invalid end time in `{note}`, using default NoteHoldTimes...");
                        return ParseNoteTimesConfig(defaultNoteTimes);
                    }
                    result.Add(new Note(start, end, grace));
                }
                else
                {
                    logger.LogWarning($"Invalid start time in `{note}`, using default NoteHoldTimes...");
                    return ParseNoteTimesConfig(defaultNoteTimes);
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

            if (!localPlayer.IsPlayerMuted())
                isSinging = localPlayer.IsPlayerSpeaking();

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
            if (!isTargetPlayer) { return; }
            isSinging = buttonDown;
        }

        public void PlaySongOnLocalClient()
        {
            if (timesPlayed > maxPlays || !targetPlayer.isPlayerControlled || targetPlayer.isPlayerDead)
            {
                if (targetPlayer.isPlayerControlled && !targetPlayer.isPlayerDead)
                    targetPlayer.KillPlayer(Vector3.zero);
                DispenseCandy(CandyType.Bad);
                eyesMaterial.SetColor("_EmissiveColor", Color.black);
                return;
            }

            logger.LogDebug("Animation: song");
            animator.SetTrigger("song");
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

                if (score >= minAccuracyRequiredForPerfect)
                {
                    DispenseCandyRpc(CandyType.Perfect);
                    eyesMaterial.SetColor("_EmissiveColor", Color.black);
                }
                else if (score >= minAccuracyRequiredForGood)
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

            if (songIndex > 0 && isTargetPlayer)
                Utils.DisplayStatusEffect("WARNING: You are aging rapidly");

            timesPlayedNormalized = timesPlayed / maxPlays;

            ResetHoldTimes();
            audioSource.clip = birthdaySongsSFX[songIndex];
            audioSource.Play();
            songPlaying = true;
            timesPlayed++;

            if (timesPlayed > 2 && isTargetPlayer && score < 0.2f)
            {
                if (localPlayer.IsPlayerMuted())
                    HUDManager.Instance.DisplayTip("Tip", "Pick up and use [LMB] the monkey toy to sing along", useSave: true, prefsKey: "SCP983Tip1");
                else
                    HUDManager.Instance.DisplayTip("Tip", "Sing along with your microphone", useSave: true, prefsKey: "SCP983Tip2");
            }
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void ActivateRpc()
        {
            activated = true;
            grabbable = false;

            animator.SetTrigger("flip");
            audioSource.PlayOneShot(monkeyFlipSFX, 1f);

            if (!isTargetPlayer) { return; }

            localPlayer.StatusEffectController().ApplyEffect(new TickActionEffect(() =>
            {
                var setVignette = Mathf.Lerp(0f, 0.9f, timesPlayedNormalized);
                vignetteIntensity = Mathf.Min(setVignette, vignetteIntensity + Time.deltaTime * 0.1f);
                VignetteOverlay.SetIntensity(Mathf.Max(VignetteOverlay.currentIntensity, vignetteIntensity));

                var setSprintMeter = Mathf.Clamp01(Mathf.Lerp(1f, -1f, timesPlayedNormalized));
                localPlayer.sprintMeter = Mathf.Min(setSprintMeter, localPlayer.sprintMeter);

                localPlayer.isExhausted |= timesPlayedNormalized > 0.5f;

                if (timesPlayedNormalized > 0.6f && !hinderingLocalPlayer)
                {
                    hinderingLocalPlayer = true;
                    localPlayer.isMovementHindered++;
                }

                var setVolume = Mathf.Lerp(1f, 0.3f, timesPlayedNormalized);
                AudioListener.volume = Mathf.Min(AudioListener.volume, setVolume);

            }, "SCP-983", "SCP-983_Aging", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Replace, curable: false, onRemove: (effect) =>
            {
                logger.LogDebug("SCP-983_Aging OnRemove");
                AudioListener.volume = IngamePlayerSettings.Instance.settings.masterVolume;
                vignetteIntensity = 0f;

                if (hinderingLocalPlayer)
                {
                    hinderingLocalPlayer = false;
                    localPlayer.isMovementHindered--;
                }
            }));
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

            switch (candyType)
            {
                case CandyType.Perfect:

                    localPlayer.StatusEffectController().RemoveEffect((e) => e.id == "SCP-983_Aging");
                    localPlayer.health = 200;
                    localPlayer.MakeCriticallyInjured(false);

                    localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect((effect) =>
                    {
                        logger.LogDebug("PerfectCandyEffect start");
                        if (!localPlayer.isPlayerControlled || localPlayer.isPlayerDead || StartOfRound.Instance.inShipPhase || StartOfRound.Instance.shipIsLeaving) { return; }
                        if (UnityEngine.Random.Range(0f, 1f) > 0.2f) { return; }
                        NetworkHandler.Instance.CreateLightFlashRpc(localPlayer.bodyParts[5].transform.position);
                        localPlayer.KillPlayer(Vector3.zero, spawnBody: false);

                        localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect((effect) =>
                        {
                            logger.LogDebug("PerfectCandyExtraLife start");
                            localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect((effect) =>
                            {
                                logger.LogDebug("PerfectCandyRevive start");
                                if (localPlayer.isPlayerControlled && !localPlayer.isPlayerDead) { return; }
                                SnowyLib.NetworkHandler.Instance.RevivePlayerRpc(localPlayer.actualClientId);
                                localPlayer.health = 200;

                            }, "SCP-983-1", "PerfectCandyRevive", 10f, onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false));

                        }, "SCP-983-1", "PerfectCandyExtraLife", onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false));

                    }, "SCP-983-1", "PerfectCandyEffect", UnityEngine.Random.Range(10, 301), onConflict: (existing, incoming) => StatusEffectController.ConflictResult.Deny, curable: false));

                    break;
                case CandyType.Good:
                    localPlayer.StatusEffectController().RemoveEffect((e) => e.id == "SCP-983_Aging");
                    break;
                case CandyType.Bad:

                    localPlayer.StatusEffectController().ApplyEffect(new OnRemoveActionEffect((effect) =>
                    {
                        logger.LogDebug("BadCandyEffect start");
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